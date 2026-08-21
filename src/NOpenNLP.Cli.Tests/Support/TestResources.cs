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
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace NOpenNLP.Tools.Cmdline.Support;

/// <summary>
/// Loads the corpora the CLI tests drive the tools over.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. The corpora are copied
/// verbatim from <c>opennlp-tools/src/test/resources</c> and embedded, as they are in
/// NOpenNLP.Tools.Tests. The CLI takes file paths rather than streams, so a test
/// materializes what it needs into a <see cref="TempDirectory"/> first.
/// </remarks>
internal static class TestResources
{
    /// <summary>
    /// Opens an embedded corpus by file name, i.e. <c>token.train</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The resource is not embedded in the test assembly, which is a build configuration
    /// error, so it fails loudly rather than returning null.
    /// </exception>
    public static Stream OpenResource(string name)
    {
        string manifestName = "opennlp.tools.cmdline." + name;

        Stream? stream = typeof(TestResources).Assembly.GetManifestResourceStream(manifestName);

        return stream ?? throw new InvalidOperationException(
            $"Test resource '{name}' (manifest name '{manifestName}') is not embedded in the " +
            "test assembly. Add it under Data/ in NOpenNLP.Cli.Tests.csproj.");
    }
}

/// <summary>
/// Creates an empty temporary directory for the duration of a test, and removes it
/// afterwards.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. The same helper exists in
/// NOpenNLP.Tools.Tests; the two test projects have no reference between them, so it is
/// duplicated rather than shared. As there, the directory name carries a random component
/// so concurrent runs cannot collide, and a failing test keeps its directory instead of
/// deleting the evidence.
/// </remarks>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory(string prefix = "nopennlp-cli")
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            prefix + "-" + System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetRandomFileName()));

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    /// <summary>
    /// Copies an embedded corpus into this directory and returns its full path.
    /// </summary>
    public string CopyResource(string name)
    {
        string path = System.IO.Path.Combine(Path, name);

        using Stream source = TestResources.OpenResource(name);
        using FileStream target = File.Create(path);
        source.CopyTo(target);

        return path;
    }

    /// <summary>
    /// The full path of a file in this directory, which need not exist yet.
    /// </summary>
    public string PathOf(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
        {
            TestContext.Error.WriteLine($"NOTE: leaving temporary files on disk at: {Path}");
            return;
        }

        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            TestContext.Error.WriteLine($"NOTE: could not remove temporary directory: {Path}");
        }
    }
}
