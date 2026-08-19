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

using NOpenNLP.Tools.Util.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace NOpenNLP.Tools.Sentdetect;

public class DummySentenceDetectorFactory : SentenceDetectorFactory
{
    private const string DUMMY_DICT = "dummy";
    private DummyDictionary? dict;

    public DummySentenceDetectorFactory()
    {
    }

    public DummySentenceDetectorFactory(string languageCode, bool useTokenEnd,
        NOpenNLP.Tools.Dictionary.Dictionary abbreviationDictionary, char[]? eosCharacters)
        : base(languageCode, useTokenEnd, abbreviationDictionary, eosCharacters)
    {
    }

    protected override void Init(string languageCode, bool useTokenEnd,
        NOpenNLP.Tools.Dictionary.Dictionary abbreviationDictionary, char[]? eosCharacters)
    {
        base.Init(languageCode, useTokenEnd, abbreviationDictionary, eosCharacters);
        this.dict = new DummyDictionary(abbreviationDictionary);
    }

    public override NOpenNLP.Tools.Dictionary.Dictionary? AbbreviationDictionary
    {
        get
        {
            if (this.dict == null && artifactProvider != null)
            {
                this.dict = artifactProvider.GetArtifact<DummyDictionary>(DUMMY_DICT);
            }

            return this.dict;
        }
    }

    public override ISDContextGenerator GetSDContextGenerator() =>
        new DummySDContextGenerator(((DummyDictionary)AbbreviationDictionary!)
            .AsStringSet(), EOSCharacters!);

    public override IEndOfSentenceScanner EndOfSentenceScanner =>
        new DummyEOSScanner(EOSCharacters!);

    public override IDictionary<string, IArtifactSerializer> CreateArtifactSerializersMap()
    {
        var serializers = base.CreateArtifactSerializersMap();
        serializers[DUMMY_DICT] = new DummyDictionarySerializer();
        return serializers;
    }

    public override IDictionary<string, object> CreateArtifactMap()
    {
        var artifactMap = base.CreateArtifactMap();
        if (this.dict != null)
        {
            artifactMap[DUMMY_DICT] = this.dict;
        }

        return artifactMap;
    }

    public class DummyDictionarySerializer : IArtifactSerializer<DummyDictionary>
    {
        public DummyDictionary Create(Stream @in) => new DummyDictionary(@in);

        public void Serialize(DummyDictionary artifact, Stream @out) => artifact.Serialize(@out);

        // NOpenNLP: upstream relies on a default interface implementation to bridge
        // the non-generic IArtifactSerializer; DIMs are unavailable on
        // netstandard2.0/net462, so the bridge is explicit here, as in
        // DictionarySerializer.
        object IArtifactSerializer.Create(Stream @in) => Create(@in);

        void IArtifactSerializer.Serialize(object artifact, Stream @out)
            => Serialize((DummyDictionary)artifact, @out);
    }

    public class DummyDictionary : NOpenNLP.Tools.Dictionary.Dictionary
    {
        private readonly NOpenNLP.Tools.Dictionary.Dictionary indict; // NOpenNLP: made readonly

        public DummyDictionary(NOpenNLP.Tools.Dictionary.Dictionary dict) => this.indict = dict;

        public DummyDictionary(Stream @in) => this.indict = new NOpenNLP.Tools.Dictionary.Dictionary(@in);

        public override void Serialize(Stream @out) => indict.Serialize(@out);

        public override ISet<string> AsStringSet() => indict.AsStringSet();

        public override Type ArtifactSerializerClass => typeof(DummyDictionarySerializer);
    }

    internal class DummySDContextGenerator(ISet<string> inducedAbbreviations, char[] eosCharacters)
        : DefaultSDContextGenerator(inducedAbbreviations, eosCharacters)
    {
    }

    internal class DummyEOSScanner(char[] eosCharacters)
        : DefaultEndOfSentenceScanner(eosCharacters)
    {
    }
}
