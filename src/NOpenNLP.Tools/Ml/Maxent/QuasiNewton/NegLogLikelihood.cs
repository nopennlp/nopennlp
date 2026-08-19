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

namespace NOpenNLP.Tools.Ml.Maxent.Quasinewton;

/// <summary>
/// Evaluate negative log-likelihood and its gradient from DataIndexer.
/// </summary>
public class NegLogLikelihood : IFunction
{
    protected int dimension;
    protected int numOutcomes;
    protected int numFeatures;
    protected int numContexts;

    // Information from data index
    protected readonly float[][]? values;
    protected readonly int[][] contexts;
    protected readonly int[] outcomeList;
    protected readonly int[] numTimesEventsSeen;

    // For calculating negLogLikelihood and gradient
    protected double[] tempSums;
    protected double[] expectation;

    protected double[] gradient;

    public NegLogLikelihood(IDataIndexer indexer)
    {
        // Get data from indexer.
        // NOpenNLP: upstream checks for OnePassRealValueDataIndexer specifically. The
        // indexer's Values is null unless it is real-valued, so reading it directly
        // covers the same cases and any other real-valued indexer.
        this.values = indexer.Values;

        this.contexts = indexer.Contexts;
        this.outcomeList = indexer.OutcomeList;
        this.numTimesEventsSeen = indexer.NumTimesEventsSeen;

        this.numOutcomes = indexer.OutcomeLabels.Length;
        this.numFeatures = indexer.PredLabels.Length;
        this.numContexts = this.contexts.Length;
        this.dimension = numOutcomes * numFeatures;

        this.expectation = new double[numOutcomes];
        this.tempSums = new double[numOutcomes];
        this.gradient = new double[dimension];
    }

    public virtual int Dimension => this.dimension;

    public virtual double[] GetInitialPoint() => new double[dimension];

    /// <summary>
    /// Negative log-likelihood
    /// </summary>
    public virtual double ValueAt(double[] x)
    {
        if (x.Length != dimension)
            throw new ArgumentException(
                "x is invalid, its dimension is not equal to domain dimension.");

        int ci, oi, ai, vectorIndex, outcome;
        double predValue, logSumOfExps;
        double negLogLikelihood = 0;

        for (ci = 0; ci < numContexts; ci++)
        {
            for (oi = 0; oi < numOutcomes; oi++)
            {
                tempSums[oi] = 0;
                for (ai = 0; ai < contexts[ci].Length; ai++)
                {
                    vectorIndex = IndexOf(oi, contexts[ci][ai]);
                    predValue = values != null ? values[ci][ai] : 1.0;
                    tempSums[oi] += predValue * x[vectorIndex];
                }
            }

            logSumOfExps = ArrayMath.LogSumOfExps(tempSums);

            outcome = outcomeList[ci];
            negLogLikelihood -= (tempSums[outcome] - logSumOfExps) * numTimesEventsSeen[ci];
        }

        return negLogLikelihood;
    }

    /// <summary>
    /// Compute gradient
    /// </summary>
    public virtual double[] GradientAt(double[] x)
    {
        if (x.Length != dimension)
            throw new ArgumentException(
                "x is invalid, its dimension is not equal to the function.");

        int ci, oi, ai, vectorIndex;
        double predValue, logSumOfExps;
        int empirical;

        // Reset gradient
        Array.Clear(gradient, 0, gradient.Length);

        for (ci = 0; ci < numContexts; ci++)
        {
            for (oi = 0; oi < numOutcomes; oi++)
            {
                expectation[oi] = 0;
                for (ai = 0; ai < contexts[ci].Length; ai++)
                {
                    vectorIndex = IndexOf(oi, contexts[ci][ai]);
                    predValue = values != null ? values[ci][ai] : 1.0;
                    expectation[oi] += predValue * x[vectorIndex];
                }
            }

            logSumOfExps = ArrayMath.LogSumOfExps(expectation);

            for (oi = 0; oi < numOutcomes; oi++)
            {
                expectation[oi] = Math.Exp(expectation[oi] - logSumOfExps);
            }

            for (oi = 0; oi < numOutcomes; oi++)
            {
                empirical = outcomeList[ci] == oi ? 1 : 0;
                for (ai = 0; ai < contexts[ci].Length; ai++)
                {
                    vectorIndex = IndexOf(oi, contexts[ci][ai]);
                    predValue = values != null ? values[ci][ai] : 1.0;
                    gradient[vectorIndex] +=
                        predValue * (expectation[oi] - empirical) * numTimesEventsSeen[ci];
                }
            }
        }

        return gradient;
    }

    protected int IndexOf(int outcomeId, int featureId) => outcomeId * numFeatures + featureId;
}
