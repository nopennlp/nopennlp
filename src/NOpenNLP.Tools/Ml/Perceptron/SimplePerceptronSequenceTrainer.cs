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
using NOpenNLP.Tools.Ml.Model;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ml.Perceptron;

/// <summary>
/// Trains models for sequences using the perceptron algorithm. Each outcome is represented as
/// a binary perceptron classifier. This supports standard (integer) weighting as well as
/// average weighting. Sequence information is used in a simplified way to that described in:
/// Discriminative Training Methods for Hidden Markov Models: Theory and Experiments
/// with the Perceptron Algorithm. Michael Collins, EMNLP 2002.
/// Specifically only updates are applied to tokens which were incorrectly tagged by a sequence
/// tagger rather than to all features across the sequence which differ from the training sequence.
/// </summary>
/// <typeparam name="T">The type of the object which is the source of each sequence.</typeparam>
public class SimplePerceptronSequenceTrainer<T> : AbstractEventModelSequenceTrainer<T>
{
    public const string PERCEPTRON_SEQUENCE_VALUE = "PERCEPTRON_SEQUENCE";

    private const int VALUE = 0;
    private const int ITER = 1;
    private const int EVENT = 2;

    private int iterations;
    private ISequenceStream<T> sequenceStream = null!;

    /// <summary>Number of events in the event set.</summary>
    private int numEvents;

    /// <summary>Number of predicates.</summary>
    private int numPreds;
    private int numOutcomes;

    /// <summary>List of outcomes for each event i, in context[i].</summary>
    private int[] outcomeList = null!;

    private string[] outcomeLabels = null!;

    /// <summary>Stores the average parameter values of each predicate during iteration.</summary>
    private MutableContext[] averageParams = null!;

    /// <summary>Mapping between context and an integer.</summary>
    private IDictionary<string, int> pmap = null!;

    private IDictionary<string, int> omap = null!;

    /// <summary>Stores the estimated parameter value of each predicate during iteration.</summary>
    private MutableContext[] @params = null!;
    private bool useAverage;
    private int[][][] updates = null!;

    private string[] predLabels = null!;
    private int numSequences;

    public SimplePerceptronSequenceTrainer()
    {
    }

    public override void Validate()
    {
        base.Validate();

        string algorithmName = Algorithm;
        if (algorithmName != null
            && !PERCEPTRON_SEQUENCE_VALUE.Equals(algorithmName, StringComparison.Ordinal))
        {
            throw new ArgumentException("algorithmName must be PERCEPTRON_SEQUENCE");
        }
    }

    public override IMaxentModel DoTrain(ISequenceStream<T> events)
    {
        int iterations = Iterations;
        int cutoff = Cutoff;

        bool useAverage = trainingParameters.GetBooleanParameter("UseAverage", true);

        return TrainModel(iterations, events, cutoff, useAverage);
    }

    public virtual AbstractModel TrainModel(int iterations, ISequenceStream<T> sequenceStream,
        int cutoff, bool useAverage)
    {
        this.iterations = iterations;
        this.sequenceStream = sequenceStream;

        trainingParameters.Put(AbstractDataIndexer.CUTOFF_PARAM, cutoff);
        trainingParameters.Put(AbstractDataIndexer.SORT_PARAM, false);
        IDataIndexer di = new OnePassDataIndexer();
        di.Init(trainingParameters, reportMap);
        di.Index(new SequenceStreamEventStream<T>(sequenceStream));
        numSequences = 0;

        sequenceStream.Reset();

        while (sequenceStream.Read() != null)
        {
            numSequences++;
        }

        outcomeList = di.OutcomeList;
        predLabels = di.PredLabels;
        pmap = new JCG.Dictionary<string, int>();

        for (int i = 0; i < predLabels.Length; i++)
        {
            pmap[predLabels[i]] = i;
        }

        Display("Incorporating indexed data for training...  \n");
        this.useAverage = useAverage;
        numEvents = di.NumEvents;

        this.iterations = iterations;
        outcomeLabels = di.OutcomeLabels;
        omap = new JCG.Dictionary<string, int>();
        for (int oli = 0; oli < outcomeLabels.Length; oli++)
        {
            omap[outcomeLabels[oli]] = oli;
        }

        outcomeList = di.OutcomeList;

        numPreds = predLabels.Length;
        numOutcomes = outcomeLabels.Length;
        if (useAverage)
        {
            updates = new int[numPreds][][];
            for (int pi = 0; pi < numPreds; pi++)
            {
                updates[pi] = new int[numOutcomes][];
                for (int oi = 0; oi < numOutcomes; oi++)
                {
                    updates[pi][oi] = new int[3];
                }
            }
        }

        Display("done.\n");

        Display("\tNumber of Event Tokens: " + numEvents + "\n");
        Display("\t    Number of Outcomes: " + numOutcomes + "\n");
        Display("\t  Number of Predicates: " + numPreds + "\n");

        @params = new MutableContext[numPreds];
        if (useAverage)
        {
            averageParams = new MutableContext[numPreds];
        }

        int[] allOutcomesPattern = new int[numOutcomes];
        for (int oi = 0; oi < numOutcomes; oi++)
        {
            allOutcomesPattern[oi] = oi;
        }

        for (int pi = 0; pi < numPreds; pi++)
        {
            @params[pi] = new MutableContext(allOutcomesPattern, new double[numOutcomes]);
            if (useAverage)
            {
                averageParams[pi] = new MutableContext(allOutcomesPattern, new double[numOutcomes]);
            }

            for (int aoi = 0; aoi < numOutcomes; aoi++)
            {
                @params[pi].SetParameter(aoi, 0.0);
                if (useAverage)
                {
                    averageParams[pi].SetParameter(aoi, 0.0);
                }
            }
        }

        Display("Computing model parameters...\n");
        FindParameters(iterations);
        Display("...done.\n");

        string[] updatedPredLabels = predLabels;

        return useAverage
            ? new PerceptronModel(averageParams, updatedPredLabels, outcomeLabels)
            : new PerceptronModel(@params, updatedPredLabels, outcomeLabels);
    }

