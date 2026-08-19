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
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Sentdetect;

/// <summary>
/// A cross validator for the sentence detector.
/// </summary>
public class SDCrossValidator
{
    // NOpenNLP: made readonly
    private readonly string languageCode;

    // NOpenNLP: upstream names this field "params", which is a C# keyword;
    // renamed to match TrainerFactory's spelling of the same concept.
    private readonly TrainingParameters trainParams;

    private readonly FMeasure fmeasure = new FMeasure();

    // NOpenNLP: made readonly
    private readonly ISentenceDetectorEvaluationMonitor?[]? listeners;

    // NOpenNLP: made readonly
    private readonly SentenceDetectorFactory sdFactory;

    public SDCrossValidator(string languageCode, TrainingParameters trainParams,
        SentenceDetectorFactory sdFactory, params ISentenceDetectorEvaluationMonitor?[]? listeners)
    {
        this.languageCode = languageCode;
        this.trainParams = trainParams;
        this.listeners = listeners;
        this.sdFactory = sdFactory;
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    /// Deprecated: Use <c>SDCrossValidator(string, TrainingParameters, SentenceDetectorFactory,
    /// ISentenceDetectorEvaluationMonitor[])</c> and pass in a <see cref="SentenceDetectorFactory"/>.
    /// </remarks>
    public SDCrossValidator(string languageCode, TrainingParameters trainParams)
        : this(languageCode, trainParams, new SentenceDetectorFactory(languageCode, true, null!, null))
    {
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    /// Deprecated: Use <c>SDCrossValidator(string, TrainingParameters, SentenceDetectorFactory,
    /// ISentenceDetectorEvaluationMonitor[])</c> instead and pass in a <see cref="SentenceDetectorFactory"/>.
    /// </remarks>
    public SDCrossValidator(string languageCode, TrainingParameters trainParams,
        params ISentenceDetectorEvaluationMonitor?[]? listeners)
        : this(languageCode, trainParams, new SentenceDetectorFactory(languageCode, true, null!, null), listeners)
    {
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    /// Deprecated: Use <c>SDCrossValidator(string, TrainingParameters, SentenceDetectorFactory,
    /// ISentenceDetectorEvaluationMonitor[])</c> instead and pass in a <see cref="TrainingParameters"/> object.
    /// </remarks>
    public SDCrossValidator(string languageCode)
        : this(languageCode, ModelUtil.CreateDefaultTrainingParameters())
    {
    }

    /// <summary>
    /// Starts the evaluation.
    /// </summary>
    /// <param name="samples">the data to train and test</param>
    /// <param name="nFolds">number of folds</param>
    /// <exception cref="IOException">IOException</exception>
    public virtual void Evaluate(IObjectStream<SentenceSample?> samples, int nFolds)
    {
        CrossValidationPartitioner<SentenceSample> partitioner = new(samples, nFolds);

        while (partitioner.HasNext)
        {
            CrossValidationPartitioner<SentenceSample>.TrainingSampleStream trainingSampleStream =
                partitioner.Next();

            SentenceModel model = SentenceDetectorME.Train(languageCode, trainingSampleStream,
                sdFactory, trainParams);

            // do testing
            SentenceDetectorEvaluator evaluator = new(new SentenceDetectorME(model), listeners);

            evaluator.Evaluate(trainingSampleStream.GetTestSampleStream());

            fmeasure.MergeInto(evaluator.FMeasure);
        }
    }

    public virtual FMeasure FMeasure => fmeasure;
}
