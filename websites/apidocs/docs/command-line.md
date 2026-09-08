# Command line tools

The `NOpenNLP.Cli` package installs OpenNLP's command line tools as a
[dotnet tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) named
`nopennlp`:

```
dotnet tool install --global NOpenNLP.Cli
```

It takes the same arguments as the `opennlp` command, so existing OpenNLP
command lines, scripts and documentation carry over by changing the command
name:

```
nopennlp                                    # lists the tools
nopennlp SimpleTokenizer < sentences.txt
nopennlp TokenizerTrainer -model en-token.bin -lang eng -data token.train
nopennlp POSTaggerTrainer.conllu -model pos.bin -lang deu -data corpus.conllu -tagset u
nopennlp TokenizerME en-token.bin < sentences.txt
```

As upstream, a `.format` suffix on a tool name selects the corpus format to read
(`POSTaggerTrainer.conllu`), any tool prints its help when invoked with `help`,
and converters take their format as the first argument
(`nopennlp POSTaggerConverter conllu -data corpus.conllu`).

The package name follows Apache OpenNLP's own post-1.9.4 layout, which moved
these tools into an `opennlp-cli` module.

## Docker

The `Dockerfile` at the repository root builds an image with the tools already
installed, for trying them without a .NET SDK or a tool install:

```
docker build -t nopennlp .
docker run --rm -it nopennlp
```

That drops into a shell in `/data` with `nopennlp` on the PATH. A single command
works too:

```
echo "Dr. Smith went to Washington." | docker run --rm -i nopennlp nopennlp SimpleTokenizer
```

The image carries no models. Mount a directory holding them onto `/data`, which
is where the container starts:

```
docker run --rm -it -v "$PWD/models:/data" nopennlp
```
