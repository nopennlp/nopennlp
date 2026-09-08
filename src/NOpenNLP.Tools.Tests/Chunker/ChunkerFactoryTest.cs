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
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// Tests for the <see cref="ChunkerFactory"/> class.
/// </summary>
public class ChunkerFactoryTest
{
    private static IObjectStream<ChunkSample?> CreateSampleStream()
    {
        ResourceAsStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/chunker/test.txt");

        return new ChunkSampleStream(new PlainTextByLineStream(@in, Encoding.UTF8));
    }

    private static ChunkerModel TrainModel(ModelType type, ChunkerFactory factory) =>
        ChunkerME.Train("eng", CreateSampleStream(), TrainingParameters.DefaultParams(), factory);

    [Test]
    public void TestDefaultFactory()
    {
        ChunkerModel model = TrainModel(ModelType.MAXENT, new ChunkerFactory());

        ChunkerFactory factory = model.Factory;
        ClassicAssert.IsTrue(factory.ContextGenerator is DefaultChunkerContextGenerator);
        ClassicAssert.IsTrue(factory.SequenceValidator is DefaultChunkerSequenceValidator);

        MemoryStream @out = new MemoryStream();
        model.Serialize(@out);
        MemoryStream @in = new MemoryStream(@out.ToArray());

        ChunkerModel fromSerialized = new ChunkerModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsTrue(factory.ContextGenerator is DefaultChunkerContextGenerator);
        ClassicAssert.IsTrue(factory.SequenceValidator is DefaultChunkerSequenceValidator);
    }

    [Test]
    public void TestDummyFactory()
    {
        ChunkerModel model = TrainModel(ModelType.MAXENT, new DummyChunkerFactory());

        DummyChunkerFactory factory = (DummyChunkerFactory)model.Factory;
        ClassicAssert.IsTrue(factory.ContextGenerator is DummyChunkerFactory.DummyContextGenerator);
        ClassicAssert.IsTrue(factory.SequenceValidator is DummyChunkerFactory.DummySequenceValidator);

        MemoryStream @out = new MemoryStream();
        model.Serialize(@out);
        MemoryStream @in = new MemoryStream(@out.ToArray());

        ChunkerModel fromSerialized = new ChunkerModel(@in);

        factory = (DummyChunkerFactory)fromSerialized.Factory;

        // NOpenNLP: upstream asserts the round-tripped factory reverts to
        // DefaultChunkerContextGenerator/DefaultChunkerSequenceValidator, because Java
        // resolves the manifest's factory name against the classpath and the test's
        // DummyChunkerFactory is not reachable there, so BaseModel falls back to the
        // default factory. The port writes the factory's assembly-qualified type name
        // into the manifest and resolves it via ExtensionLoader, which searches the
        // loaded assemblies and does find DummyChunkerFactory. The custom factory
        // therefore survives serialization here, which is the behavior the port
        // intends, so the assertions check that the custom generators come back.
        ClassicAssert.IsTrue(factory.ContextGenerator is DummyChunkerFactory.DummyContextGenerator);
        ClassicAssert.IsTrue(factory.SequenceValidator is DummyChunkerFactory.DummySequenceValidator);

        ChunkerME chunker = new ChunkerME(model);

        string[] toks1 =
        [
            "Rockwell", "said", "the", "agreement", "calls", "for",
            "it", "to", "supply", "200", "additional", "so-called", "shipsets",
            "for", "the", "planes", "."
        ];

        string[] tags1 =
        [
            "NNP", "VBD", "DT", "NN", "VBZ", "IN", "PRP", "TO", "VB",
            "CD", "JJ", "JJ", "NNS", "IN", "DT", "NNS", "."
        ];

        chunker.Chunk(toks1, tags1);
    }
}
