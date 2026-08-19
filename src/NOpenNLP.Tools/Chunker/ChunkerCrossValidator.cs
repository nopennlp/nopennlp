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
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Chunker;

public class ChunkerCrossValidator(
    string languageCode,
    TrainingParameters @params,
    ChunkerFactory factory,
    params IChunkerEvaluationMonitor?[]? listeners)
{
    private readonly FMeasure fmeasure = new(); // NOpenNLP: made readonly

    /// <summary>
    /// Starts the evaluation.
    /// </summary>
    /// <param name="samples">the data to train and test</param>
    /// <param name="nFolds">number of folds</param>
    /// <exception cref="IOException">IOException</exception>
    public virtual void Evaluate(IObjectStream<ChunkSample?> samples, int nFolds)
    {
        CrossValidationPartitioner<ChunkSample> partitioner = new(samples, nFolds);

        while (partitioner.HasNext)
        {
            var trainingSampleStream = partitioner.Next();

            var model = ChunkerME.Train(languageCode, trainingSampleStream, @params, factory);

            // do testing
            ChunkerEvaluator evaluator = new(new ChunkerME(model), listeners);

            evaluator.Evaluate(trainingSampleStream.GetTestSampleStream());

            fmeasure.MergeInto(evaluator.FMeasure);
        }
    }

    public virtual FMeasure FMeasure => fmeasure;
}
