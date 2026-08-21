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
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// Registry for object stream factories.
/// </summary>
public static class StreamFactoryRegistry
{
    // NOpenNLP: upstream keys a HashMap by the sample Class and nests a HashMap of
    // format name to factory, so both iteration orders are unspecified. That order is
    // user-visible -- it drives the [.fmt1|.fmt2] alternation in a typed tool's help
    // and the parenthesized format list in a converter's description -- so both levels
    // are insertion-ordered here and the registrations below run in upstream's order.
    private static readonly JCG.LinkedDictionary<Type, JCG.LinkedDictionary<string, object>> registry =
        new JCG.LinkedDictionary<Type, JCG.LinkedDictionary<string, object>>();

    /// <summary>
    /// The format assumed when the user names none.
    /// </summary>
    public const string DefaultFormat = "opennlp";

    static StreamFactoryRegistry()
    {
        // NOpenNLP: the order below is upstream's static initializer order in
        // opennlp.tools.cmdline.StreamFactoryRegistry, and is preserved because it is
        // the order formats are listed in a tool's help.
        ChunkerSampleStreamFactory.RegisterFactory();
        DocumentSampleStreamFactory.RegisterFactory();
        NameSampleDataStreamFactory.RegisterFactory();
        ParseSampleStreamFactory.RegisterFactory();
        SentenceSampleStreamFactory.RegisterFactory();
        TokenSampleStreamFactory.RegisterFactory();
        WordTagSampleStreamFactory.RegisterFactory();
        LemmatizerSampleStreamFactory.RegisterFactory();
        LanguageDetectorSampleStreamFactory.RegisterFactory();

        Convert.NameToSentenceSampleStreamFactory.RegisterFactory();
        Convert.NameToTokenSampleStreamFactory.RegisterFactory();

        Convert.POSToSentenceSampleStreamFactory.RegisterFactory();
        Convert.POSToTokenSampleStreamFactory.RegisterFactory();

        Convert.ParseToPOSSampleStreamFactory.RegisterFactory();
        Convert.ParseToSentenceSampleStreamFactory.RegisterFactory();
        Convert.ParseToTokenSampleStreamFactory.RegisterFactory();

        Ontonotes.OntoNotesNameSampleStreamFactory.RegisterFactory();
        Ontonotes.OntoNotesParseSampleStreamFactory.RegisterFactory();
        Ontonotes.OntoNotesPOSSampleStreamFactory.RegisterFactory();

        BioNLP2004NameSampleStreamFactory.RegisterFactory();
        Conll02NameSampleStreamFactory.RegisterFactory();
        Conll03NameSampleStreamFactory.RegisterFactory();
        EvalitaNameSampleStreamFactory.RegisterFactory();
        ConllXPOSSampleStreamFactory.RegisterFactory();
        ConllXSentenceSampleStreamFactory.RegisterFactory();
        ConllXTokenSampleStreamFactory.RegisterFactory();
        Ad.ADChunkSampleStreamFactory.RegisterFactory();
        Ad.ADNameSampleStreamFactory.RegisterFactory();
        Ad.ADSentenceSampleStreamFactory.RegisterFactory();
        Ad.ADPOSSampleStreamFactory.RegisterFactory();
        Ad.ADTokenSampleStreamFactory.RegisterFactory();
        TwentyNewsgroupSampleStreamFactory.RegisterFactory();

        Muc.Muc6NameSampleStreamFactory.RegisterFactory();

        Frenchtreebank.ConstitParseSampleStreamFactory.RegisterFactory();

        Brat.BratNameSampleStreamFactory.RegisterFactory();

        Letsmt.LetsmtSentenceStreamFactory.RegisterFactory();
        Moses.MosesSentenceSampleStreamFactory.RegisterFactory();

        Conllu.ConlluTokenSampleStreamFactory.RegisterFactory();
        Conllu.ConlluSentenceSampleStreamFactory.RegisterFactory();
        Conllu.ConlluPOSSampleStreamFactory.RegisterFactory();
        Conllu.ConlluLemmaSampleStreamFactory.RegisterFactory();

        Irishsentencebank.IrishSentenceBankSentenceStreamFactory.RegisterFactory();
        Irishsentencebank.IrishSentenceBankTokenSampleStreamFactory.RegisterFactory();
        Leipzig.LeipzigLanguageSampleStreamFactory.RegisterFactory();
        Nkjp.NKJPSentenceSampleStreamFactory.RegisterFactory();
    }

    /// <summary>
    /// Registers <paramref name="factory"/>, which reads the format named
    /// <paramref name="formatName"/> and instantiates streams producing objects of type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <returns><c>true</c> if the factory was successfully registered</returns>
    public static bool RegisterFactory<T>(string formatName, IObjectStreamFactory<T> factory)
    {
        if (!registry.TryGetValue(typeof(T), out JCG.LinkedDictionary<string, object>? formats))
        {
            formats = new JCG.LinkedDictionary<string, object>();
            registry[typeof(T)] = formats;
        }

        if (formats.ContainsKey(formatName))
        {
            return false;
        }

        formats[formatName] = factory;
        return true;
    }

    /// <summary>
    /// Unregisters the factory which reads the format named
    /// <paramref name="formatName"/> and produces objects of type
    /// <typeparamref name="T"/>.
    /// </summary>
    public static void UnregisterFactory<T>(string formatName)
    {
        if (registry.TryGetValue(typeof(T), out JCG.LinkedDictionary<string, object>? formats))
        {
            formats.Remove(formatName);
        }
    }

    /// <summary>
    /// Returns all factories which produce objects of type <typeparamref name="T"/>,
    /// keyed by format name, in registration order.
    /// </summary>
    // NOpenNLP: upstream returns null for a sample type nothing registered, and its
    // only caller immediately calls keySet() on it. An empty map says the same thing
    // without the NullReferenceException.
    public static IReadOnlyDictionary<string, IObjectStreamFactory<T>> GetFactories<T>()
    {
        var result = new JCG.LinkedDictionary<string, IObjectStreamFactory<T>>();

        if (registry.TryGetValue(typeof(T), out JCG.LinkedDictionary<string, object>? formats))
        {
            foreach (KeyValuePair<string, object> entry in formats)
            {
                result[entry.Key] = (IObjectStreamFactory<T>)entry.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the factory which reads the format named <paramref name="formatName"/>
    /// and produces objects of type <typeparamref name="T"/>, or <c>null</c> when no
    /// such format is registered.
    /// </summary>
    /// <param name="formatName">
    /// the format name; when <c>null</c>, <see cref="DefaultFormat"/> is assumed
    /// </param>
    // NOpenNLP: upstream falls back to Class.forName(formatName) so a fully qualified
    // class name can be used as a format. That is not ported: it loads an arbitrary
    // class named on the command line, the ported factories take their parameters
    // through IObjectStreamFactory rather than a no-arg constructor plus reflection,
    // and upstream's own TODO notes it cannot check the type produces the right
    // samples. An unknown format returns null here, which the CLI reports as
    // "Format <name> is not found." with exit code 1 -- the same outcome upstream
    // reaches when the class does not resolve.
    public static IObjectStreamFactory<T>? GetFactory<T>(string? formatName)
    {
        formatName ??= DefaultFormat;

        if (registry.TryGetValue(typeof(T), out JCG.LinkedDictionary<string, object>? formats)
            && formats.TryGetValue(formatName, out object? factory))
        {
            return (IObjectStreamFactory<T>)factory;
        }

        return null;
    }
}
