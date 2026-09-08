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

using System;
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using JCG = J2N.Collections.Generic;
// NOpenNLP: inside NOpenNLP.Tools.Cmdline.Namefind the bare name Dictionary binds to the
// NOpenNLP.Tools.Cmdline.Dictionary namespace, so the type is aliased.
using NameDictionary = NOpenNLP.Tools.Dictionary.Dictionary;

namespace NOpenNLP.Tools.Cmdline.Namefind;

/// <summary>
/// Tests the evaluation of a <see cref="DictionaryNameFinder"/>.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream keeps this test in opennlp-tools, beside the name finder it
/// evaluates, because the cmdline package ships there too. This port splits cmdline out
/// into NOpenNLP.Cli, which NOpenNLP.Tools.Tests does not reference, so the test lives
/// here instead of at its upstream-mirrored path.
/// </remarks>
public class DictionaryNameFinderEvaluatorTest
{
    [Test]
    public void TestEvaluator()
    {
        DictionaryNameFinder nameFinder = new(CreateDictionary());
        TokenNameFinderEvaluator evaluator = new(nameFinder, new NameEvaluationErrorListener());
        IObjectStream<NameSample?> sample = CreateSample();

        evaluator.Evaluate(sample);
        // NOpenNLP: upstream calls close(); the ported IObjectStream is IDisposable.
        sample.Dispose();
        var fmeasure = evaluator.FMeasure;

        ClassicAssert.IsTrue(fmeasure.Value == 1);
        ClassicAssert.IsTrue(fmeasure.RecallScore == 1);
    }

    /// <summary>
    /// Creates a NameSample stream using an annotated corpus
    /// </summary>
    /// <exception cref="System.IO.IOException"/>
    private static IObjectStream<NameSample?> CreateSample()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/namefind/AnnotatedSentences.txt");

        return new NameSampleDataStream(new PlainTextByLineStream(@in, Latin1));
    }

    /// <summary>
    /// Creates a dictionary with all names from the sample data.
    /// </summary>
    /// <returns>a dictionary</returns>
    /// <exception cref="System.IO.IOException"/>
    private static NameDictionary CreateDictionary()
    {
        IObjectStream<NameSample?> sampleStream = CreateSample();
        NameSample? sample = sampleStream.Read();
        JCG.List<string[]> entries = [];
        while (sample != null)
        {
            Span[] names = sample.Names;
            if (names != null && names.Length > 0)
            {
                string[] toks = sample.Sentence;
                foreach (var name in names)
                {
                    string[] nameToks = new string[name.Length];
                    Array.Copy(toks, name.Start, nameToks, 0, name.Length);
                    entries.Add(nameToks);
                }
            }
            sample = sampleStream.Read();
        }
        sampleStream.Dispose();
        NameDictionary dictionary = new(true);
        foreach (var entry in entries)
        {
            StringList dicEntry = new(entry);
            dictionary.Put(dicEntry);
        }
        return dictionary;
    }

    // NOpenNLP: StandardCharsets.ISO_8859_1 has no named BCL counterpart that is
    // registered on every target, so the code page is used directly.
    private static Encoding Latin1 => Encoding.GetEncoding(28591);
}
