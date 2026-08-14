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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using System;
using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Featuregen;

// TODO: (OPENNLP-1174) just remove when back-compat is no longer needed
[Obsolete("Obsolete")]
public class GeneratorFactoryClassicFormatTest
{
    [Test]
    public void TestCreationWithTokenClassFeatureGenerator()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestTokenClassFeatureGeneratorConfig_classic.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        AggregatedFeatureGenerator aggregatedGenerator =
            (AggregatedFeatureGenerator)GeneratorFactory.Create(generatorDescriptorIn, null);

        ClassicAssert.AreEqual(1, aggregatedGenerator.Generators.Count);
        // NOpenNLP: upstream compares Class.getName(); the ported counterpart is
        // the .NET type, so the generator's type is compared directly.
        IEnumerator<IAdaptiveFeatureGenerator> it = aggregatedGenerator.Generators.GetEnumerator();
        it.MoveNext();
        ClassicAssert.AreEqual(typeof(TokenClassFeatureGenerator), it.Current.GetType());
    }

    [Test]
    public void TestCreationWihtSimpleDescriptor()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestFeatureGeneratorConfig_classic.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        ICollection<Type> expectedGenerators = new List<Type>
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

    [Test]
    public void TestCreationWithCustomGenerator()
    {
        using Stream generatorDescriptorIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/CustomClassLoading_classic.xml");

        // If this fails the generator descriptor could not be found
        // at the expected location
        ClassicAssert.IsNotNull(generatorDescriptorIn);

        AggregatedFeatureGenerator aggregatedGenerator =
            (AggregatedFeatureGenerator)GeneratorFactory.Create(generatorDescriptorIn, null);

        ICollection<IAdaptiveFeatureGenerator> embeddedGenerator = aggregatedGenerator.Generators;

        ClassicAssert.AreEqual(1, embeddedGenerator.Count);

        foreach (IAdaptiveFeatureGenerator generator in embeddedGenerator)
        {
            ClassicAssert.AreEqual(typeof(TokenFeatureGenerator), generator.GetType());
        }
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
        Assert.Throws<InvalidFormatException>((Action)(() =>
        {
            using Stream descIn = TestResources.OpenResource(
                "/opennlp/tools/util/featuregen/FeatureGeneratorConfigWithUnkownElement_classic.xml");
            GeneratorFactory.Create(descIn, null);
        }));
    }

    [Test]
    public void TestArtifactToSerializerMappingExtraction()
    {
        // TODO: Define a new one here with custom elements ...
        using Stream descIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/CustomClassLoadingWithSerializers_classic.xml");

        IDictionary<string, IArtifactSerializer> mapping =
            GeneratorFactory.ExtractArtifactSerializerMappings(descIn);

        ClassicAssert.IsTrue(mapping["test.resource"]
            is WordClusterDictionary.WordClusterDictionarySerializer);
    }

    [Test]
    public void TestDictionaryArtifactToSerializerMappingExtraction()
    {
        using Stream descIn = TestResources.OpenResource(
            "/opennlp/tools/util/featuregen/TestDictionarySerializerMappingExtraction_classic.xml");

        IDictionary<string, IArtifactSerializer> mapping =
            GeneratorFactory.ExtractArtifactSerializerMappings(descIn);

        ClassicAssert.IsTrue(mapping["test.dictionary"] is DictionarySerializer);
        // TODO: if make the following effective, the test fails.
        // this is strange because DictionaryFeatureGeneratorFactory cast dictResource to Dictionary...
        //ClassicAssert.IsTrue(mapping["test.dictionary"] is
        //    NOpenNLP.Tools.Dictionary.Dictionary);
    }
}
