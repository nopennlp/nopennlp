# Runs the NOpenNLP command line tools in a container, so that `nopennlp` can be
# used without installing the .NET SDK or the tool itself.
#
# Apache OpenNLP ships the equivalent under opennlp-distr/src/main/docker and
# feeds it a release tarball through a build argument. This one lives at the
# repository root and builds the tool from the source beside it: the point of a
# container here is to try the working tree, and a Dockerfile that installed a
# published NuGet package would ignore whatever change prompted the build.
#
#   docker build -t nopennlp .
#   docker run --rm -it nopennlp                       # a shell, `nopennlp` on PATH
#   docker run --rm -i nopennlp nopennlp SimpleTokenizer < sentences.txt
#
# Models and corpora are not in the image. Mount them:
#
#   docker run --rm -it -v "$PWD/models:/data" nopennlp

ARG DOTNET_VERSION=10.0

# ---------------------------------------------------------------------------
# Build stage: pack NOpenNLP.Cli into a .nupkg the runtime stage installs from.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

WORKDIR /src

# The project files and version.json first, so that a source-only edit reuses
# the cached restore layer. Nerdbank.GitVersioning reads version.json here and
# .git in the copy below; both must be present before the restore runs.
COPY version.json ./
COPY src/NOpenNLP.Tools/NOpenNLP.Tools.csproj src/NOpenNLP.Tools/
COPY src/NOpenNLP.Cli/NOpenNLP.Cli.csproj src/NOpenNLP.Cli/

# Restoring the CLI project rather than the solution pulls in NOpenNLP.Tools as
# a project reference and nothing else. NOpenNLP.Benchmarks is in the solution
# and its IKVM dependency unpacks to over 4 GB, which has no place in a build of
# the command line tools; CI restores project by project for the same reason.
RUN dotnet restore src/NOpenNLP.Cli/NOpenNLP.Cli.csproj

# .git comes along because Nerdbank.GitVersioning derives the package version
# from the commit height. Without it the build fails rather than guessing, so
# building from an exported tarball needs -p:PublicRelease=false and a version
# supplied on the command line.
COPY .git .git/
COPY README.md NOTICE LICENSE ./
COPY src/NOpenNLP.Tools/ src/NOpenNLP.Tools/
COPY src/NOpenNLP.Cli/ src/NOpenNLP.Cli/

RUN dotnet pack src/NOpenNLP.Cli/NOpenNLP.Cli.csproj \
        --configuration Release \
        --no-restore \
        --output /packages

# Installing here rather than in the runtime stage, because `dotnet tool
# install` is an SDK command and the runtime image has no SDK. A tool installed
# to a --tool-path is a self-contained directory - the launcher, the tool's
# assemblies and its dependencies - so the runtime stage just copies it.
#
# --source rather than --add-source so the local directory is the only feed
# considered: the package installed must be the one just built, and a version
# that happens to exist on nuget.org must not be able to satisfy this.
# --prerelease because version.json gives development builds a `-alpha` suffix,
# which `dotnet tool install` will not resolve otherwise.
RUN dotnet tool install NOpenNLP.Cli \
        --tool-path /opt/nopennlp \
        --source /packages \
        --prerelease

# ---------------------------------------------------------------------------
# Runtime stage: the tool installed from the local package, on PATH, in a shell.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION}

LABEL org.opencontainers.image.title="NOpenNLP" \
      org.opencontainers.image.description="The NOpenNLP command line tools, a C# port of Apache OpenNLP." \
      org.opencontainers.image.source="https://github.com/nopennlp/nopennlp" \
      org.opencontainers.image.licenses="Apache-2.0"

# /opt/nopennlp on PATH is what makes `nopennlp` work in the shell this image
# drops into. A --tool-path install rather than --global keeps the command
# outside any particular user's home directory.
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    PATH="/opt/nopennlp:${PATH}"

COPY --from=build /opt/nopennlp /opt/nopennlp

# The image redistributes the ported Apache OpenNLP source, so it carries the
# license and the attribution notice somewhere a user can find them. The tool
# package embeds NOTICE too, but only inside the tool store's version-numbered
# directory.
COPY LICENSE NOTICE /usr/share/nopennlp/

# Proves the copied tool actually runs on this image before it ships, rather
# than failing the first time someone runs the container.
RUN nopennlp Doccat help > /dev/null

# Somewhere to mount models and corpora, and the directory a bare `docker run`
# starts in.
WORKDIR /data

CMD ["/bin/sh"]
