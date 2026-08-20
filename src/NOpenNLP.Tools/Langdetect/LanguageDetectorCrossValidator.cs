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

namespace NOpenNLP.Tools.Langdetect;

/// <summary>
/// Cross validator for the language detector.
/// </summary>
public class LanguageDetectorCrossValidator
{
    private readonly TrainingParameters @params;

    private readonly Mean documentAccuracy = new(); // NOpenNLP: made readonly

    private readonly ILanguageDetectorEvaluationMonitor?[]? listeners; // NOpenNLP: made readonly

    private readonly LanguageDetectorFactory factory; // NOpenNLP: made readonly

    /// <summary>
    /// Creates a <see cref="LanguageDetectorCrossValidator"/>.
    /// </summary>
    public LanguageDetectorCrossValidator(TrainingParameters mlParams,
        LanguageDetectorFactory factory,
        params ILanguageDetectorEvaluationMonitor?[]? listeners)
    {
        this.@params = mlParams;
        this.listeners = listeners;
        this.factory = factory;
    }

    /// <summary>
    /// Starts the evaluation.
    /// </summary>
    /// <param name="samples">the data to train and test</param>
    /// <param name="nFolds">number of folds</param>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public virtual void Evaluate(IObjectStream<LanguageSample?> samples, int nFolds)
    {
        CrossValidationPartitioner<LanguageSample> partitioner = new(samples, nFolds);

        while (partitioner.HasNext)
        {
            CrossValidationPartitioner<LanguageSample>.TrainingSampleStream trainingSampleStream =
                partitioner.Next();

            LanguageDetectorModel model = LanguageDetectorME.Train(
                trainingSampleStream, @params, factory);

            LanguageDetectorEvaluator evaluator = new(
                new LanguageDetectorME(model), listeners);

            evaluator.Evaluate(trainingSampleStream.GetTestSampleStream());

            documentAccuracy.Add(evaluator.Accuracy, evaluator.DocumentCount);
        }
    }

    /// <summary>
    /// Retrieves the accuracy for all iterations.
    /// </summary>
    public virtual double DocumentAccuracy => documentAccuracy.Value;

    /// <summary>
    /// Retrieves the number of words which where validated over all iterations.
    /// The result is the amount of folds multiplied by the total number of words.
    /// </summary>
    public virtual long DocumentCount => documentAccuracy.Count;
}
