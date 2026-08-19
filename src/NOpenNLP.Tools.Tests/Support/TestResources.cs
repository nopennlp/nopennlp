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
/// An <see cref="NOpenNLP.Tools.Util.IInputStreamFactory"/> over an embedded test resource.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. It stands in for
/// upstream's <c>opennlp.tools.formats.ResourceAsStreamFactory</c>, which resolves a
/// classpath resource. That class lives in the not-yet-ported <c>formats</c> package,
/// and its only role in these tests is to reopen a resource on each call so a stream
/// can be reset, which <see cref="TestResources.OpenResource"/> already provides.
/// </remarks>
internal sealed class ResourceAsStreamFactory(string resourcePath) : NOpenNLP.Tools.Util.IInputStreamFactory
{
    public Stream CreateInputStream() => TestResources.OpenResource(resourcePath);
}
