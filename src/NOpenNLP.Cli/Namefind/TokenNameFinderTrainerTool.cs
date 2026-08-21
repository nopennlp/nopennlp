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
using System.CommandLine;
using System.IO;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Featuregen;
using NOpenNLP.Tools.Util.Model;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Cmdline.Namefind;

public sealed class TokenNameFinderTrainerTool : AbstractTrainerTool<NameSample?>
{
    private readonly Option<string?> type = TrainingParams.Type();
    private readonly Option<DirectoryInfo?> resources = TrainingParams.Resources();
    private readonly Option<FileInfo?> featuregen = TrainingParams.Featuregen();
    private readonly Option<string?> nameTypes = TrainingParams.NameTypes();
    private readonly Option<string> sequenceCodec = TrainingParams.SequenceCodec();
    private readonly Option<string?> factoryName = TrainingParams.Factory();
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();
    private readonly Option<FileInfo> model = ToolParams.ModelForTraining();

    /// <inheritdoc/>
    public override string ShortDescription => "trainer for the learnable name finder";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
        [type, resources, featuregen, nameTypes, sequenceCodec, factoryName, lang, @params, model];

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format) +
        OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    internal static byte[]? OpenFeatureGeneratorBytes(string? featureGenDescriptorFile)
    {
        if (featureGenDescriptorFile != null)
        {
            return OpenFeatureGeneratorBytes(new FileInfo(featureGenDescriptorFile));
        }

        return null;
    }

    public static byte[]? OpenFeatureGeneratorBytes(FileInfo? featureGenDescriptorFile)
    {
        byte[]? featureGeneratorBytes = null;

        // load descriptor file into memory
        if (featureGenDescriptorFile != null)
        {
            try
            {
                using Stream bytesIn = CmdLineUtil.OpenInFile(featureGenDescriptorFile);
                featureGeneratorBytes = ModelUtil.Read(bytesIn);
            }
            catch (IOException e)
            {
                throw new TerminateToolException(-1,
                    "IO error while reading training data or indexing data: " + e.Message, e);
            }
        }

        return featureGeneratorBytes;
    }

    /// <summary>
    /// Load the resources, such as dictionaries, by reading the feature xml descriptor
    /// and looking into the directory passed as argument.
    /// </summary>
    /// <param name="resourcePath">the directory in which the resources are to be found</param>
    /// <param name="featureGenDescriptor">the feature xml descriptor</param>
    /// <returns>
    /// a map consisting of the file name of the resource and its corresponding Object
    /// </returns>
    /// <exception cref="IOException">if a resource cannot be read</exception>
    public static IDictionary<string, object> LoadResources(DirectoryInfo? resourcePath,
        FileInfo? featureGenDescriptor)
    {
        IDictionary<string, object> resources = new JCG.Dictionary<string, object>();

        if (resourcePath != null)
        {
            var artifactSerializers = new JCG.Dictionary<string, IArtifactSerializer>();

            if (featureGenDescriptor != null)
            {
                using Stream xmlDescriptorIn = CmdLineUtil.OpenInFile(featureGenDescriptor);
                foreach (KeyValuePair<string, IArtifactSerializer> mapping in
                    GeneratorFactory.ExtractArtifactSerializerMappings(xmlDescriptorIn))
                {
                    artifactSerializers[mapping.Key] = mapping.Value;
                }
            }

            foreach (KeyValuePair<string, IArtifactSerializer> serializerMapping in artifactSerializers)
            {
                string resourceName = serializerMapping.Key;
                using Stream resourceIn = CmdLineUtil.OpenInFile(
                    new FileInfo(Path.Combine(resourcePath.FullName, resourceName)));
                resources[resourceName] = serializerMapping.Value.Create(resourceIn)!;
            }
        }

        return resources;
    }

    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        mlParams = CmdLineUtil.LoadTrainingParameters(parseResult.GetValue(@params), true);
        if (mlParams == null)
        {
            mlParams = new TrainingParameters();
        }

        FileInfo modelOutFile = parseResult.GetRequiredValue(model);

        FileInfo? featuregenFile = parseResult.GetValue(featuregen);

        byte[]? featureGeneratorBytes = OpenFeatureGeneratorBytes(featuregenFile);

        // TODO: Support Custom resources:
        //       Must be loaded into memory, or written to tmp file until descriptor
        //       is loaded which defines parses when model is loaded

        IDictionary<string, object> resourcesMap;
        try
        {
            resourcesMap = LoadResources(parseResult.GetValue(resources), featuregenFile);
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1, e.Message, e);
        }

        CmdLineUtil.CheckOutputFile("name finder model", modelOutFile);

        string? nameTypesValue = parseResult.GetValue(nameTypes);
        if (nameTypesValue != null)
        {
            string[] nameTypesArr = nameTypesValue.Split(',');
            sampleStream = new NameSampleTypeFilter(nameTypesArr, sampleStream!);
        }

        string? sequenceCodecImplName = parseResult.GetValue(sequenceCodec);

        if ("BIO".Equals(sequenceCodecImplName, StringComparison.Ordinal))
        {
            sequenceCodecImplName = typeof(BioCodec).FullName;
        }
        else if ("BILOU".Equals(sequenceCodecImplName, StringComparison.Ordinal))
        {
            sequenceCodecImplName = typeof(BilouCodec).FullName;
        }

        ISequenceCodec<string> codec =
            TokenNameFinderFactory.InstantiateSequenceCodec(sequenceCodecImplName);

        TokenNameFinderFactory nameFinderFactory;
        try
        {
            nameFinderFactory = TokenNameFinderFactory.Create(parseResult.GetValue(factoryName),
                featureGeneratorBytes, resourcesMap, codec);
        }
        catch (InvalidFormatException e)
        {
            throw new TerminateToolException(-1, e.Message, e);
        }

        var counters = new NameSampleCountersStream(sampleStream!);
        sampleStream = counters;

        TokenNameFinderModel nameFinderModel;
        try
        {
            nameFinderModel = NameFinderME.Train(parseResult.GetRequiredValue(lang),
                parseResult.GetValue(type), sampleStream, mlParams, nameFinderFactory);
        }
        catch (IOException e)
        {
            throw CreateTerminationIOException(e);
        }
        finally
        {
            try
            {
                sampleStream.Dispose();
            }
            catch (IOException)
            {
                // sorry that this can fail
            }
        }

        Console.WriteLine();
        counters.PrintSummary();
        Console.WriteLine();

        CmdLineUtil.WriteModel("name finder", modelOutFile, nameFinderModel);
    }
}
