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

using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;
using NOpenNLP.Tools.Util.Featuregen;
using NOpenNLP.Tools.Util.Model;
using NOpenNLP.Tools.Support;
using J2N;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// The factory that provides POS Tagger default implementations and resources
/// </summary>
public class POSTaggerFactory : BaseToolFactory
{
    private const string TAG_DICTIONARY_ENTRY_NAME = "tags.tagdict";
    private const string NGRAM_DICTIONARY_ENTRY_NAME = "ngram.dictionary";
    protected NOpenNLP.Tools.Dictionary.Dictionary ngramDictionary;
    private byte[] featureGeneratorBytes;
    private Dictionary<string, object> resources;
    protected ITagDictionary posDictionary;

    /// <summary>
    /// Creates a <see cref="POSTaggerFactory"/> that provides the default implementation
    /// of the resources.
    /// </summary>
    public POSTaggerFactory()
    {
    }

    /// <summary>
    /// Creates a <see cref="POSTaggerFactory"/>. Use this constructor to
    /// programmatically create a factory.
    /// </summary>
    /// <param name="ngramDictionary"></param>
    /// <param name="posDictionary"></param>
    /// <remarks>
    /// Deprecated: This constructor is here for backward compatibility and
    ///             is not functional anymore in the training of 1.8.x series models
    /// </remarks>
    public POSTaggerFactory(NOpenNLP.Tools.Dictionary.Dictionary ngramDictionary, ITagDictionary posDictionary)
    {
        this.Init(ngramDictionary, posDictionary); // TODO: This could be made functional by creating some default feature generation
        // which uses the dictionary ...
    }

    public POSTaggerFactory(byte[]? featureGeneratorBytes, Dictionary<string, object> resources, ITagDictionary posDictionary)
    {
        this.featureGeneratorBytes = featureGeneratorBytes ?? LoadDefaultFeatureGeneratorBytes();

        this.resources = resources;
        this.posDictionary = posDictionary;
    }

    protected virtual void Init(NOpenNLP.Tools.Dictionary.Dictionary ngramDictionary, ITagDictionary posDictionary)
    {
        this.ngramDictionary = ngramDictionary;
        this.posDictionary = posDictionary;
    }

    protected virtual void Init(byte[] featureGeneratorBytes, Dictionary<string, object> resources, ITagDictionary posDictionary)
    {
        this.featureGeneratorBytes = featureGeneratorBytes;
        this.resources = resources;
        this.posDictionary = posDictionary;
    }

    private static byte[] LoadDefaultFeatureGeneratorBytes()
    {
        ByteArrayOutputStream bytes = new ByteArrayOutputStream();
        try
        {
            // NOpenNLP: upstream resolves the classpath path
            // "/opennlp/tools/postag/pos-default-features.xml". The .NET
            // counterpart is an embedded resource, and J2N resolves a bare file
            // name relative to the requesting type's namespace, so the file lives
            // beside this class and is named without a path. The requesting type
            // must be POSTaggerFactory; TokenNameFinderFactory would resolve
            // against the Namefind namespace and never find this resource.
            using var @in = typeof(POSTaggerFactory).FindAndGetManifestResourceStream("pos-default-features.xml");

            if (@in == null)
            {
                throw new InvalidOperationException("Classpath must contain pos-default-features.xml file!");
            }

            byte[] buf = new byte[1024];
            int len;
            while ((len = @in.Read(buf, 0, buf.Length)) > 0)
            {
                bytes.Write(buf, 0, len);
            }
        }
        catch (IOException e)
        {
            throw new InvalidOperationException("Failed reading from pos-default-features.xml file on classpath!");
        }

        return bytes.ToArray();
    }

