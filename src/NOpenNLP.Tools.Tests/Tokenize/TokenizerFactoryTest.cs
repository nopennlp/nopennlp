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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Tokenize.Lang;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// Tests for the <see cref="TokenizerFactory"/> class.
/// </summary>
public class TokenizerFactoryTest
{
    private static IObjectStream<TokenSample?> CreateSampleStream()
    {
        // NOpenNLP: upstream uses opennlp.tools.formats.ResourceAsStreamFactory,
        // which lives in the not-yet-ported formats package; the test-side
        // ResourceAsStreamFactory in Support does the same job over an embedded
        // resource.
        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/tokenize/token.train");

        return new TokenSampleStream(new PlainTextByLineStream(@in, Encoding.UTF8));
    }

    private static TokenizerModel Train(TokenizerFactory factory) =>
        TokenizerME.Train(CreateSampleStream(), factory, TrainingParameters.DefaultParams());

    private static NOpenNLP.Tools.Dictionary.Dictionary LoadAbbDictionary()
    {
        // NOpenNLP: upstream calls getResourceAsStream; the .NET counterpart is
        // an embedded resource, opened through the shared TestResources helper.
        using Stream @in = TestResources.OpenResource("/opennlp/tools/sentdetect/abb.xml");

        return new NOpenNLP.Tools.Dictionary.Dictionary(@in);
    }

    [Test]
    public void TestDefault()
    {
        NOpenNLP.Tools.Dictionary.Dictionary dic = LoadAbbDictionary();
        const string lang = "spa";

        TokenizerModel model = Train(new TokenizerFactory(lang, dic, false, null!));

        TokenizerFactory factory = model.Factory;
        ClassicAssert.IsTrue(factory.AbbreviationDictionary != null);
        ClassicAssert.IsTrue(factory.ContextGenerator is DefaultTokenContextGenerator);

        ClassicAssert.AreEqual(Factory.DEFAULT_ALPHANUMERIC, factory.AlphaNumericPattern!.ToString());
        ClassicAssert.AreEqual(lang, factory.LanguageCode);
        ClassicAssert.AreEqual(lang, model.Language);
        ClassicAssert.IsFalse(factory.UseAlphaNumericOptmization);

        MemoryStream @out = new MemoryStream();
        model.Serialize(@out);
        MemoryStream @in = new MemoryStream(@out.ToArray());

        TokenizerModel fromSerialized = new TokenizerModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsTrue(factory.AbbreviationDictionary != null);
        ClassicAssert.IsTrue(factory.ContextGenerator is DefaultTokenContextGenerator);

        ClassicAssert.AreEqual(Factory.DEFAULT_ALPHANUMERIC, factory.AlphaNumericPattern!.ToString());
        ClassicAssert.AreEqual(lang, factory.LanguageCode);
        ClassicAssert.AreEqual(lang, model.Language);
        ClassicAssert.IsFalse(factory.UseAlphaNumericOptmization);
    }

    [Test]
    public void TestNullDict()
    {
        NOpenNLP.Tools.Dictionary.Dictionary? dic = null;
        const string lang = "spa";

        TokenizerModel model = Train(new TokenizerFactory(lang, dic, false, null!));

        TokenizerFactory factory = model.Factory;
        ClassicAssert.IsNull(factory.AbbreviationDictionary);
        ClassicAssert.IsTrue(factory.ContextGenerator is DefaultTokenContextGenerator);

        ClassicAssert.AreEqual(Factory.DEFAULT_ALPHANUMERIC, factory.AlphaNumericPattern!.ToString());
        ClassicAssert.AreEqual(lang, factory.LanguageCode);
        ClassicAssert.AreEqual(lang, model.Language);
        ClassicAssert.IsFalse(factory.UseAlphaNumericOptmization);

        MemoryStream @out = new MemoryStream();
        model.Serialize(@out);
        MemoryStream @in = new MemoryStream(@out.ToArray());

        TokenizerModel fromSerialized = new TokenizerModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsNull(factory.AbbreviationDictionary);
        ClassicAssert.IsTrue(factory.ContextGenerator is DefaultTokenContextGenerator);

        ClassicAssert.AreEqual(Factory.DEFAULT_ALPHANUMERIC, factory.AlphaNumericPattern!.ToString());
        ClassicAssert.AreEqual(lang, factory.LanguageCode);
        ClassicAssert.AreEqual(lang, model.Language);
        ClassicAssert.IsFalse(factory.UseAlphaNumericOptmization);
    }

