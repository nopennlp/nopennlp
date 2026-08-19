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

using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;
using NOpenNLP.Tools.Util.Featuregen;
using System;
using System.Collections.Generic;
using System.IO;
using J2N;

namespace NOpenNLP.Tools.Namefind;

// Idea of this factory is that most resources/impls used by the name finder
// can be modified through this class!
// That only works if that's the central class used for training/runtime
public class TokenNameFinderFactory : BaseToolFactory
{
    private byte[]? featureGeneratorBytes;
    private IDictionary<string, object>? resources;
    private ISequenceCodec<string> seqCodec;

    /// <summary>
    /// Creates a <see cref="TokenNameFinderFactory"/> that provides the default implementation
    /// of the resources.
    /// </summary>
    public TokenNameFinderFactory()
    {
        this.seqCodec = new BioCodec();
    }

    public TokenNameFinderFactory(byte[]? featureGeneratorBytes, IDictionary<string, object>? resources,
        ISequenceCodec<string> seqCodec)
    {
        Init(featureGeneratorBytes, resources, seqCodec);
    }

    public virtual void Init(byte[]? featureGeneratorBytes, IDictionary<string, object>? resources,
        ISequenceCodec<string> seqCodec)
    {
        this.featureGeneratorBytes = featureGeneratorBytes;
        this.resources = resources;
        this.seqCodec = seqCodec;
    }

    private static byte[] LoadDefaultFeatureGeneratorBytes()
    {
        ByteArrayOutputStream bytes = new ByteArrayOutputStream();
        try
        {
            // NOpenNLP: upstream resolves the classpath path
            // "/opennlp/tools/namefind/ner-default-features.xml". The .NET
            // counterpart is an embedded resource, and J2N resolves a bare file
            // name relative to the requesting type's namespace, so the file lives
            // beside this class and is named without a path.
            using var @in = typeof(TokenNameFinderFactory).FindAndGetManifestResourceStream("ner-default-features.xml");

            if (@in == null)
            {
                throw new InvalidOperationException("Classpath must contain ner-default-features.xml file!");
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
            throw new InvalidOperationException("Failed reading from ner-default-features.xml file on classpath!");
        }

        return bytes.ToArray();
    }

    public virtual ISequenceCodec<string> SequenceCodec => seqCodec;

    // NOpenNLP: upstream declares these `protected`, which in Java also grants
    // access to the rest of the package -- NameFinderME.train() calls them.
    // `protected internal` is the C# equivalent of that reach.
    protected internal virtual IDictionary<string, object>? Resources => resources;

    protected internal virtual byte[]? FeatureGenerator => featureGeneratorBytes;

    public static TokenNameFinderFactory Create(string? subclassName, byte[]? featureGeneratorBytes,
        IDictionary<string, object>? resources, ISequenceCodec<string> seqCodec)
    {
        TokenNameFinderFactory theFactory;
        if (subclassName == null)
        {
            // will create the default factory
            theFactory = new TokenNameFinderFactory();
        }
        else
        {
            try
            {
                theFactory = ExtensionLoader.InstantiateExtension<TokenNameFinderFactory>(subclassName);
            }
            catch (Exception e)
            {
                string msg = $"Could not instantiate the {subclassName}. The initialization throw an exception.";
                Console.Error.WriteLine(msg);
                e.PrintStackTrace();
                throw new InvalidFormatException(msg, e);
            }
        }

        theFactory.Init(featureGeneratorBytes, resources, seqCodec);
        return theFactory;
    }

    public override void ValidateArtifactMap()
    {
    }

    public virtual ISequenceCodec<string> CreateSequenceCodec()
    {
        if (artifactProvider != null)
        {
            string sequeceCodecImplName = artifactProvider.GetManifestProperty(TokenNameFinderModel.SEQUENCE_CODEC_CLASS_NAME_PARAMETER);
            return InstantiateSequenceCodec(sequeceCodecImplName);
        }
        else
        {
            return seqCodec;
        }
    }

    public virtual INameContextGenerator CreateContextGenerator()
    {
        IAdaptiveFeatureGenerator? featureGenerator = CreateFeatureGenerators();
        if (featureGenerator == null)
        {
            featureGenerator = new CachedFeatureGenerator(
                new WindowFeatureGenerator(new TokenFeatureGenerator(), 2, 2),
                new WindowFeatureGenerator(new TokenClassFeatureGenerator(true), 2, 2),
                new OutcomePriorFeatureGenerator(),
                new PreviousMapFeatureGenerator(),
                new BigramNameFeatureGenerator(),
                new SentenceFeatureGenerator(true, false));
        }

        return new DefaultNameContextGenerator(featureGenerator);
    }

    /// <summary>
    /// Creates the <see cref="IAdaptiveFeatureGenerator"/>. Usually this
    /// is a set of generators contained in the <see cref="AggregatedFeatureGenerator"/>.
    ///
    /// Note:
    /// The generators are created on every call to this method.
    /// </summary>
    /// <returns>the feature generator or null if there is no descriptor in the model</returns>
    public virtual IAdaptiveFeatureGenerator? CreateFeatureGenerators()
    {
        if (featureGeneratorBytes == null && artifactProvider != null)
        {
            featureGeneratorBytes = artifactProvider.GetArtifact<byte[]>(TokenNameFinderModel.GENERATOR_DESCRIPTOR_ENTRY_NAME);
        }

        if (featureGeneratorBytes == null)
        {
            featureGeneratorBytes = LoadDefaultFeatureGeneratorBytes();
        }

        var descriptorIn = new MemoryStream(featureGeneratorBytes);
        IAdaptiveFeatureGenerator? generator;
        try
        {
            generator = GeneratorFactory.Create(descriptorIn, key =>
            {
                if (artifactProvider != null)
                {
                    return artifactProvider.GetArtifact<IAdaptiveFeatureGenerator>(key);
                }
                else
                {
                    return resources?[key];
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
            throw new TokenNameFinderModel.FeatureGeneratorCreationError(e);
        }
        catch (IOException e)
        {
            throw new InvalidOperationException("Reading from mem cannot result in an I/O error", e);
        }

        return generator;
    }

    public static ISequenceCodec<string> InstantiateSequenceCodec(string? sequenceCodecImplName)
    {
        if (sequenceCodecImplName != null)
        {
            return ExtensionLoader.InstantiateExtension<ISequenceCodec<string>>(sequenceCodecImplName);
        }
        else
        {
            // If nothing is specified return old default!
            return new BioCodec();
        }
    }
}
