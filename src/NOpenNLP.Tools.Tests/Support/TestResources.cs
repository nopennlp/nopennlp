/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
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

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Loads test resources copied from <c>opennlp-tools/src/test/resources</c> upstream.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. Upstream tests
/// call <c>getResourceAsStream("/opennlp/tools/postag/Foo.xml")</c>, which resolves
/// against the classpath. The .NET counterpart is an embedded resource, whose
/// manifest name is declared by the LogicalName in the test csproj and mirrors the
/// upstream path with dots instead of slashes. This helper takes the upstream
/// classpath path so ported tests can keep reading like the originals.
/// </remarks>
internal static class TestResources
{
    /// <summary>
    /// Opens an embedded test resource by its upstream classpath path, i.e.
    /// <c>/opennlp/tools/postag/TagDictionaryCaseSensitive.xml</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The resource is not embedded in the test assembly. This is a build
    /// configuration error, so it fails loudly rather than returning null the
    /// way Java's getResourceAsStream would.
    /// </exception>
    public static Stream OpenResource(string path)
    {
        // Translate the upstream classpath path to the manifest name declared by
        // LogicalName. J2N's FindAndGetManifestResourceStream does not do this
        // translation itself, so the lookup is done against the dotted name.
        string manifestName = path.TrimStart('/').Replace('/', '.');

        var stream = typeof(TestResources).Assembly.GetManifestResourceStream(manifestName);

        return stream ?? throw new InvalidOperationException(
            $"Test resource '{path}' (manifest name '{manifestName}') is not embedded in the test assembly. " +
            "Add it as an EmbeddedResource with a matching LogicalName in NOpenNLP.Tools.Tests.csproj.");
    }
}

/// <summary>
/// Materializes an embedded test resource to a temporary file for the duration of a test.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. The file-based event
/// streams take a path, but the test corpora are embedded resources here.
/// </remarks>
internal sealed class TempResourceFile : IDisposable
{
    public TempResourceFile(string resourcePath)
    {
        Path = System.IO.Path.GetTempFileName();
        using Stream source = TestResources.OpenResource(resourcePath);
        using FileStream target = File.Create(Path);
        source.CopyTo(target);
    }

    public string Path { get; }

    public void Dispose() => File.Delete(Path);
}

/// <summary>
/// Creates an empty temporary directory for the duration of a test, and removes it
/// afterwards.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. It stands in for JUnit's
/// <c>TemporaryFolder</c> rule, which upstream's <c>DirectorySampleStreamTest</c> uses and
/// NUnit has no direct equivalent for. The two behaviours worth copying from Lucene.NET's
/// <c>LuceneTestCase.CreateTempDir</c> are here: the directory name carries a random
/// component rather than a counter, so concurrent test runs cannot collide, and a failing
/// test keeps its directory instead of deleting the evidence.
/// </remarks>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory(string prefix = "nopennlp")
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            prefix + "-" + System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetRandomFileName()));

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public DirectoryInfo DirectoryInfo => new DirectoryInfo(Path);

    /// <summary>
    /// Writes <paramref name="content"/> to a new file in this directory and returns it.
    /// </summary>
    public FileInfo CreateFile(string name, string content)
    {
        string path = System.IO.Path.Combine(Path, name);
        File.WriteAllText(path, content);
        return new FileInfo(path);
    }

    /// <summary>
    /// Copies an embedded test resource into this directory under <paramref name="name"/>.
    /// </summary>
    public FileInfo CopyResource(string resourcePath, string name)
    {
        string path = System.IO.Path.Combine(Path, name);
        using Stream source = TestResources.OpenResource(resourcePath);
        using FileStream target = File.Create(path);
        source.CopyTo(target);
        return new FileInfo(path);
    }

    public void Dispose()
    {
        // Leave the directory behind when the test failed, so it can be inspected, and
        // say where it is. Deleting it would discard exactly the evidence needed to
        // diagnose the failure.
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
            // A file still held open by the test is not itself a test failure, and
            // throwing here would replace the real result with a cleanup error.
            TestContext.Error.WriteLine($"NOTE: could not remove temporary directory: {Path}");
        }
    }
}
