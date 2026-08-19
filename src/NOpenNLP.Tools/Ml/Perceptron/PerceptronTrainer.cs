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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Perceptron;

/// <summary>
/// Trains models using the perceptron algorithm. Each outcome is represented as
/// a binary perceptron classifier. This supports standard (integer) weighting as well
/// as average weighting as described in:
/// Discriminative Training Methods for Hidden Markov Models: Theory and Experiments
/// with the Perceptron Algorithm. Michael Collins, EMNLP 2002.
/// </summary>
public class PerceptronTrainer : AbstractEventTrainer
{
    public const string PERCEPTRON_VALUE = "PERCEPTRON";
    public const double TOLERANCE_DEFAULT = .00001;

    /// <summary>Number of unique events which occurred in the event set.</summary>
    private int numUniqueEvents;

    /// <summary>Number of events in the event set.</summary>
    private int numEvents;

    /// <summary>Number of predicates.</summary>
    private int numPreds;

    /// <summary>Number of outcomes.</summary>
    private int numOutcomes;

    /// <summary>Records the array of predicates seen in each event.</summary>
    private int[][] contexts = null!;

    /// <summary>
    /// The value associated with each context. If null then context values are assumed to be 1.
    /// </summary>
    private float[][]? values;

    /// <summary>List of outcomes for each event i, in context[i].</summary>
    private int[] outcomeList = null!;

    /// <summary>
    /// Records the number of times an event has been seen for each event i, in context[i].
    /// </summary>
    private int[] numTimesEventsSeen = null!;

    /// <summary>
    /// Stores the string names of the outcomes. The perceptron only tracks outcomes
    /// as ints, and so this array is needed to save the model to disk and thereby
    /// allow users to know what the outcome was in human understandable terms.
    /// </summary>
    private string[] outcomeLabels = null!;

    /// <summary>
    /// Stores the string names of the predicates. The perceptron only tracks
    /// predicates as ints, and so this array is needed to save the model to disk
    /// and thereby allow users to know what the outcome was in human
    /// understandable terms.
    /// </summary>
    private string[] predLabels = null!;

    private double tolerance = TOLERANCE_DEFAULT;

    private double? stepSizeDecrease;

    private bool useSkippedlAveraging;

    public PerceptronTrainer()
    {
    }

    public PerceptronTrainer(TrainingParameters parameters)
        : base(parameters)
    {
    }

    public override void Validate()
    {
        base.Validate();

        string algorithmName = Algorithm;
        if (algorithmName != null && !PERCEPTRON_VALUE.Equals(algorithmName, StringComparison.Ordinal))
        {
            throw new ArgumentException("algorithmName must be PERCEPTRON");
        }
    }

    public override bool IsSortAndMerge => false;

    public override IMaxentModel DoTrain(IDataIndexer indexer)
    {
        int iterations = Iterations;
        int cutoff = Cutoff;

        bool useAverage = trainingParameters.GetBooleanParameter("UseAverage", true);

        bool useSkippedAveraging = trainingParameters.GetBooleanParameter("UseSkippedAveraging", false);

        // overwrite otherwise it might not work
        if (useSkippedAveraging)
        {
            useAverage = true;
        }

        double stepSizeDecreaseParam = trainingParameters.GetDoubleParameter("StepSizeDecrease", 0);

        double toleranceParam = trainingParameters.GetDoubleParameter("Tolerance", TOLERANCE_DEFAULT);

        SkippedAveraging = useSkippedAveraging;

        if (stepSizeDecreaseParam > 0)
        {
            StepSizeDecrease = stepSizeDecreaseParam;
        }

        Tolerance = toleranceParam;

        return TrainModel(iterations, indexer, cutoff, useAverage);
    }

    /// <summary>
    /// Specifies the tolerance. If the change in training set accuracy
    /// is less than this, stop iterating.
    /// </summary>
    public virtual double Tolerance
    {
        set
        {
            if (value < 0)
            {
                throw new ArgumentException(
                    "tolerance must be a positive number but is " + value + "!", nameof(value));
            }

            tolerance = value;
        }
    }

    /// <summary>
    /// Enables and sets step size decrease. The step size is decreased every
    /// iteration by the specified value, in percent.
    /// </summary>
    public virtual double StepSizeDecrease
    {
        set
        {
            if (value < 0 || value > 100)
            {
                throw new ArgumentException(
                    "decrease must be between 0 and 100 but is " + value + "!", nameof(value));
            }

            stepSizeDecrease = value;
        }
    }

    /// <summary>
    /// Enables skipped averaging; this flag changes the standard averaging to
    /// special averaging instead.
    /// <para/>
    /// If we are doing averaging, and the current iteration is one of the first 20
    /// or it is a perfect square, then update the summed parameters.
    /// <para/>
    /// The reason we don't take all of them is that the parameters change less
    /// toward the end of training, so they drown out the contributions of the more
    /// volatile early iterations. The use of perfect squares allows us to sample
    /// from successively farther apart iterations.
    /// </summary>
    public virtual bool SkippedAveraging
    {
        set => useSkippedlAveraging = value;
    }

    public virtual AbstractModel TrainModel(int iterations, IDataIndexer di, int cutoff) =>
        TrainModel(iterations, di, cutoff, true);

    public virtual AbstractModel TrainModel(int iterations, IDataIndexer di, int cutoff, bool useAverage)
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

        MutableContext[] finalParameters = FindParameters(iterations, useAverage);

        Display("...done.\n");

