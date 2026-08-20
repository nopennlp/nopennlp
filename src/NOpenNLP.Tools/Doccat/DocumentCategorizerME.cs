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
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// Maxent implementation of <see cref="IDocumentCategorizer"/>.
/// </summary>
public class DocumentCategorizerME : IDocumentCategorizer
{
    private readonly DoccatModel model; // NOpenNLP: made readonly

    // NOpenNLP: made readonly
    private readonly DocumentCategorizerContextGenerator mContextGenerator;

    /// <summary>
    /// Initializes the current instance with a doccat model. Default feature
    /// generation is used.
    /// </summary>
    /// <param name="model">the doccat model</param>
    public DocumentCategorizerME(DoccatModel model)
    {
        this.model = model;
        this.mContextGenerator = new DocumentCategorizerContextGenerator(model.Factory.FeatureGenerators);
    }

    /// <summary>
    /// Categorize the given text provided as tokens along with
    /// the provided extra information
    /// </summary>
    /// <param name="text">text tokens to categorize</param>
    /// <param name="extraInformation">additional information</param>
    public virtual double[] Categorize(string[] text, IDictionary<string, object> extraInformation) =>
        model.MaxentModel.Eval(mContextGenerator.GetContext(text, extraInformation));

    /// <summary>
    /// Categorizes the given text.
    /// </summary>
    /// <param name="text">the text to categorize</param>
    public virtual double[] Categorize(string[] text) =>
        Categorize(text, new JCG.Dictionary<string, object>());

    /// <summary>
    /// Returns a map in which the key is the category name and the value is the score
    /// </summary>
    /// <param name="text">the input text to classify</param>
    /// <returns>the score map</returns>
    public virtual IDictionary<string, double> ScoreMap(string[] text)
    {
        IDictionary<string, double> probDist = new JCG.Dictionary<string, double>();

        double[] categorize = Categorize(text);
        int catSize = NumberOfCategories;
        for (int i = 0; i < catSize; i++)
        {
            string category = GetCategory(i);
            probDist[category] = categorize[GetIndex(category)];
        }

        return probDist;
    }

    /// <summary>
    /// Returns a map with the score as a key in ascending order.
    /// The value is a set of categories with the score.
    /// Many categories can have the same score, hence the set as value
    /// </summary>
    /// <param name="text">the input text to classify</param>
    /// <returns>the sorted score map</returns>
    public virtual SortedDictionary<double, ISet<string>> SortedScoreMap(string[] text)
    {
        SortedDictionary<double, ISet<string>> descendingMap = [];
        double[] categorize = Categorize(text);
        int catSize = NumberOfCategories;
        for (int i = 0; i < catSize; i++)
        {
            string category = GetCategory(i);
            double score = categorize[GetIndex(category)];
            if (descendingMap.TryGetValue(score, out ISet<string>? categories))
            {
                categories.Add(category);
            }
            else
            {
                ISet<string> newset = new JCG.HashSet<string> { category };
                descendingMap[score] = newset;
            }
        }

        return descendingMap;
    }

    public virtual string GetBestCategory(double[] outcome) =>
        model.MaxentModel.GetBestOutcome(outcome);

    public virtual int GetIndex(string category) =>
        model.MaxentModel.GetIndex(category);

    public virtual string GetCategory(int index) =>
        model.MaxentModel.GetOutcome(index);

    public virtual int NumberOfCategories => model.MaxentModel.NumOutcomes;

    public virtual string GetAllResults(double[] results) =>
        model.MaxentModel.GetAllOutcomes(results);

    public static DoccatModel Train(string languageCode, IObjectStream<DocumentSample?> samples,
        TrainingParameters mlParams, DoccatFactory factory)
    {
        IDictionary<string, string> manifestInfoEntries = new JCG.Dictionary<string, string>();

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(mlParams, manifestInfoEntries);

        IMaxentModel model = trainer.Train(
            new DocumentCategorizerEventStream(samples, factory.FeatureGenerators));

        return new DoccatModel(languageCode, model, manifestInfoEntries, factory);
    }
}
