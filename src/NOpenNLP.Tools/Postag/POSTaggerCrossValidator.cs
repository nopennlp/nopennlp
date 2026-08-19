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
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Postag;

public class POSTaggerCrossValidator
{
    private readonly string languageCode;
    private readonly TrainingParameters @params;
    private readonly byte[]? featureGeneratorBytes; // NOpenNLP: made readonly
    private readonly Dictionary<string, object>? resources; // NOpenNLP: made readonly
    private readonly Mean wordAccuracy = new(); // NOpenNLP: made readonly
    private readonly IPOSTaggerEvaluationMonitor?[]? listeners; // NOpenNLP: made readonly

    /* this will be used to load the factory after the ngram dictionary was created */
    private readonly string? factoryClassName; // NOpenNLP: made readonly
    /* user can also send a ready to use factory */
    private POSTaggerFactory? factory;

    private readonly int? tagdicCutoff; // NOpenNLP: made readonly
    private readonly FileInfo? tagDictionaryFile; // NOpenNLP: made readonly

    /// <summary>
    /// Creates a <see cref="POSTaggerCrossValidator"/> that builds a ngram dictionary
    /// dynamically. It instantiates a sub-class of <see cref="POSTaggerFactory"/> using
    /// the tag and the ngram dictionaries.
    /// </summary>
    public POSTaggerCrossValidator(string languageCode, TrainingParameters trainParam,
        FileInfo? tagDictionary, byte[]? featureGeneratorBytes,
        Dictionary<string, object>? resources, int? tagdicCutoff, string? factoryClass,
        params IPOSTaggerEvaluationMonitor?[]? listeners)
    {
        this.languageCode = languageCode;
        this.@params = trainParam;
        this.featureGeneratorBytes = featureGeneratorBytes;
        this.resources = resources;
        this.listeners = listeners;
        this.factoryClassName = factoryClass;
        this.tagdicCutoff = tagdicCutoff;
        this.tagDictionaryFile = tagDictionary;
    }

    /// <summary>
    /// Creates a <see cref="POSTaggerCrossValidator"/> using the given
    /// <see cref="POSTaggerFactory"/>.
    /// </summary>
    public POSTaggerCrossValidator(string languageCode, TrainingParameters trainParam,
        POSTaggerFactory factory, params IPOSTaggerEvaluationMonitor?[]? listeners)
    {
        this.languageCode = languageCode;
        this.@params = trainParam;
        this.listeners = listeners;
        this.factory = factory;
        this.tagdicCutoff = null;
    }

    /// <summary>
    /// Starts the evaluation.
    /// </summary>
    /// <param name="samples">the data to train and test</param>
    /// <param name="nFolds">number of folds</param>
    /// <exception cref="IOException">IOException</exception>
    public virtual void Evaluate(IObjectStream<POSSample?> samples, int nFolds)
    {
        CrossValidationPartitioner<POSSample> partitioner = new(samples, nFolds);

        while (partitioner.HasNext)
        {
            var trainingSampleStream = partitioner.Next();

            if (this.tagDictionaryFile != null && this.factory!.TagDictionary == null)
            {
                this.factory.TagDictionary = this.factory.CreateTagDictionary(tagDictionaryFile);
            }

            ITagDictionary? dict = null;
            if (this.tagdicCutoff != null)
            {
                dict = this.factory!.TagDictionary;
                if (dict == null)
                {
                    dict = this.factory.CreateEmptyTagDictionary();
                }

                if (dict is IMutableTagDictionary mutableDict)
                {
                    POSTaggerME.PopulatePOSDictionary(trainingSampleStream, mutableDict,
                        this.tagdicCutoff.Value);
                }
                else
                {
                    throw new ArgumentException(
                        "Can't extend a TagDictionary that does not implement MutableTagDictionary.");
                }

                trainingSampleStream.Reset();
            }

            if (this.factory == null)
            {
                this.factory = POSTaggerFactory.Create(this.factoryClassName!, null, null);
            }

            factory.Init(featureGeneratorBytes, resources, dict);

            POSModel model = POSTaggerME.Train(languageCode, trainingSampleStream, @params, this.factory);

            POSEvaluator evaluator = new(new POSTaggerME(model), listeners);

            evaluator.Evaluate(trainingSampleStream.GetTestSampleStream());

            wordAccuracy.Add(evaluator.WordAccuracy, evaluator.WordCount);

            if (this.tagdicCutoff != null)
            {
                this.factory.TagDictionary = null;
            }
        }
    }

    /// <summary>
    /// Retrieves the accuracy for all iterations.
    /// </summary>
    public virtual double WordAccuracy => wordAccuracy.Value;

    /// <summary>
    /// Retrieves the number of words which where validated
    /// over all iterations. The result is the amount of folds
    /// multiplied by the total number of words.
    /// </summary>
    public virtual long WordCount => wordAccuracy.Count;

    // NOpenNLP: upstream declares a private static create(Dictionary, TagDictionary)
    // helper here that nothing calls; it is omitted rather than ported as dead code.
}
