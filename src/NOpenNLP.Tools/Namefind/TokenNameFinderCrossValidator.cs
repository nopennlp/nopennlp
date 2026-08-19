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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Eval;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

public class TokenNameFinderCrossValidator
{
    // NOpenNLP: upstream implements java.io.Serializable, which has no .NET
    // counterpart the port needs.
    private sealed class DocumentSample(NameSample[] samples)
    {
        public NameSample[] Samples => samples;
    }

    /// <summary>
    /// Reads Name Samples to group them as a document based on the clear adaptive data flag.
    /// </summary>
    private sealed class NameToDocumentSampleStream(IObjectStream<NameSample?> samples)
        : FilterObjectStream<NameSample?, DocumentSample?>(samples)
    {
        private NameSample? beginSample;

        /// <inheritdoc/>
        /// <exception cref="IOException">if there is an error during reading</exception>
        public override DocumentSample? Read()
        {
            JCG.List<NameSample> document = [];

            // Assume that the clear flag is set
            beginSample ??= this.samples.Read();

            // Underlying stream is exhausted!
            if (beginSample == null)
            {
                return null;
            }

            document.Add(beginSample);

            NameSample? sample;
            while ((sample = this.samples.Read()) != null)
            {
                if (sample.IsClearAdaptiveDataSet)
                {
                    beginSample = sample;
                    break;
                }

                document.Add(sample);
            }

            // Underlying stream is exhausted,
            // next call must return null
            if (sample == null)
            {
                beginSample = null;
            }

            return new DocumentSample([.. document]);
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();

            beginSample = null;
        }
    }

    /// <summary>
    /// Splits <see cref="DocumentSample"/> into <see cref="NameSample"/>s.
    /// </summary>
    private sealed class DocumentToNameSampleStream(IObjectStream<DocumentSample?> samples)
        : FilterObjectStream<DocumentSample?, NameSample?>(samples)
    {
        // NOpenNLP: upstream holds an Iterator here and drives it with hasNext()/next().
        // An IEnumerator is the equivalent shape when the algorithm must advance by hand.
        private IEnumerator<NameSample> documentSamples = Enumerable.Empty<NameSample>().GetEnumerator();

        /// <inheritdoc/>
        /// <exception cref="IOException">if there is an error during reading</exception>
        public override NameSample? Read()
        {
            // Note: Empty document samples should be skipped

            // NOpenNLP specific: tail-recursive call replaced with a loop
            while (true)
            {
                if (documentSamples.MoveNext())
                {
                    return documentSamples.Current;
                }

                DocumentSample? docSample = this.samples.Read();

                if (docSample != null)
                {
                    documentSamples = ((IEnumerable<NameSample>)docSample.Samples).GetEnumerator();
                }
                else
                {
                    return null;
                }
            }
        }
    }

    private readonly string languageCode;
    private readonly TrainingParameters params_;
    private readonly string? type;
    private readonly byte[]? featureGeneratorBytes; // NOpenNLP: made readonly
    private readonly IDictionary<string, object>? resources; // NOpenNLP: made readonly
    private readonly ITokenNameFinderEvaluationMonitor?[]? listeners; // NOpenNLP: made readonly

    private readonly FMeasure fmeasure = new(); // NOpenNLP: made readonly
    private readonly TokenNameFinderFactory? factory; // NOpenNLP: made readonly

    /// <summary>
    /// Name finder cross validator
    /// </summary>
    /// <param name="languageCode">the language of the training data</param>
    /// <param name="type">null or an override type for all types in the training data</param>
    /// <param name="trainParams">machine learning train parameters</param>
    /// <param name="featureGeneratorBytes">descriptor to configure the feature generation or null</param>
    /// <param name="resources">the resources for the name finder or null if none</param>
    /// <param name="codec">the sequence codec</param>
    /// <param name="listeners">a list of listeners</param>
    public TokenNameFinderCrossValidator(string languageCode, string? type,
        TrainingParameters trainParams, byte[]? featureGeneratorBytes,
        IDictionary<string, object>? resources, ISequenceCodec<string> codec,
        params ITokenNameFinderEvaluationMonitor?[]? listeners)
    {
        this.languageCode = languageCode;
        this.type = type;
        this.featureGeneratorBytes = featureGeneratorBytes;
        this.resources = resources;
        params_ = trainParams;
        this.listeners = listeners;
    }

    public TokenNameFinderCrossValidator(string languageCode, string? type,
        TrainingParameters trainParams, byte[]? featureGeneratorBytes,
        IDictionary<string, object>? resources,
        params ITokenNameFinderEvaluationMonitor?[]? listeners)
        : this(languageCode, type, trainParams, featureGeneratorBytes, resources, new BioCodec(), listeners)
    {
    }

    public TokenNameFinderCrossValidator(string languageCode, string? type,
        TrainingParameters trainParams, TokenNameFinderFactory factory,
        params ITokenNameFinderEvaluationMonitor?[]? listeners)
    {
        this.languageCode = languageCode;
        this.type = type;
        params_ = trainParams;
        this.factory = factory;
        this.listeners = listeners;
    }

    /// <summary>
    /// Starts the evaluation.
    /// </summary>
    /// <param name="samples">the data to train and test</param>
    /// <param name="nFolds">number of folds</param>
    /// <exception cref="IOException">IOException</exception>
    public virtual void Evaluate(IObjectStream<NameSample?> samples, int nFolds)
    {
        // Note: The name samples need to be grouped on a document basis.

        CrossValidationPartitioner<DocumentSample> partitioner = new(
            new NameToDocumentSampleStream(samples), nFolds);

        while (partitioner.HasNext)
        {
            var trainingSampleStream = partitioner.Next();

            TokenNameFinderModel model;
            if (factory != null)
            {
                model = NameFinderME.Train(languageCode, type,
                    new DocumentToNameSampleStream(trainingSampleStream), params_, factory);
            }
            else
            {
                model = NameFinderME.Train(languageCode, type,
                    new DocumentToNameSampleStream(trainingSampleStream), params_,
                    TokenNameFinderFactory.Create(null, featureGeneratorBytes, resources, new BioCodec()));
            }

            // do testing
            TokenNameFinderEvaluator evaluator = new(new NameFinderME(model), listeners);

            evaluator.Evaluate(new DocumentToNameSampleStream(trainingSampleStream.GetTestSampleStream()));

            fmeasure.MergeInto(evaluator.FMeasure);
        }
    }

    public virtual FMeasure FMeasure => fmeasure;
}
