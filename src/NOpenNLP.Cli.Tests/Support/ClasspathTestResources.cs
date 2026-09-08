/*
 * Copyright 2026 NOpenNLP Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Loads the corpora the tests ported from opennlp-tools read, by their upstream
/// classpath path.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. This is the same helper
/// NOpenNLP.Tools.Tests declares under the same name and namespace, duplicated rather
/// than linked because the two test projects have no reference between them.
/// <see cref="Formats.ResourceAsStreamFactory"/>, which is linked in from that project,
/// resolves every resource through it, so the name and namespace have to match.
/// <para/>
/// This sits alongside <see cref="Cmdline.Support.TestResources"/>, which serves the
/// end-to-end CLI tests: those look a corpus up by bare file name under
/// <c>opennlp.tools.cmdline.</c>, while the ported tests address theirs by the upstream
/// classpath path the Java originals use.
/// </remarks>
internal static class TestResources
{
    /// <summary>
    /// Opens an embedded test resource by its upstream classpath path, i.e.
    /// <c>/opennlp/tools/chunker/output.txt</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The resource is not embedded in the test assembly. This is a build configuration
    /// error, so it fails loudly rather than returning null the way Java's
    /// getResourceAsStream would.
    /// </exception>
    public static Stream OpenResource(string path)
    {
        var manifestName = path.TrimStart('/').Replace('/', '.');

        var stream = typeof(TestResources).Assembly.GetManifestResourceStream(manifestName);

        return stream ?? throw new InvalidOperationException(
            $"Test resource '{path}' (manifest name '{manifestName}') is not embedded in the test assembly. " +
            "Add it under Corpora/ in NOpenNLP.Cli.Tests.csproj.");
    }
}

/// <summary>
/// Materializes an embedded test resource to a temporary file for the duration of a test.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. Duplicated from
/// NOpenNLP.Tools.Tests for the same reason as <see cref="TestResources"/>: the ported
/// tests read corpora by classpath path, but some of the streams they feed take a file
/// path instead.
/// </remarks>
internal sealed class TempResourceFile : IDisposable
{
    public TempResourceFile(string resourcePath)
    {
        Path = System.IO.Path.GetTempFileName();
        using var source = TestResources.OpenResource(resourcePath);
        using var target = File.Create(Path);
        source.CopyTo(target);
    }

    public string Path { get; }

    /// <remarks>
    /// NOpenNLP: best effort. A reader that still holds the file open makes File.Delete
    /// throw on Windows while succeeding elsewhere, which would fail an otherwise
    /// passing test on one platform only. The file is in the system temp directory, so
    /// the OS reclaims it either way.
    /// </remarks>
    public void Dispose()
    {
        try
        {
            File.Delete(Path);
        }
        catch (IOException)
        {
            // Something still holds the file open; leave it to the OS.
        }
        catch (UnauthorizedAccessException)
        {
            // As above.
        }
    }
}
