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
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ml.Maxent.Quasinewton;

/// <summary>
/// Maxent model trainer using L-BFGS algorithm.
/// </summary>
public class QNTrainer : AbstractEventTrainer
{
    public const string MAXENT_QN_VALUE = "MAXENT_QN";

    public const string THREADS_PARAM = "Threads";
    public const int THREADS_DEFAULT = 1;

    public const string L1COST_PARAM = "L1Cost";
    public const double L1COST_DEFAULT = 0.1;

    public const string L2COST_PARAM = "L2Cost";
    public const double L2COST_DEFAULT = 0.1;

    // Number of Hessian updates to store
    public const string M_PARAM = "NumOfUpdates";
    public const int M_DEFAULT = 15;

    // Maximum number of function evaluations
    public const string MAX_FCT_EVAL_PARAM = "MaxFctEval";
    public const int MAX_FCT_EVAL_DEFAULT = 30000;

    // Number of threads
    private int threads;

    // L1-regularization cost
    private double l1Cost;

    // L2-regularization cost
    private double l2Cost;

    // Settings for QNMinimizer
    private int m;
    private int maxFctEval;

    public QNTrainer(TrainingParameters parameters)
        : base(parameters)
    {
    }

    // Constructor -- to log. For testing purpose
    public QNTrainer(bool printMessages)
        : this(M_DEFAULT, printMessages)
    {
    }

    // Constructor -- m : number of hessian updates to store. For testing purpose
    public QNTrainer(int m)
        : this(m, true)
    {
    }

    // Constructor -- to log, number of hessian updates to store. For testing purpose
    public QNTrainer(int m, bool verbose)
        : this(m, MAX_FCT_EVAL_DEFAULT, verbose)
    {
    }

    // For testing purpose
    public QNTrainer(int m, int maxFctEval, bool printMessages)
    {
        this.printMessages = printMessages;
        this.m = m < 0 ? M_DEFAULT : m;
        this.maxFctEval = maxFctEval < 0 ? MAX_FCT_EVAL_DEFAULT : maxFctEval;
        this.threads = THREADS_DEFAULT;
        this.l1Cost = L1COST_DEFAULT;
        this.l2Cost = L2COST_DEFAULT;
    }

    // >> Members related to AbstractEventTrainer
    public QNTrainer()
    {
    }

    public override void Init(TrainingParameters trainingParameters, IDictionary<string, string>? reportMap)
    {
        base.Init(trainingParameters, reportMap);
        this.m = trainingParameters.GetIntParameter(M_PARAM, M_DEFAULT);
        this.maxFctEval = trainingParameters.GetIntParameter(MAX_FCT_EVAL_PARAM, MAX_FCT_EVAL_DEFAULT);
        this.threads = trainingParameters.GetIntParameter(THREADS_PARAM, THREADS_DEFAULT);
        this.l1Cost = trainingParameters.GetDoubleParameter(L1COST_PARAM, L1COST_DEFAULT);
        this.l2Cost = trainingParameters.GetDoubleParameter(L2COST_PARAM, L2COST_DEFAULT);
    }

    public override void Validate()
    {
        base.Validate();

        string algorithmName = Algorithm;
        if (algorithmName != null && !MAXENT_QN_VALUE.Equals(algorithmName, StringComparison.Ordinal))
        {
            throw new ArgumentException("algorithmName must be MAXENT_QN");
        }

        // Number of Hessian updates to remember
        if (m < 0)
        {
            throw new ArgumentException(
                "Number of Hessian updates to remember must be >= 0");
        }

        // Maximum number of function evaluations
        if (maxFctEval < 0)
        {
            throw new ArgumentException(
                "Maximum number of function evaluations must be >= 0");
        }

        // Number of threads must be >= 1
        if (threads < 1)
        {
            throw new ArgumentException("Number of threads must be >= 1");
        }

        // Regularization costs must be >= 0
        if (l1Cost < 0)
        {
            throw new ArgumentException("Regularization costs must be >= 0");
        }

        if (l2Cost < 0)
        {
            throw new ArgumentException("Regularization costs must be >= 0");
        }
    }

    public override bool IsSortAndMerge => true;

    public override IMaxentModel DoTrain(IDataIndexer indexer)
    {
        int iterations = Iterations;
        return TrainModel(iterations, indexer);
    }

    // << Members related to AbstractEventTrainer
    public QNModel TrainModel(int iterations, IDataIndexer indexer)
    {
        // Train model's parameters
        IFunction objectiveFunction;
        if (threads == 1)
        {
            Console.WriteLine("Computing model parameters ...");
            objectiveFunction = new NegLogLikelihood(indexer);
        }
        else
        {
            Console.WriteLine("Computing model parameters in " + threads + " threads ...");
            objectiveFunction = new ParallelNegLogLikelihood(indexer, threads);
        }

        QNMinimizer minimizer = new(
            l1Cost, l2Cost, iterations, m, maxFctEval, printMessages)
        {
            Evaluator = new ModelEvaluator(indexer),
        };

        double[] parameters = minimizer.Minimize(objectiveFunction);

        // Construct model with trained parameters
        string[] predLabels = indexer.PredLabels;
        int nPredLabels = predLabels.Length;

        string[] outcomeNames = indexer.OutcomeLabels;
        int nOutcomes = outcomeNames.Length;

        Context[] @params = new Context[nPredLabels];
        for (int ci = 0; ci < @params.Length; ci++)
        {
            JCG.List<int> outcomePattern = new(nOutcomes);
            JCG.List<double> alpha = new(nOutcomes);
            for (int oi = 0; oi < nOutcomes; oi++)
            {
                double val = parameters[oi * nPredLabels + ci];
                outcomePattern.Add(oi);
                alpha.Add(val);
            }

            @params[ci] = new Context(ArrayMath.ToIntArray(outcomePattern),
                ArrayMath.ToDoubleArray(alpha));
        }

        return new QNModel(@params, predLabels, outcomeNames);
    }

    /// <summary>
    /// For measuring model's training accuracy
    /// </summary>
    private sealed class ModelEvaluator(IDataIndexer indexer) : QNMinimizer.IEvaluator
    {
        /// <summary>
        /// Evaluate the current model on training data set
        /// </summary>
        /// <returns>model's training accuracy</returns>
        public double Evaluate(double[] parameters)
        {
            int[][] contexts = indexer.Contexts;
            float[][]? values = indexer.Values;
            int[] nEventsSeen = indexer.NumTimesEventsSeen;
            int[] outcomeList = indexer.OutcomeList;
            int nOutcomes = indexer.OutcomeLabels.Length;
            int nPredLabels = indexer.PredLabels.Length;

            int nCorrect = 0;
            int nTotalEvents = 0;

            for (int ei = 0; ei < contexts.Length; ei++)
            {
                int[] context = contexts[ei];
                float[]? value = values?[ei];

                double[] probs = new double[nOutcomes];
                QNModel.Eval(context, value, probs, nOutcomes, nPredLabels, parameters);
                int outcome = ArrayMath.Argmax(probs);
                if (outcome == outcomeList[ei])
                {
                    nCorrect += nEventsSeen[ei];
                }

                nTotalEvents += nEventsSeen[ei];
            }

            return (double)nCorrect / nTotalEvents;
        }
    }
}
