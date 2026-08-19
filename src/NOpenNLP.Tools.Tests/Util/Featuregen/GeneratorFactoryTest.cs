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
using System.Linq;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Featuregen;

public class GeneratorFactoryTest
{
    // NOpenNLP: referenced by TestParametersConfig.xml using an assembly-qualified
    // .NET type name, since ExtensionLoader.ResolveType only searches the library
    // assembly. Must stay public so Activator.CreateInstance can construct it.
    public class TestParametersFeatureGeneratorFactory
        : GeneratorFactory.AbstractXmlFeatureGeneratorFactory
    {
        public TestParametersFeatureGeneratorFactory()
            : base()
        {
        }

        public override IAdaptiveFeatureGenerator Create()
        {
            // NOpenNLP: upstream calls getInt/getFloat/getLong; the ported names
            // follow the C# convention of Int32/Single/Int64.
            return new TestParametersFeatureGenerator(
                GetInt32("intParam"),
                GetSingle("floatParam"),
                GetInt64("longParam"),
                GetDouble("doubleParam"),
                GetBool("boolParam"),
                GetStr("strParam"));
        }
    }

    public class TestParametersFeatureGenerator(int ip, float fp, long lp, double dp, bool bp, string sp)
        : IAdaptiveFeatureGenerator
    {
        public int Ip => ip;
        public float Fp => fp;
        public long Lp => lp;
        public double Dp => dp;
        public bool Bp => bp;
        public string Sp => sp;

        public void CreateFeatures(IList<string> features, string[] tokens, int index,
            string[] previousOutcomes)
        {
        }

        // NOpenNLP: netstandard2.0 has no default interface methods, so the
        // no-op adaptive members are explicit.
        public void UpdateAdaptiveData(string[] tokens, string[] outcomes)
        {
        }

        public void ClearAdaptiveData()
        {
        }
    }

    [Test]
    public void TestCreationWithTokenClassFeatureGenerator()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestTokenClassFeatureGeneratorConfig.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        AggregatedFeatureGenerator aggregatedGenerator =
            (AggregatedFeatureGenerator)GeneratorFactory.Create(generatorDescriptorIn, null);

