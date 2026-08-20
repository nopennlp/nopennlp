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
using System.Text;
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Parser;

/// <summary>
/// This is an abstract base class for <see cref="ParserModel"/> implementations.
/// </summary>
// TODO: Model should validate the artifact map
public class ParserModel : BaseModel
{
    private sealed class HeadRulesSerializer : IArtifactSerializer<Lang.En.HeadRules>
    {
        public Lang.En.HeadRules Create(Stream @in) =>
            // NOpenNLP: Java wraps the stream in an InputStreamReader/BufferedReader; the
            // StreamReader here is left open so the caller keeps ownership of the stream,
            // matching the IArtifactSerializer contract.
            new(new StreamReader(@in, new UTF8Encoding(false), false, 1024, leaveOpen: true));

        public void Serialize(Lang.En.HeadRules artifact, Stream @out)
        {
            // NOpenNLP: mirrors the StreamReader in Create above -- BOM-less UTF-8,
            // left open so the caller keeps ownership of the stream, matching the
            // IArtifactSerializer contract.
            artifact.Serialize(new StreamWriter(@out, new UTF8Encoding(false), 1024, leaveOpen: true));
        }

        // NOpenNLP: upstream relies on a default interface implementation to
        // bridge the non-generic IArtifactSerializer; DIMs are unavailable on
        // netstandard2.0/net462, so the bridge is explicit here.
        object IArtifactSerializer.Create(Stream @in) => Create(@in);

        void IArtifactSerializer.Serialize(object artifact, Stream @out) =>
            Serialize((Lang.En.HeadRules)artifact, @out);
    }

    private const string COMPONENT_NAME = "Parser";
    private const string BUILD_MODEL_ENTRY_NAME = "build.model";
    private const string CHECK_MODEL_ENTRY_NAME = "check.model";
    private const string ATTACH_MODEL_ENTRY_NAME = "attach.model";
    private const string PARSER_TAGGER_MODEL_ENTRY_NAME = "parsertager.postagger";
    private const string CHUNKER_TAGGER_MODEL_ENTRY_NAME = "parserchunker.chunker";
    private const string HEAD_RULES_MODEL_ENTRY_NAME = "head-rules.headrules";
    private const string PARSER_TYPE = "parser-type";

    public ParserModel(string languageCode, IMaxentModel buildModel, IMaxentModel checkModel,
        IMaxentModel? attachModel, POSModel parserTagger,
        ChunkerModel chunkerTagger, IHeadRules headRules,
        ParserType modelType, Dictionary<string, string> manifestInfoEntries)
        : base(COMPONENT_NAME, languageCode, manifestInfoEntries)
    {
        SetManifestProperty(PARSER_TYPE, modelType.ToString());

        artifactMap.Put(BUILD_MODEL_ENTRY_NAME, buildModel);

        artifactMap.Put(CHECK_MODEL_ENTRY_NAME, checkModel);

        if (ParserType.CHUNKING.Equals(modelType))
        {
            if (attachModel != null)
            {
                throw new ArgumentException("attachModel must be null for chunking parser!");
            }
        }
        else if (ParserType.TREEINSERT.Equals(modelType))
        {
            if (attachModel is null)
            {
                throw new ArgumentNullException(nameof(attachModel), "attachModel must not be null");
            }

            artifactMap.Put(ATTACH_MODEL_ENTRY_NAME, attachModel);
        }
        else
        {
            throw new InvalidOperationException($"Unknown ParserType '{modelType}'!");
        }

        artifactMap.Put(PARSER_TAGGER_MODEL_ENTRY_NAME, parserTagger);

        artifactMap.Put(CHUNKER_TAGGER_MODEL_ENTRY_NAME, chunkerTagger);

        artifactMap.Put(HEAD_RULES_MODEL_ENTRY_NAME, headRules);
        CheckArtifactMap();
    }

    public ParserModel(string languageCode, IMaxentModel buildModel, IMaxentModel checkModel,
        IMaxentModel? attachModel, POSModel parserTagger,
        ChunkerModel chunkerTagger, IHeadRules headRules, ParserType modelType)
        : this(languageCode, buildModel, checkModel, attachModel, parserTagger,
            chunkerTagger, headRules, modelType, null)
    {
    }

    public ParserModel(string languageCode, IMaxentModel buildModel, IMaxentModel checkModel,
        POSModel parserTagger, ChunkerModel chunkerTagger,
        IHeadRules headRules, ParserType type, Dictionary<string, string> manifestInfoEntries)
        : this(languageCode, buildModel, checkModel, null, parserTagger,
            chunkerTagger, headRules, type, manifestInfoEntries)
    {
    }

    public ParserModel(Stream @in)
        : base(COMPONENT_NAME, @in)
    {
    }

    public ParserModel(FileInfo modelFile)
        : base(COMPONENT_NAME, modelFile)
    {
    }

    // NOpenNLP: upstream takes a java.nio.file.Path here; a path string is the
    // natural .NET equivalent, matching the other ported models.
    public ParserModel(string modelPath)
        : this(new FileInfo(modelPath))
    {
    }

    // NOpenNLP: the URL overload has no .NET equivalent in BaseModel.
    // public ParserModel(Uri modelURL) : base(COMPONENT_NAME, modelURL)
    // {
    // }

