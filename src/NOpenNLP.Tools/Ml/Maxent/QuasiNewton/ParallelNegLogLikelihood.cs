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
using System.Threading.Tasks;
using NOpenNLP.Tools.Ml.Model;

namespace NOpenNLP.Tools.Ml.Maxent.Quasinewton;

/// <summary>
/// Evaluate negative log-likelihood and its gradient in parallel
/// </summary>
public class ParallelNegLogLikelihood : NegLogLikelihood
{
    // Number of threads
    private readonly int threads;

    // Partial value of negative log-likelihood to be computed by each thread
    private readonly double[] negLogLikelihoodThread;

    // Partial gradient
    private readonly double[][] gradientThread;

    // Scratch space per thread, reused across calls as upstream's task objects are
    private readonly double[][] sumsThread;

    public ParallelNegLogLikelihood(IDataIndexer indexer, int threads)
        : base(indexer)
    {
        if (threads <= 0)
            throw new ArgumentException(
                "Number of threads must 1 or larger");

        this.threads = threads;
        this.negLogLikelihoodThread = new double[threads];
        this.gradientThread = new double[threads][];
        this.sumsThread = new double[threads][];
        for (int t = 0; t < threads; t++)
        {
            this.gradientThread[t] = new double[dimension];
            this.sumsThread[t] = new double[numOutcomes];
        }
    }

    /// <summary>
    /// Negative log-likelihood
    /// </summary>
    public override double ValueAt(double[] x)
    {
        if (x.Length != dimension)
            throw new ArgumentException(
                "x is invalid, its dimension is not equal to domain dimension.");

        // Compute partial value of negative log-likelihood in each thread
        ComputeInParallel(x, ComputeNegLogLikelihood);

        double negLogLikelihood = 0;
        for (int t = 0; t < threads; t++)
        {
            negLogLikelihood += negLogLikelihoodThread[t];
        }

        return negLogLikelihood;
    }

    /// <summary>
    /// Compute gradient
    /// </summary>
    public override double[] GradientAt(double[] x)
    {
        if (x.Length != dimension)
            throw new ArgumentException(
                "x is invalid, its dimension is not equal to the function.");

        // Compute partial gradient in each thread
        ComputeInParallel(x, ComputeGradient);

        // Accumulate gradient
        for (int i = 0; i < dimension; i++)
        {
            gradient[i] = 0;
            for (int t = 0; t < threads; t++)
            {
                gradient[i] += gradientThread[t][i];
            }
        }

        return gradient;
    }

    /// <summary>
    /// Compute tasks in parallel
    /// </summary>
    // NOpenNLP: upstream selects the task by Class and constructs it reflectively over an
    // ExecutorService. The two tasks differ only in the body run per thread, so the body is
    // passed as a delegate and dispatched with Parallel.For, which needs no executor lifetime.
    private void ComputeInParallel(double[] x, Action<int, int, int, double[]> task)
    {
        int taskSize = numContexts / threads;
        int leftOver = numContexts % threads;

        Parallel.For(0, threads, i =>
        {
            if (i != threads - 1)
                task(i, i * taskSize, taskSize, x);
            else
                task(i, i * taskSize, taskSize + leftOver, x);
        });
    }

    /// <summary>
    /// Computes the partial value of negative log-likelihood for one thread's contexts
    /// </summary>
    private void ComputeNegLogLikelihood(int threadIndex, int startIndex, int length, double[] x)
    {
        double[] tempSums = sumsThread[threadIndex];
        int ci, oi, ai, vectorIndex, outcome;
        double predValue, logSumOfExps;
        negLogLikelihoodThread[threadIndex] = 0;

        for (ci = startIndex; ci < startIndex + length; ci++)
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
            negLogLikelihoodThread[threadIndex] -=
                (tempSums[outcome] - logSumOfExps) * numTimesEventsSeen[ci];
        }
    }

    /// <summary>
    /// Computes the partial gradient for one thread's contexts
    /// </summary>
    private void ComputeGradient(int threadIndex, int startIndex, int length, double[] x)
    {
        double[] expectation = sumsThread[threadIndex];
        int ci, oi, ai, vectorIndex;
        double predValue, logSumOfExps;
        int empirical;

        // Reset gradientThread
        Array.Clear(gradientThread[threadIndex], 0, gradientThread[threadIndex].Length);

        for (ci = startIndex; ci < startIndex + length; ci++)
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
                    gradientThread[threadIndex][vectorIndex] +=
                        predValue * (expectation[oi] - empirical) * numTimesEventsSeen[ci];
                }
            }
        }
    }
}
