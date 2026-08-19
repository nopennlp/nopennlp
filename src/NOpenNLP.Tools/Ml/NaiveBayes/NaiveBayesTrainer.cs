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

using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Naivebayes;

/// <summary>
/// Trains models using the combination of EM algorithm and Naive Bayes classifier
/// which is described in:
/// Text Classification from Labeled and Unlabeled Documents using EM
/// Nigam, McCallum, et al paper of 2000
/// </summary>
public class NaiveBayesTrainer : AbstractEventTrainer
{
    public const string NAIVE_BAYES_VALUE = "NAIVEBAYES";

    /// <summary>
    /// Number of unique events which occurred in the event set.
    /// </summary>
    private int numUniqueEvents;

    /// <summary>
    /// Number of events in the event set.
    /// </summary>
    private int numEvents;

    /// <summary>
    /// Number of predicates.
    /// </summary>
    private int numPreds;

    /// <summary>
    /// Number of outcomes.
    /// </summary>
    private int numOutcomes;

    /// <summary>
    /// Records the array of predicates seen in each event.
    /// </summary>
    private int[][] contexts = null!;

    /// <summary>
    /// The value associated with each context. If null then context values are assumed to be 1.
    /// </summary>
    private float[][]? values;

    /// <summary>
    /// List of outcomes for each event i, in context[i].
    /// </summary>
    private int[] outcomeList = null!;

    /// <summary>
    /// Records the number of times an event has been seen for each event i, in context[i].
    /// </summary>
    private int[] numTimesEventsSeen = null!;

    /// <summary>
    /// Stores the string names of the outcomes. The NaiveBayes only tracks outcomes
    /// as ints, and so this array is needed to save the model to disk and
    /// thereby allow users to know what the outcome was in human
    /// understandable terms.
    /// </summary>
    private string[] outcomeLabels = null!;

    /// <summary>
    /// Stores the string names of the predicates. The NaiveBayes only tracks
    /// predicates as ints, and so this array is needed to save the model to
    /// disk and thereby allow users to know what the outcome was in human
    /// understandable terms.
    /// </summary>
    private string[] predLabels = null!;

    public NaiveBayesTrainer()
    {
    }

    public NaiveBayesTrainer(TrainingParameters parameters)
        : base(parameters)
    {
    }

    public override bool IsSortAndMerge => false;

    public override IMaxentModel DoTrain(IDataIndexer indexer) => TrainModel(indexer);

    public virtual AbstractModel TrainModel(IDataIndexer di)
    {
        Display("Incorporating indexed data for training...  \n");
        contexts = di.Contexts;
        values = di.Values;
        numTimesEventsSeen = di.NumTimesEventsSeen;
        numEvents = di.NumEvents;
        numUniqueEvents = contexts.Length;

        outcomeLabels = di.OutcomeLabels;
        outcomeList = di.OutcomeList;

        predLabels = di.PredLabels;
        numPreds = predLabels.Length;
        numOutcomes = outcomeLabels.Length;

        Display("done.\n");

        Display("\tNumber of Event Tokens: " + numUniqueEvents + "\n");
        Display("\t    Number of Outcomes: " + numOutcomes + "\n");
        Display("\t  Number of Predicates: " + numPreds + "\n");

        Display("Computing model parameters...\n");

        MutableContext[] finalParameters = FindParameters();

        Display("...done.\n");

        return new NaiveBayesModel(finalParameters, predLabels, outcomeLabels);
    }

    private MutableContext[] FindParameters()
    {
        int[] allOutcomesPattern = new int[numOutcomes];
        for (int oi = 0; oi < numOutcomes; oi++)
        {
            allOutcomesPattern[oi] = oi;
        }

        // Stores the estimated parameter value of each predicate during iteration.
        MutableContext[] @params = new MutableContext[numPreds];
        for (int pi = 0; pi < numPreds; pi++)
        {
            @params[pi] = new MutableContext(allOutcomesPattern, new double[numOutcomes]);
            for (int aoi = 0; aoi < numOutcomes; aoi++)
            {
                @params[pi].SetParameter(aoi, 0.0);
            }
        }

        EvalParameters evalParams = new(@params, numOutcomes);

        double stepSize = 1;

        for (int ei = 0; ei < numUniqueEvents; ei++)
        {
            int targetOutcome = outcomeList[ei];
            for (int ni = 0; ni < numTimesEventsSeen[ei]; ni++)
            {
                for (int ci = 0; ci < contexts[ei].Length; ci++)
                {
                    int pi = contexts[ei][ci];
                    if (values == null)
                    {
                        @params[pi].UpdateParameter(targetOutcome, stepSize);
                    }
                    else
                    {
                        @params[pi].UpdateParameter(targetOutcome, stepSize * values[ei][ci]);
                    }
                }
            }
        }

        // Output the final training stats.
        TrainingStats(evalParams);

        return @params;
    }

    private double TrainingStats(EvalParameters evalParams)
    {
        int numCorrect = 0;

        for (int ei = 0; ei < numUniqueEvents; ei++)
        {
            for (int ni = 0; ni < numTimesEventsSeen[ei]; ni++)
            {
                double[] modelDistribution = new double[numOutcomes];

                if (values != null)
                {
                    NaiveBayesModel.Eval(contexts[ei], values[ei], modelDistribution, evalParams, false);
                }
                else
                {
                    NaiveBayesModel.Eval(contexts[ei], null, modelDistribution, evalParams, false);
                }

                int max = ArrayMath.Argmax(modelDistribution);
                if (max == outcomeList[ei])
                {
                    numCorrect++;
                }
            }
        }

        double trainingAccuracy = (double)numCorrect / numEvents;
        Display("Stats: (" + numCorrect + "/" + numEvents + ") " + trainingAccuracy + "\n");
        return trainingAccuracy;
    }
}