    private void FindParameters(int iterations)
    {
        Display("Performing " + iterations + " iterations.\n");
        for (int i = 1; i <= iterations; i++)
        {
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

            NextIteration(i);
        }

        TrainingStats(useAverage ? averageParams : @params);
    }

    public virtual void NextIteration(int iteration)
    {
        iteration--; // move to 0-based index
        int numCorrect = 0;
        int oei = 0;
        int si = 0;
        List<IDictionary<string, float>> featureCounts = new(numOutcomes);
        for (int oi = 0; oi < numOutcomes; oi++)
        {
            featureCounts.Add(new JCG.Dictionary<string, float>());
        }

        PerceptronModel model = new(@params, predLabels, outcomeLabels);

        sequenceStream.Reset();

        Sequence<T>? sequence;
        while ((sequence = sequenceStream.Read()) != null)
        {
            Event[] taggerEvents = sequenceStream.UpdateContext(sequence, model);
            Event[] events = sequence.Events;
            bool update = false;
            for (int ei = 0; ei < events.Length; ei++, oei++)
            {
                if (!taggerEvents[ei].Outcome.Equals(events[ei].Outcome, StringComparison.Ordinal))
                {
                    update = true;
                }
                else
                {
                    numCorrect++;
                }
            }

            if (update)
            {
                for (int oi = 0; oi < numOutcomes; oi++)
                {
                    featureCounts[oi].Clear();
                }

                // training feature count computation
                for (int ei = 0; ei < events.Length; ei++, oei++)
                {
                    string[] contextStrings = events[ei].Context;
                    float[]? values = events[ei].Values;
                    int oi = omap[events[ei].Outcome];
                    for (int ci = 0; ci < contextStrings.Length; ci++)
                    {
                        float value = values != null ? values[ci] : 1;
                        featureCounts[oi][contextStrings[ci]] =
                            featureCounts[oi].TryGetValue(contextStrings[ci], out float c)
                                ? c + value
                                : value;
                    }
                }

                // evaluation feature count computation
                foreach (Event taggerEvent in taggerEvents)
                {
                    string[] contextStrings = taggerEvent.Context;
                    float[]? values = taggerEvent.Values;
                    int oi = omap[taggerEvent.Outcome];
                    for (int ci = 0; ci < contextStrings.Length; ci++)
                    {
                        float value = values != null ? values[ci] : 1;
                        float c = featureCounts[oi].TryGetValue(contextStrings[ci], out float existing)
                            ? existing - value
                            : -1 * value;

                        if (c == 0f)
                        {
                            featureCounts[oi].Remove(contextStrings[ci]);
                        }
                        else
                        {
                            featureCounts[oi][contextStrings[ci]] = c;
                        }
                    }
                }

                for (int oi = 0; oi < numOutcomes; oi++)
                {
                    foreach (KeyValuePair<string, float> entry in featureCounts[oi])
                    {
                        if (!pmap.TryGetValue(entry.Key, out int pi))
                        {
                            continue;
                        }

                        @params[pi].UpdateParameter(oi, entry.Value);
                        if (useAverage)
                        {
                            if (updates[pi][oi][VALUE] != 0)
                            {
                                averageParams[pi].UpdateParameter(oi, updates[pi][oi][VALUE]
                                    * (numSequences * (iteration - updates[pi][oi][ITER])
                                        + (si - updates[pi][oi][EVENT])));
                            }

                            updates[pi][oi][VALUE] = (int)@params[pi].Parameters[oi];
                            updates[pi][oi][ITER] = iteration;
                            updates[pi][oi][EVENT] = si;
                        }
                    }
                }

                model = new PerceptronModel(@params, predLabels, outcomeLabels);
            }

            si++;
        }

        // finish average computation
        double totIterations = (double)iterations * si;
        if (useAverage && iteration == iterations - 1)
        {
            for (int pi = 0; pi < numPreds; pi++)
            {
                double[] predParams = averageParams[pi].Parameters;
                for (int oi = 0; oi < numOutcomes; oi++)
                {
                    if (updates[pi][oi][VALUE] != 0)
                    {
                        predParams[oi] += updates[pi][oi][VALUE] * (numSequences
                            * (iterations - updates[pi][oi][ITER]) - updates[pi][oi][EVENT]);
                    }

                    if (predParams[oi] != 0)
                    {
                        predParams[oi] /= totIterations;
                        averageParams[pi].SetParameter(oi, predParams[oi]);
                    }
                }
            }
        }

        Display(". (" + numCorrect + "/" + numEvents + ") " + ((double)numCorrect / numEvents) + "\n");
    }

    private void TrainingStats(MutableContext[] @params)
    {
        int numCorrect = 0;
        int oei = 0;

        sequenceStream.Reset();

        Sequence<T>? sequence;
        while ((sequence = sequenceStream.Read()) != null)
        {
            Event[] taggerEvents = sequenceStream.UpdateContext(sequence,
                new PerceptronModel(@params, predLabels, outcomeLabels));
            for (int ei = 0; ei < taggerEvents.Length; ei++, oei++)
            {
                int max = omap[taggerEvents[ei].Outcome];
                if (max == outcomeList[oei])
                {
                    numCorrect++;
                }
            }
        }

        Display(". (" + numCorrect + "/" + numEvents + ") " + ((double)numCorrect / numEvents) + "\n");
    }
}