    public override void CreateArtifactSerializers(IDictionary<string, IArtifactSerializer> serializers)
    {
        base.CreateArtifactSerializers(serializers);

        // In 1.6.x the headrules artifact is serialized with the new API
        // which uses the Serializeable interface
        // This change is not backward compatible with the 1.5.x models.
        // In order to laod 1.5.x model the English headrules serializer must be
        // put on the serializer map.

        if (Version is { Major: 1, Minor: 5 })
        {
            serializers.Put("headrules", new HeadRulesSerializer());
        }

        serializers.Put("postagger", new POSModelSerializer());
        serializers.Put("chunker", new ChunkerModelSerializer());
    }

    /// <summary>
    /// Retrieves the <see cref="ParserType"/> this model was trained for,
    /// or <c>null</c> if the manifest declares no known type.
    /// </summary>
    /// <remarks>NOpenNLP: this was <c>getParserType()</c> in Java.</remarks>
    public virtual ParserType? ParserTypeValue
    {
        get
        {
            string? parserType = GetManifestProperty(PARSER_TYPE);
            return parserType == null ? null : ParserTypeExtensions.Parse(parserType);
        }
    }

    public virtual IMaxentModel? BuildModel => GetArtifact<IMaxentModel>(BUILD_MODEL_ENTRY_NAME);

    public virtual IMaxentModel? CheckModel => GetArtifact<IMaxentModel>(CHECK_MODEL_ENTRY_NAME);

    public virtual IMaxentModel? AttachModel => GetArtifact<IMaxentModel>(ATTACH_MODEL_ENTRY_NAME);

    public virtual POSModel? ParserTaggerModel => GetArtifact<POSModel>(PARSER_TAGGER_MODEL_ENTRY_NAME);

    public virtual ChunkerModel? ParserChunkerModel => GetArtifact<ChunkerModel>(CHUNKER_TAGGER_MODEL_ENTRY_NAME);

    public virtual IHeadRules? HeadRules => GetArtifact<IHeadRules>(HEAD_RULES_MODEL_ENTRY_NAME);

    // TODO: Update model methods should make sure properties are copied correctly ...
    public virtual ParserModel UpdateBuildModel(IMaxentModel buildModel) =>
        new(Language, buildModel, CheckModel!, AttachModel,
            ParserTaggerModel!, ParserChunkerModel!, HeadRules!, ParserTypeValue!.Value);

    public virtual ParserModel UpdateCheckModel(IMaxentModel checkModel) =>
        new(Language, BuildModel!, checkModel, AttachModel,
            ParserTaggerModel!, ParserChunkerModel!, HeadRules!, ParserTypeValue!.Value);

    public virtual ParserModel UpdateTaggerModel(POSModel taggerModel) =>
        new(Language, BuildModel!, CheckModel!, AttachModel,
            taggerModel, ParserChunkerModel!, HeadRules!, ParserTypeValue!.Value);

    public virtual ParserModel UpdateChunkerModel(ChunkerModel chunkModel) =>
        new(Language, BuildModel!, CheckModel!, AttachModel,
            ParserTaggerModel!, chunkModel, HeadRules!, ParserTypeValue!.Value);

    protected override void ValidateArtifactMap()
    {
        base.ValidateArtifactMap();

        // NOpenNLP: Java's Map.get() returns null for an absent key, whereas the
        // .NET indexer throws KeyNotFoundException, so GetArtifact is used for
        // every lookup below.
        if (GetArtifact<object>(BUILD_MODEL_ENTRY_NAME) is not AbstractModel)
        {
            throw new InvalidFormatException("Missing the build model!");
        }

        ParserType? modelType = ParserTypeValue;

        if (modelType != null)
        {
            if (ParserType.CHUNKING.Equals(modelType))
            {
                if (GetArtifact<object>(ATTACH_MODEL_ENTRY_NAME) != null)
                    throw new InvalidFormatException("attachModel must be null for chunking parser!");
            }
            else if (ParserType.TREEINSERT.Equals(modelType))
            {
                if (GetArtifact<object>(ATTACH_MODEL_ENTRY_NAME) is not AbstractModel)
                    throw new InvalidFormatException("attachModel must not be null!");
            }
            else
            {
                throw new InvalidFormatException($"Unknown ParserType '{modelType}'!");
            }
        }
        else
        {
            throw new InvalidFormatException("Missing the parser type property!");
        }

        if (GetArtifact<object>(CHECK_MODEL_ENTRY_NAME) is not AbstractModel)
        {
            throw new InvalidFormatException("Missing the check model!");
        }

        if (GetArtifact<object>(PARSER_TAGGER_MODEL_ENTRY_NAME) is not POSModel)
        {
            throw new InvalidFormatException("Missing the tagger model!");
        }

        if (GetArtifact<object>(CHUNKER_TAGGER_MODEL_ENTRY_NAME) is not ChunkerModel)
        {
            throw new InvalidFormatException("Missing the chunker model!");
        }

        if (GetArtifact<object>(HEAD_RULES_MODEL_ENTRY_NAME) is not IHeadRules)
        {
            throw new InvalidFormatException("Missing the head rules!");
        }
    }
}
