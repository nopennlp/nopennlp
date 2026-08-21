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
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Parser;
using NOpenNLP.Tools.Parser.Chunking;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using OpenNlpDictionary = NOpenNLP.Tools.Dictionary.Dictionary;

namespace NOpenNLP.Tools.Cmdline.Parser;

// trains a new check model ...
public sealed class CheckModelUpdaterTool : ModelUpdaterTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "trains and updates the check model in a parser model";

    /// <inheritdoc/>
    protected override ParserModel TrainAndUpdate(ParserModel originalModel,
        IObjectStream<Parse?> parseSamples)
    {
        OpenNlpDictionary? mdict = ParserTrainerTool.BuildDictionary(parseSamples,
            originalModel.HeadRules!, 5);

        parseSamples.Reset();

        // TODO: Maybe that should be part of the ChunkingParser ...
        // Training build
        Console.WriteLine("Training check model");
        IObjectStream<Event?> bes = new ParserEventStream(parseSamples, originalModel.HeadRules!,
            ParserEventTypeEnum.CHECK, mdict);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(
            ModelUtil.CreateDefaultTrainingParameters(), null);
        IMaxentModel checkModel = trainer.Train(bes);

        parseSamples.Dispose();

        return originalModel.UpdateCheckModel(checkModel);
    }
}
