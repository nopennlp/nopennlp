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
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Postag;

public class DummyPOSTaggerFactory : POSTaggerFactory
{
    private const string DUMMY_POSDICT = "DUMMY_POSDICT";
    private readonly DummyPOSDictionary? dict; // NOpenNLP: made readonly

    public DummyPOSTaggerFactory()
    {
    }

    public DummyPOSTaggerFactory(DummyPOSDictionary posDictionary)
        : base(null, null, null)
        => this.dict = posDictionary;

    public override ISequenceValidator<string> SequenceValidator =>
        new DummyPOSSequenceValidator();

    // NOpenNLP: upstream narrows the return type to DummyPOSDictionary, which C#
    // does not allow when overriding; the declared type stays ITagDictionary and the
    // test asserts on the runtime type instead, exactly as upstream's test does.
    public override ITagDictionary TagDictionary =>
        artifactProvider!.GetArtifact<DummyPOSDictionary>(DUMMY_POSDICT)!;

    public override IPOSContextGenerator POSContextGenerator =>
        new DummyPOSContextGenerator(this.ngramDictionary);

    public override IDictionary<string, IArtifactSerializer> CreateArtifactSerializersMap()
    {
        var serializers = base.CreateArtifactSerializersMap();

        serializers.Put(DUMMY_POSDICT, new DummyPOSDictionarySerializer());
        return serializers;
    }

    public override IDictionary<string, object> CreateArtifactMap()
    {
        var artifactMap = base.CreateArtifactMap();
        if (this.dict != null)
            artifactMap.Put(DUMMY_POSDICT, this.dict);
        return artifactMap;
    }

    internal class DummyPOSContextGenerator(NOpenNLP.Tools.Dictionary.Dictionary? dict)
        : DefaultPOSContextGenerator(dict)
    {
    }

    public class DummyPOSDictionarySerializer : IArtifactSerializer<DummyPOSDictionary>
    {
        public DummyPOSDictionary Create(Stream @in) =>
            DummyPOSDictionary.Create(new UncloseableInputStream(@in));

        public void Serialize(DummyPOSDictionary artifact, Stream @out) => artifact.Serialize(@out);

        // NOpenNLP: upstream relies on a default interface implementation to
        // bridge the non-generic IArtifactSerializer; DIMs are unavailable on
        // netstandard2.0/net462, so the bridge is explicit here.
        object IArtifactSerializer.Create(Stream @in) => Create(@in);

        void IArtifactSerializer.Serialize(object artifact, Stream @out) =>
            Serialize((DummyPOSDictionary)artifact, @out);
    }

    internal class DummyPOSSequenceValidator : ISequenceValidator<string>
    {
        public bool ValidSequence(int i, string[] inputSequence, string[] outcomesSequence,
            string outcome) => true;
    }

    public class DummyPOSDictionary : POSDictionary
    {
        private readonly POSDictionary? dict; // NOpenNLP: made readonly

        public DummyPOSDictionary()
        {
        }

        public DummyPOSDictionary(POSDictionary dict) => this.dict = dict;

        public static DummyPOSDictionary Create(UncloseableInputStream uncloseableInputStream) =>
            new(POSDictionary.Create(uncloseableInputStream));

        public override void Serialize(Stream @out) => dict!.Serialize(@out);

        public override string[]? GetTags(string word) => dict!.GetTags(word);

        public override Type ArtifactSerializerClass => typeof(DummyPOSDictionarySerializer);
    }
}