        ClassicAssert.AreEqual(1, aggregatedGenerator.Generators.Count);
        // NOpenNLP: upstream compares Class.getName(); the ported counterpart is
        // the .NET type, so the generator's type is compared directly.
        ClassicAssert.AreEqual(typeof(TokenClassFeatureGenerator),
            aggregatedGenerator.Generators.First().GetType());
    }

    [Test]
    public void TestCreationWihtSimpleDescriptor()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestFeatureGeneratorConfig.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        ICollection<System.Type> expectedGenerators = new List<System.Type>
        {
            typeof(OutcomePriorFeatureGenerator),
        };

        AggregatedFeatureGenerator aggregatedGenerator =
            (AggregatedFeatureGenerator)GeneratorFactory.Create(generatorDescriptorIn, null);

        foreach (IAdaptiveFeatureGenerator generator in aggregatedGenerator.Generators)
        {
            expectedGenerators.Remove(generator.GetType());

            // if of kind which requires parameters check that
        }

        // If this fails not all expected generators were found and
        // removed from the expected generators collection
        ClassicAssert.AreEqual(0, expectedGenerators.Count);
    }

    /// <summary>
    /// Tests the creation from a descriptor which contains an unkown element.
    /// The creation should fail with an <see cref="InvalidFormatException"/>
    /// </summary>
    [Test]
    public void TestCreationWithUnkownElement()
    {
        // NOpenNLP: upstream declares @Test(expected = IOException.class), and
        // InvalidFormatException derives from IOException in both Java and the port.
        Assert.Throws<InvalidFormatException>((System.Action)(() =>
        {
            using Stream descIn = TestResources.OpenResource(
                "/opennlp/tools/util/featuregen/FeatureGeneratorConfigWithUnkownElement.xml");
            GeneratorFactory.Create(descIn, null);
        }));
    }

    [Test]
    public void TestDictionaryArtifactToSerializerMappingExtraction()
    {
        using Stream descIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestDictionarySerializerMappingExtraction.xml");

        IDictionary<string, IArtifactSerializer> mapping =
            GeneratorFactory.ExtractArtifactSerializerMappings(descIn);

        ClassicAssert.IsTrue(mapping["test.dictionary"] is DictionarySerializer);
        // TODO: if make the following effective, the test fails.
        // this is strange because DictionaryFeatureGeneratorFactory cast dictResource to Dictionary...
        //ClassicAssert.IsTrue(mapping["test.dictionary"] is
        //    NOpenNLP.Tools.Dictionary.Dictionary);
    }

    [Test]
    public void TestParameters()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestParametersConfig.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        IAdaptiveFeatureGenerator generator = GeneratorFactory.Create(generatorDescriptorIn, null);
        ClassicAssert.IsTrue(generator is TestParametersFeatureGenerator);

        TestParametersFeatureGenerator featureGenerator = (TestParametersFeatureGenerator)generator;
        ClassicAssert.AreEqual(123, featureGenerator.Ip);
        ClassicAssert.AreEqual(45, featureGenerator.Fp, 0.1);
        ClassicAssert.AreEqual(67890, featureGenerator.Lp);
        ClassicAssert.AreEqual(123456.789, featureGenerator.Dp, 0.1);
        ClassicAssert.IsTrue(featureGenerator.Bp);
        ClassicAssert.AreEqual("HELLO", featureGenerator.Sp);
    }

    [Test]
    public void TestNotAutomaticallyInsertAggregatedFeatureGenerator()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestNotAutomaticallyInsertAggregatedFeatureGenerator.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        IAdaptiveFeatureGenerator featureGenerator = GeneratorFactory.Create(generatorDescriptorIn, null);
        ClassicAssert.IsTrue(featureGenerator is OutcomePriorFeatureGenerator);
    }

    [Test]
    public void TestAutomaticallyInsertAggregatedFeatureGenerator()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestAutomaticallyInsertAggregatedFeatureGenerator.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        IAdaptiveFeatureGenerator featureGenerator = GeneratorFactory.Create(generatorDescriptorIn, null);
        ClassicAssert.IsTrue(featureGenerator is AggregatedFeatureGenerator);

        AggregatedFeatureGenerator aggregatedFeatureGenerator = (AggregatedFeatureGenerator)featureGenerator;
        ClassicAssert.AreEqual(3, aggregatedFeatureGenerator.Generators.Count);
        foreach (IAdaptiveFeatureGenerator afg in aggregatedFeatureGenerator.Generators)
        {
            ClassicAssert.IsTrue(afg is OutcomePriorFeatureGenerator);
        }
    }

    [Test]
    public void TestNotAutomaticallyInsertAggregatedFeatureGeneratorChild()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestNotAutomaticallyInsertAggregatedFeatureGeneratorCache.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        IAdaptiveFeatureGenerator featureGenerator = GeneratorFactory.Create(generatorDescriptorIn, null);
        ClassicAssert.IsTrue(featureGenerator is CachedFeatureGenerator);

        CachedFeatureGenerator cachedFeatureGenerator = (CachedFeatureGenerator)featureGenerator;
        ClassicAssert.IsTrue(cachedFeatureGenerator.CachedFeatureGeneratorValue
            is OutcomePriorFeatureGenerator);
    }

    [Test]
    public void TestAutomaticallyInsertAggregatedFeatureGeneratorChildren()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestAutomaticallyInsertAggregatedFeatureGeneratorCache.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        IAdaptiveFeatureGenerator featureGenerator = GeneratorFactory.Create(generatorDescriptorIn, null);
        ClassicAssert.IsTrue(featureGenerator is CachedFeatureGenerator);

        CachedFeatureGenerator cachedFeatureGenerator = (CachedFeatureGenerator)featureGenerator;
        IAdaptiveFeatureGenerator afg = cachedFeatureGenerator.CachedFeatureGeneratorValue;
        ClassicAssert.IsTrue(afg is AggregatedFeatureGenerator);

        AggregatedFeatureGenerator aggregatedFeatureGenerator = (AggregatedFeatureGenerator)afg;
        ClassicAssert.AreEqual(3, aggregatedFeatureGenerator.Generators.Count);
        foreach (IAdaptiveFeatureGenerator afgen in aggregatedFeatureGenerator.Generators)
        {
            ClassicAssert.IsTrue(afgen is OutcomePriorFeatureGenerator);
        }
    }

    [Test]
    public void TestInsertCachedFeatureGenerator()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestInsertCachedFeatureGenerator.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        IAdaptiveFeatureGenerator featureGenerator = GeneratorFactory.Create(generatorDescriptorIn, null);
        ClassicAssert.IsTrue(featureGenerator is CachedFeatureGenerator);
        CachedFeatureGenerator cachedFeatureGenerator = (CachedFeatureGenerator)featureGenerator;

        ClassicAssert.IsTrue(cachedFeatureGenerator.CachedFeatureGeneratorValue
            is AggregatedFeatureGenerator);
        AggregatedFeatureGenerator aggregatedFeatureGenerator =
            (AggregatedFeatureGenerator)cachedFeatureGenerator.CachedFeatureGeneratorValue;
        ClassicAssert.AreEqual(3, aggregatedFeatureGenerator.Generators.Count);
        foreach (IAdaptiveFeatureGenerator afg in aggregatedFeatureGenerator.Generators)
        {
            ClassicAssert.IsTrue(afg is OutcomePriorFeatureGenerator);
        }
    }
}