    /// <summary>
    /// Creates the <see cref="IAdaptiveFeatureGenerator"/>. Usually this
    /// is a set of generators contained in the <see cref="AggregatedFeatureGenerator"/>.
    ///
    /// Note:
    /// The generators are created on every call to this method.
    /// </summary>
    /// <returns>the feature generator or null if there is no descriptor in the model</returns>
    public virtual IAdaptiveFeatureGenerator CreateFeatureGenerators()
    {
        if (featureGeneratorBytes == null && artifactProvider != null)
        {
            featureGeneratorBytes = artifactProvider.GetArtifact<byte[]>(POSModel.GENERATOR_DESCRIPTOR_ENTRY_NAME);
        }

        if (featureGeneratorBytes == null)
        {
            featureGeneratorBytes = LoadDefaultFeatureGeneratorBytes();
        }

        Stream descriptorIn = new MemoryStream(featureGeneratorBytes);
        IAdaptiveFeatureGenerator generator;
        try
        {
            generator = GeneratorFactory.Create(descriptorIn, (key) =>
            {
                if (artifactProvider != null)
                {
                    return artifactProvider.GetArtifact<object>(key);
                }
                else
                {
                    return resources[key];
                }
            });
        }
        catch (InvalidFormatException e)
        {

            // It is assumed that the creation of the feature generation does not
            // fail after it succeeded once during model loading.
            // But it might still be possible that such an exception is thrown,
            // in this case the caller should not be forced to handle the exception
            // and a Runtime Exception is thrown instead.
            // If the re-creation of the feature generation fails it is assumed
            // that this can only be caused by a programming mistake and therefore
            // throwing a Runtime Exception is reasonable
            throw new InvalidOperationException(); // FeatureGeneratorCreationError(e);
        }
        catch (IOException e)
        {
            throw new InvalidOperationException("Reading from mem cannot result in an I/O error", e);
        }

        return generator;
    }

    public override IDictionary<string, IArtifactSerializer> CreateArtifactSerializersMap()
    {
        var serializers = base.CreateArtifactSerializersMap();

        // NOTE: This is only needed for old models and this if can be removed if support is dropped
        POSDictionarySerializer.Register(serializers);
        return serializers;
    }

    public override IDictionary<string, object> CreateArtifactMap()
    {
        var artifactMap = base.CreateArtifactMap();
        if (posDictionary != null)
            artifactMap.Put(TAG_DICTIONARY_ENTRY_NAME, posDictionary);
        if (ngramDictionary != null)
            artifactMap.Put(NGRAM_DICTIONARY_ENTRY_NAME, ngramDictionary);
        return artifactMap;
    }

    public virtual ITagDictionary CreateTagDictionary(FileInfo dictionary)
    {
        return CreateTagDictionary(dictionary.OpenRead());
    }

    public virtual ITagDictionary CreateTagDictionary(Stream @in)
    {
        return POSDictionary.Create(@in);
    }

    public virtual void SetTagDictionary(ITagDictionary dictionary)
    {
        if (artifactProvider != null)
        {
            throw new InvalidOperationException("Can not set tag dictionary while using artifact provider.");
        }

        this.posDictionary = dictionary;
    }

    protected virtual Dictionary<string, object> GetResources()
    {
        if (resources != null)
        {
            return resources;
        }

        return new Dictionary<string, object>();
    }

    protected virtual byte[] GetFeatureGenerator()
    {
        return featureGeneratorBytes;
    }

    public virtual ITagDictionary GetTagDictionary()
    {
        if (this.posDictionary == null && artifactProvider != null)
            this.posDictionary = artifactProvider.GetArtifact<ITagDictionary>(TAG_DICTIONARY_ENTRY_NAME);
        return this.posDictionary;
    }

    /// <summary>
    /// </summary>
    /// <remarks>Deprecated: This will be reduced in visibility and later removed</remarks>
    public virtual NOpenNLP.Tools.Dictionary.Dictionary GetDictionary()
    {
        if (this.ngramDictionary == null && artifactProvider != null)
            this.ngramDictionary = artifactProvider.GetArtifact<NOpenNLP.Tools.Dictionary.Dictionary>(NGRAM_DICTIONARY_ENTRY_NAME);
        return this.ngramDictionary;
    }

    public virtual void SetDictionary(NOpenNLP.Tools.Dictionary.Dictionary ngramDict)
    {
        if (artifactProvider != null)
        {
            throw new InvalidOperationException("Can not set ngram dictionary while using artifact provider.");
        }

        this.ngramDictionary = ngramDict;
    }

    public virtual IPOSContextGenerator GetPOSContextGenerator()
    {
        return GetPOSContextGenerator(0);
    }

    public virtual IPOSContextGenerator GetPOSContextGenerator(int cacheSize)
    {
        if (artifactProvider != null)
        {
            Properties manifest = artifactProvider.GetArtifact<Properties>("manifest.properties");
            string version = manifest.GetProperty("OpenNLP-Version");
            if (Util.Version.Parse(version).Minor < 8)
            {
                return new DefaultPOSContextGenerator(cacheSize, GetDictionary());
            }
        }

        return new ConfigurablePOSContextGenerator(cacheSize, CreateFeatureGenerators());
    }

    public virtual ISequenceValidator<string> GetSequenceValidator()
    {
        return new DefaultPOSSequenceValidator(GetTagDictionary());
    }