        return new PerceptronModel(finalParameters, predLabels, outcomeLabels);
    }

    private MutableContext[] FindParameters(int iterations, bool useAverage)
    {
        Display("Performing " + iterations + " iterations.\n");

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

        // Stores the sum of parameter values of each predicate over many iterations.
        MutableContext[] summedParams = new MutableContext[numPreds];
        if (useAverage)
        {
            for (int pi = 0; pi < numPreds; pi++)
            {
                summedParams[pi] = new MutableContext(allOutcomesPattern, new double[numOutcomes]);
                for (int aoi = 0; aoi < numOutcomes; aoi++)
                {
                    summedParams[pi].SetParameter(aoi, 0.0);
                }
            }
        }

        // Keep track of the previous three accuracies. The difference of
        // the mean of these and the current training set accuracy is used
        // with tolerance to decide whether to stop.
        double prevAccuracy1 = 0.0;
        double prevAccuracy2 = 0.0;
        double prevAccuracy3 = 0.0;

        // A counter for the denominator for averaging.
        int numTimesSummed = 0;

        double stepsize = 1;
        for (int i = 1; i <= iterations; i++)
        {
            // Decrease the stepsize by a small amount.
            if (stepSizeDecrease != null)
            {
                stepsize *= 1 - stepSizeDecrease.Value;
            }

            DisplayIteration(i);

            int numCorrect = 0;

            for (int ei = 0; ei < numUniqueEvents; ei++)
            {
                int targetOutcome = outcomeList[ei];

                for (int ni = 0; ni < numTimesEventsSeen[ei]; ni++)
                {
                    // Compute the model's prediction according to the current parameters.
                    double[] modelDistribution = new double[numOutcomes];
                    if (values != null)
                    {
                        PerceptronModel.Eval(contexts[ei], values[ei], modelDistribution, evalParams, false);
                    }
                    else
                    {
                        PerceptronModel.Eval(contexts[ei], null, modelDistribution, evalParams, false);
                    }

                    int maxOutcome = ArrayMath.Argmax(modelDistribution);

                    // If the predicted outcome is different from the target
                    // outcome, do the standard update: boost the parameters
                    // associated with the target and reduce those associated
                    // with the incorrect predicted outcome.
                    if (maxOutcome != targetOutcome)
                    {
                        for (int ci = 0; ci < contexts[ei].Length; ci++)
                        {
                            int pi = contexts[ei][ci];
                            if (values == null)
                            {
                                @params[pi].UpdateParameter(targetOutcome, stepsize);
                                @params[pi].UpdateParameter(maxOutcome, -stepsize);
                            }
                            else
                            {
                                @params[pi].UpdateParameter(targetOutcome, stepsize * values[ei][ci]);
                                @params[pi].UpdateParameter(maxOutcome, -stepsize * values[ei][ci]);
                            }
                        }
                    }

                    // Update the counts for accuracy.
                    if (maxOutcome == targetOutcome)
                    {
                        numCorrect++;
                    }
                }
            }

            // Calculate the training accuracy and display.
            double trainingAccuracy = (double)numCorrect / numEvents;
            if (i < 10 || (i % 10) == 0)
            {
                Display(". (" + numCorrect + "/" + numEvents + ") " + trainingAccuracy + "\n");
            }

            bool doAveraging = useAverage && useSkippedlAveraging && (i < 20 || IsPerfectSquare(i))
                || useAverage;

            if (doAveraging)
            {
                numTimesSummed++;
                for (int pi = 0; pi < numPreds; pi++)
                {
                    for (int aoi = 0; aoi < numOutcomes; aoi++)
                    {
                        summedParams[pi].UpdateParameter(aoi, @params[pi].Parameters[aoi]);
                    }
                }
            }

            // If the tolerance is greater than the difference between the
            // current training accuracy and all of the previous three
            // training accuracies, stop training.
            if (Math.Abs(prevAccuracy1 - trainingAccuracy) < tolerance
                && Math.Abs(prevAccuracy2 - trainingAccuracy) < tolerance
                && Math.Abs(prevAccuracy3 - trainingAccuracy) < tolerance)
            {
                Display("Stopping: change in training set accuracy less than " + tolerance + "\n");
                break;
            }

            // Update the previous training accuracies.
            prevAccuracy1 = prevAccuracy2;
            prevAccuracy2 = prevAccuracy3;
            prevAccuracy3 = trainingAccuracy;
        }

        // Output the final training stats.
        TrainingStats(evalParams);

        // Create averaged parameters
        if (useAverage)
        {
            for (int pi = 0; pi < numPreds; pi++)
            {
                for (int aoi = 0; aoi < numOutcomes; aoi++)
                {
                    summedParams[pi].SetParameter(aoi, summedParams[pi].Parameters[aoi] / numTimesSummed);
                }
            }

            return summedParams;
        }

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
                    PerceptronModel.Eval(contexts[ei], values[ei], modelDistribution, evalParams, false);
                }
                else
                {
                    PerceptronModel.Eval(contexts[ei], null, modelDistribution, evalParams, false);
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

    private void DisplayIteration(int i)
    {
        if (i > 10 && (i % 10) != 0)
        {
            return;
        }

        if (i < 10)
        {
            Display("  " + i + ":  ");
        }
        else if (i < 100)
        {
            Display(" " + i + ":  ");
        }
        else
        {
            Display(i + ":  ");
        }
    }

    // See whether a number is a perfect square. Inefficient, but fine for our purposes.
    private static bool IsPerfectSquare(int n)
    {
        int root = (int)Math.Sqrt(n);
        return root * root == n;
    }
}
