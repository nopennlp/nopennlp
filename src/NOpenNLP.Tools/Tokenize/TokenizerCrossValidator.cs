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

namespace NOpenNLP.Tools.Tokenize;

public class TokenizerCrossValidator
{
    // NOpenNLP: upstream names this field "params", which is a C# keyword;
    // renamed to match TrainerFactory's spelling of the same concept.
    private readonly TrainingParameters trainParams;

    private readonly FMeasure fmeasure = new FMeasure();

    // NOpenNLP: made readonly
    private readonly ITokenizerEvaluationMonitor?[]? listeners;

    private readonly TokenizerFactory factory;

    public TokenizerCrossValidator(TrainingParameters trainParams,
        TokenizerFactory factory, params ITokenizerEvaluationMonitor?[]? listeners)
    {
        this.trainParams = trainParams;
        this.listeners = listeners;
        this.factory = factory;
    }

    /// <summary>
    /// Starts the evaluation.
    /// </summary>
    /// <param name="samples">the data to train and test</param>
    /// <param name="nFolds">number of folds</param>
    /// <exception cref="IOException">IOException</exception>
    public virtual void Evaluate(IObjectStream<TokenSample?> samples, int nFolds)
    {
        CrossValidationPartitioner<TokenSample> partitioner = new(samples, nFolds);

        while (partitioner.HasNext)
        {
            CrossValidationPartitioner<TokenSample>.TrainingSampleStream trainingSampleStream =
                partitioner.Next();

            // Maybe throws IOException if temporary file handling fails ...
            TokenizerModel model = TokenizerME.Train(trainingSampleStream, this.factory, trainParams);

            TokenizerEvaluator evaluator = new(new TokenizerME(model), listeners);

            evaluator.Evaluate(trainingSampleStream.GetTestSampleStream());
            fmeasure.MergeInto(evaluator.FMeasure);
        }
    }

    public virtual FMeasure FMeasure => fmeasure;
}
