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

// This file has been modified from the original Apache OpenNLP source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Ad;

public class ADTokenSampleStreamTest
{
    // NOpenNLP: NUnit creates one fixture instance for the whole class and runs [SetUp]
    // before each test, where JUnit 4 creates a fresh instance per test, so the list has to
    // be cleared here rather than relying on a new instance the way upstream does.
    private readonly JCG.List<TokenSample> samples = [];

    private TempDirectory workingDirectory = null!;

    // NOpenNLP: upstream resolves both files with getResource(...).toURI(), which works
    // because the Java build copies test resources onto the classpath as real files. The
    // factory takes file paths -- it opens the corpus itself and reads the detokenizer
    // dictionary from disk -- and the .NET counterparts of those resources are embedded in
    // the assembly, so they are materialized into a temporary directory instead.
    [SetUp]
    public void Setup()
    {
        samples.Clear();

        workingDirectory = new TempDirectory("nopennlp-ad-token");

        FileInfo dict = workingDirectory.CopyResource(
            "/opennlp/tools/tokenize/latin-detokenizer.xml", "latin-detokenizer.xml");
        FileInfo data = workingDirectory.CopyResource(
            "/opennlp/tools/formats/ad.sample", "ad.sample");

        var factory = new ADTokenSampleStreamFactory();

        // NOpenNLP: upstream passes the command line as a string[] and lets ArgumentParser
        // build the annotated Params proxy from it. The port keys parameter values by
        // descriptor, so the same four arguments are supplied directly.
        var values = new ParameterValues
        {
            [FormatParameters.Data] = data,
            [ADNameSampleStreamFactory.EncodingParam] = "UTF-8",
            [FormatParameters.Lang] = "por",
            [FormatParameters.Detokenizer] = dict.FullName,
        };

        using IObjectStream<TokenSample?> tokenSampleStream = factory.Create(values);

        while (tokenSampleStream.Read() is { } sample)
        {
            samples.Add(sample);
        }
    }

    [TearDown]
    public void TearDown() => workingDirectory.Dispose();

    [Test]
    public void TestSimpleCount()
    {
        ClassicAssert.AreEqual(ADParagraphStreamTest.NumSentences, samples.Count);
    }

    [Test]
    public void TestSentences()
    {
        ClassicAssert.IsTrue(samples[5].Text.Contains("ofereceu-me"));
    }

    /// <summary>
    /// Supplies parameter values to a factory from a dictionary keyed by descriptor.
    /// </summary>
    /// <remarks>
    /// Authored for NOpenNLP; it stands in for the dynamic proxy upstream's
    /// <c>ArgumentParser.parse</c> returns. A parameter the test does not set falls back to
    /// the descriptor's default, as an omitted optional argument does on the command line.
    /// </remarks>
    private sealed class ParameterValues : IFormatParameterValues
    {
        private readonly Dictionary<IFormatParameter, object?> values = [];

        public object? this[IFormatParameter parameter]
        {
            set => values[parameter] = value;
        }

        public T? Get<T>(IFormatParameter parameter)
        {
            if (values.TryGetValue(parameter, out object? value))
            {
                return (T?)value;
            }

            return parameter.DefaultValue is T defaultValue ? defaultValue : default;
        }
    }
}
