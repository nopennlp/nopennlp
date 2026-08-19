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
using System.IO;
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Tokenize;

public class DummyTokenizerFactory : TokenizerFactory
{
    private const string DUMMY_DICT = "dummy";
    private DummyDictionary? dict;

    public DummyTokenizerFactory()
    {
    }

    public DummyTokenizerFactory(string languageCode,
        NOpenNLP.Tools.Dictionary.Dictionary? abbreviationDictionary, bool useAlphaNumericOptimization,
        Regex alphaNumericPattern)
        : base(languageCode, abbreviationDictionary, useAlphaNumericOptimization,
            alphaNumericPattern)
    {
    }

    protected override void Init(string languageCode, NOpenNLP.Tools.Dictionary.Dictionary? abbreviationDictionary,
        bool useAlphaNumericOptimization, Regex alphaNumericPattern)
    {
        base.Init(languageCode, abbreviationDictionary, useAlphaNumericOptimization,
            alphaNumericPattern);
        this.dict = new DummyDictionary(abbreviationDictionary!);
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

    public override ITokenContextGenerator ContextGenerator =>
        new DummyContextGenerator(AbbreviationDictionary!.AsStringSet());

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
            artifactMap[DUMMY_DICT] = this.dict;
        return artifactMap;
    }

    public class DummyDictionarySerializer : IArtifactSerializer<DummyDictionary>
    {
        public DummyDictionary Create(Stream @in)
        {
            return new DummyDictionary(@in);
        }

        public void Serialize(DummyDictionary artifact, Stream @out)
        {
            artifact.Serialize(@out);
        }

        // NOpenNLP: the non-generic IArtifactSerializer members are explicitly
        // implemented here for the same reason as the ported serializers in
        // NOpenNLP.Tools.Util.Model -- default interface members are unavailable
        // on netstandard2.0.
        object? IArtifactSerializer.Create(Stream @in) => Create(@in);

        void IArtifactSerializer.Serialize(object artifact, Stream @out) =>
            Serialize((DummyDictionary)artifact, @out);
    }

    public class DummyDictionary : NOpenNLP.Tools.Dictionary.Dictionary
    {
        private readonly NOpenNLP.Tools.Dictionary.Dictionary indict; // NOpenNLP: made readonly

        public DummyDictionary(NOpenNLP.Tools.Dictionary.Dictionary dict)
        {
            this.indict = dict;
        }

        public DummyDictionary(Stream @in)
        {
            this.indict = new NOpenNLP.Tools.Dictionary.Dictionary(@in);
        }

        public override void Serialize(Stream @out)
        {
            indict.Serialize(@out);
        }

        public override ISet<string> AsStringSet()
        {
            return indict.AsStringSet();
        }

        public override Type ArtifactSerializerClass => typeof(DummyDictionarySerializer);
    }

    internal class DummyContextGenerator(ISet<string> inducedAbbreviations)
        : DefaultTokenContextGenerator(inducedAbbreviations)
    {
    }
}