    // TODO: This should not be done anymore for 8 models, they can just
    // use the ISerializableArtifact interface
    public class POSDictionarySerializer : IArtifactSerializer<POSDictionary>
    {
        public virtual POSDictionary Create(Stream @in)
        {
            return POSDictionary.Create(new UncloseableInputStream(@in));
        }

        // NOpenNLP: serialization is not supported; inference only.
        // public virtual void Serialize(POSDictionary artifact, Stream @out)
        // {
        //     artifact.Serialize(@out);
        // }

        internal static void Register(IDictionary<string, IArtifactSerializer> factories)
        {
            factories.Put("tagdict", new POSDictionarySerializer());
        }
        // NOpenNLP: upstream relies on a default interface implementation to
        // bridge the non-generic IArtifactSerializer; DIMs are unavailable on
        // netstandard2.0/net462, so the bridge is explicit here.
        object IArtifactSerializer.Create(Stream @in) => Create(@in);
    }

    protected virtual void ValidatePOSDictionary(POSDictionary posDict, AbstractModel posModel)
    {
        HashSet<string> dictTags = [];
        foreach (string word in posDict)
        {
            dictTags.UnionWith(posDict.GetTags(word));
        }

        HashSet<string> modelTags = [];
        for (int i = 0; i < posModel.NumOutcomes; i++)
        {
            modelTags.Add(posModel.GetOutcome(i));
        }

        if (!dictTags.IsSubsetOf(modelTags))
        {
            StringBuilder unknownTag = new StringBuilder();
            foreach (string d in dictTags)
            {
                if (!modelTags.Contains(d))
                {
                    unknownTag.Append(d).Append(" ");
                }
            }

            throw new InvalidFormatException("Tag dictionary contains tags " + "which are unknown by the model! The unknown tags are: " + unknownTag);
        }
    }

    public override void ValidateArtifactMap()
    {

        // Ensure that the tag dictionary is compatible with the model
        object tagdictEntry = this.artifactProvider.GetArtifact<object>(TAG_DICTIONARY_ENTRY_NAME);
        if (tagdictEntry != null)
        {
            if (tagdictEntry is POSDictionary)
            {
                if (!this.artifactProvider.IsLoadedFromSerialized())
                {
                    AbstractModel posModel = this.artifactProvider.GetArtifact<AbstractModel>(POSModel.POS_MODEL_ENTRY_NAME);
                    POSDictionary posDict = (POSDictionary)tagdictEntry;
                    ValidatePOSDictionary(posDict, posModel);
                }
            }
            else
            {
                throw new InvalidFormatException("POSTag dictionary has wrong type!");
            }
        }

        object ngramDictEntry = this.artifactProvider.GetArtifact<object>(NGRAM_DICTIONARY_ENTRY_NAME);
        if (ngramDictEntry != null && !(ngramDictEntry is NOpenNLP.Tools.Dictionary.Dictionary))
        {
            throw new InvalidFormatException("NGram dictionary has wrong type!");
        }
    }

    public static POSTaggerFactory Create(string subclassName, NOpenNLP.Tools.Dictionary.Dictionary ngramDictionary, ITagDictionary posDictionary)
    {
        if (subclassName == null)
        {

            // will create the default factory
            return new POSTaggerFactory(ngramDictionary, posDictionary);
        }

        try
        {
            POSTaggerFactory theFactory = ExtensionLoader.InstantiateExtension<POSTaggerFactory>(subclassName);
            theFactory.Init(ngramDictionary, posDictionary);
            return theFactory;
        }
        catch (Exception e)
        {
            string msg = "Could not instantiate the " + subclassName + ". The initialization throw an exception.";
            throw new InvalidFormatException(msg, e);
        }
    }

    public static POSTaggerFactory Create(string subclassName, byte[] featureGeneratorBytes, Dictionary<string, object> resources, ITagDictionary posDictionary)
    {
        POSTaggerFactory theFactory;
        if (subclassName == null)
        {

            // will create the default factory
            theFactory = new POSTaggerFactory(null, posDictionary);
        }
        else
        {
            try
            {
                theFactory = ExtensionLoader.InstantiateExtension<POSTaggerFactory>(subclassName);
            }
            catch (Exception e)
            {
                string msg = "Could not instantiate the " + subclassName + ". The initialization throw an exception.";
                throw new InvalidFormatException(msg, e);
            }
        }

        theFactory.Init(featureGeneratorBytes, resources, posDictionary);
        return theFactory;
    }

    public virtual ITagDictionary CreateEmptyTagDictionary()
    {
        this.posDictionary = new POSDictionary(true);
        return this.posDictionary;
    }
}