    [Test]
    public void TestCustomPatternAndAlphaOpt()
    {
        NOpenNLP.Tools.Dictionary.Dictionary? dic = null;
        const string lang = "spa";
        string pattern = "^[0-9A-Za-z]+$";

        TokenizerModel model = Train(new TokenizerFactory(lang, dic, true, new Regex(pattern)));

        TokenizerFactory factory = model.Factory;
        ClassicAssert.IsNull(factory.AbbreviationDictionary);
        ClassicAssert.IsTrue(factory.ContextGenerator is DefaultTokenContextGenerator);

        ClassicAssert.AreEqual(pattern, factory.AlphaNumericPattern!.ToString());
        ClassicAssert.AreEqual(lang, factory.LanguageCode);
        ClassicAssert.AreEqual(lang, model.Language);
        ClassicAssert.IsTrue(factory.UseAlphaNumericOptmization);

        MemoryStream @out = new MemoryStream();
        model.Serialize(@out);
        MemoryStream @in = new MemoryStream(@out.ToArray());

        TokenizerModel fromSerialized = new TokenizerModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsNull(factory.AbbreviationDictionary);
        ClassicAssert.IsTrue(factory.ContextGenerator is DefaultTokenContextGenerator);
        ClassicAssert.AreEqual(pattern, factory.AlphaNumericPattern!.ToString());
        ClassicAssert.AreEqual(lang, factory.LanguageCode);
        ClassicAssert.AreEqual(lang, model.Language);
        ClassicAssert.IsTrue(factory.UseAlphaNumericOptmization);
    }

    [Test]
    public void TestDummyFactory()
    {
        NOpenNLP.Tools.Dictionary.Dictionary dic = LoadAbbDictionary();
        const string lang = "spa";
        string pattern = "^[0-9A-Za-z]+$";

        TokenizerModel model = Train(new DummyTokenizerFactory(lang, dic, true, new Regex(pattern)));

        TokenizerFactory factory = model.Factory;
        ClassicAssert.IsTrue(factory.AbbreviationDictionary is DummyTokenizerFactory.DummyDictionary);
        ClassicAssert.IsTrue(factory.ContextGenerator is DummyTokenizerFactory.DummyContextGenerator);
        ClassicAssert.AreEqual(pattern, factory.AlphaNumericPattern!.ToString());
        ClassicAssert.AreEqual(lang, factory.LanguageCode);
        ClassicAssert.AreEqual(lang, model.Language);
        ClassicAssert.IsTrue(factory.UseAlphaNumericOptmization);

        MemoryStream @out = new MemoryStream();
        model.Serialize(@out);
        MemoryStream @in = new MemoryStream(@out.ToArray());

        TokenizerModel fromSerialized = new TokenizerModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsTrue(factory.AbbreviationDictionary is DummyTokenizerFactory.DummyDictionary);
        ClassicAssert.IsTrue(factory.ContextGenerator is DummyTokenizerFactory.DummyContextGenerator);
        ClassicAssert.AreEqual(pattern, factory.AlphaNumericPattern!.ToString());
        ClassicAssert.AreEqual(lang, factory.LanguageCode);
        ClassicAssert.AreEqual(lang, model.Language);
        ClassicAssert.IsTrue(factory.UseAlphaNumericOptmization);
    }

    [Test]
    public void TestCreateDummyFactory()
    {
        NOpenNLP.Tools.Dictionary.Dictionary dic = LoadAbbDictionary();
        const string lang = "spa";
        string pattern = "^[0-9A-Za-z]+$";

        // NOpenNLP: upstream passes Class.getCanonicalName(); the .NET counterpart
        // that ExtensionLoader resolves is the type's full name.
        TokenizerFactory factory = TokenizerFactory.Create(
            typeof(DummyTokenizerFactory).FullName, lang, dic, true, new Regex(pattern))!;

        ClassicAssert.IsTrue(factory.AbbreviationDictionary is DummyTokenizerFactory.DummyDictionary);
        ClassicAssert.IsTrue(factory.ContextGenerator is DummyTokenizerFactory.DummyContextGenerator);
        ClassicAssert.AreEqual(pattern, factory.AlphaNumericPattern!.ToString());
        ClassicAssert.AreEqual(lang, factory.LanguageCode);
        ClassicAssert.IsTrue(factory.UseAlphaNumericOptmization);
    }
}
