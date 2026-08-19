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

namespace NOpenNLP.Tools.Ml.Maxent.Quasinewton;

/// <summary>
/// Class that performs line search to find minimum
/// </summary>
public static class LineSearch // NOpenNLP: made static
{
    private const double C = 0.0001;
    private const double RHO = 0.5; // decrease of step size (must be from 0 to 1)

    /// <summary>
    /// Backtracking line search (see Nocedal &amp; Wright 2006, Numerical Optimization, p. 37)
    /// </summary>
    public static void DoLineSearch(IFunction function,
        double[] direction, LineSearchResult lsr, double initialStepSize)
    {
        double stepSize = initialStepSize;
        int currFctEvalCount = lsr.FctEvalCount;
        double[] x = lsr.NextPoint;
        double[] gradAtX = lsr.GradAtNext;
        double valueAtX = lsr.ValueAtNext;
        int dimension = x.Length;

        // Retrieve current points and gradient for array reuse purpose
        double[] nextPoint = lsr.CurrPoint;
        double[] gradAtNextPoint = lsr.GradAtCurr;
        double valueAtNextPoint;

        double dirGradientAtX = ArrayMath.InnerProduct(direction, gradAtX);

        // To avoid recomputing in the loop
        double cachedProd = C * dirGradientAtX;

        while (true)
        {
            // Get next point
            for (int i = 0; i < dimension; i++)
            {
                nextPoint[i] = x[i] + direction[i] * stepSize;
            }

            // New value
            valueAtNextPoint = function.ValueAt(nextPoint);

            currFctEvalCount++;

            // Check Armijo condition
            if (valueAtNextPoint <= valueAtX + cachedProd * stepSize)
                break;

            // Shrink step size
            stepSize *= RHO;
        }

        // Compute and save gradient at the new point
        Array.Copy(function.GradientAt(nextPoint), 0, gradAtNextPoint, 0,
            gradAtNextPoint.Length);

        // Update line search result
        lsr.SetAll(stepSize, valueAtX, valueAtNextPoint,
            gradAtX, gradAtNextPoint, x, nextPoint, currFctEvalCount);
    }

    /// <summary>
    /// Constrained line search (see section 3.2 in the paper "Scalable Training
    /// of L1-Regularized Log-Linear Models", Andrew et al. 2007)
    /// </summary>
    public static void DoConstrainedLineSearch(IFunction function,
        double[] direction, LineSearchResult lsr, double l1Cost, double initialStepSize)
    {
        double stepSize = initialStepSize;
        int currFctEvalCount = lsr.FctEvalCount;
        double[] x = lsr.NextPoint;
        double[] signX = lsr.SignVector!; // existing sign vector
        double[] gradAtX = lsr.GradAtNext;
        double[] pseudoGradAtX = lsr.PseudoGradAtNext!;
        double valueAtX = lsr.ValueAtNext;
        int dimension = x.Length;

        // Retrieve current points and gradient for array reuse purpose
        double[] nextPoint = lsr.CurrPoint;
        double[] gradAtNextPoint = lsr.GradAtCurr;
        double valueAtNextPoint;

        double dirGradientAtX;

        // New sign vector
        for (int i = 0; i < dimension; i++)
        {
            signX[i] = x[i] == 0 ? -pseudoGradAtX[i] : x[i];
        }

        while (true)
        {
            // Get next point
            for (int i = 0; i < dimension; i++)
            {
                nextPoint[i] = x[i] + direction[i] * stepSize;
            }

            // Projection
            for (int i = 0; i < dimension; i++)
            {
                if (nextPoint[i] * signX[i] <= 0)
                    nextPoint[i] = 0;
            }

            // New value
            valueAtNextPoint = function.ValueAt(nextPoint) +
                l1Cost * ArrayMath.L1norm(nextPoint);

            currFctEvalCount++;

            dirGradientAtX = 0;
            for (int i = 0; i < dimension; i++)
            {
                dirGradientAtX += (nextPoint[i] - x[i]) * pseudoGradAtX[i];
            }

            // Check the sufficient decrease condition
            if (valueAtNextPoint <= valueAtX + C * dirGradientAtX)
                break;

            // Shrink step size
            stepSize *= RHO;
        }

        // Compute and save gradient at the new point
        Array.Copy(function.GradientAt(nextPoint), 0, gradAtNextPoint, 0,
            gradAtNextPoint.Length);

        // Update line search result
        lsr.SetAll(stepSize, valueAtX, valueAtNextPoint, gradAtX,
            gradAtNextPoint, pseudoGradAtX, x, nextPoint, signX, currFctEvalCount);
    }

    // ------------------------------------------------------------------------------------- //

    /// <summary>
    /// Class to store lineSearch result
    /// </summary>
    public class LineSearchResult
    {
        // NOpenNLP: the arrays are always assigned through SetAll, but the compiler
        // cannot see that through the call, so they start as empty rather than null.
        private double[] gradAtCurr = [];
        private double[] gradAtNext = [];
        private double[] currPoint = [];
        private double[] nextPoint = [];

        /// <summary>
        /// Constructor
        /// </summary>
        public LineSearchResult(
            double stepSize,
            double valueAtCurr,
            double valueAtNext,
            double[] gradAtCurr,
            double[] gradAtNext,
            double[] currPoint,
            double[] nextPoint,
            int fctEvalCount)
        {
            SetAll(stepSize, valueAtCurr, valueAtNext, gradAtCurr, gradAtNext,
                currPoint, nextPoint, fctEvalCount);
        }

        /// <summary>
        /// Constructor with sign vector
        /// </summary>
        public LineSearchResult(
            double stepSize,
            double valueAtCurr,
            double valueAtNext,
            double[] gradAtCurr,
            double[] gradAtNext,
            double[]? pseudoGradAtNext,
            double[] currPoint,
            double[] nextPoint,
            double[]? signVector,
            int fctEvalCount)
        {
            SetAll(stepSize, valueAtCurr, valueAtNext, gradAtCurr, gradAtNext,
                pseudoGradAtNext, currPoint, nextPoint, signVector, fctEvalCount);
        }

        /// <summary>
        /// Update line search elements
        /// </summary>
        public void SetAll(
            double stepSize,
            double valueAtCurr,
            double valueAtNext,
            double[] gradAtCurr,
            double[] gradAtNext,
            double[] currPoint,
            double[] nextPoint,
            int fctEvalCount)
        {
            SetAll(stepSize, valueAtCurr, valueAtNext, gradAtCurr, gradAtNext,
                null, currPoint, nextPoint, null, fctEvalCount);
        }

        /// <summary>
        /// Update line search elements
        /// </summary>
        public void SetAll(
            double stepSize,
            double valueAtCurr,
            double valueAtNext,
            double[] gradAtCurr,
            double[] gradAtNext,
            double[]? pseudoGradAtNext,
            double[] currPoint,
            double[] nextPoint,
            double[]? signVector,
            int fctEvalCount)
        {
            StepSize = stepSize;
            ValueAtCurr = valueAtCurr;
            ValueAtNext = valueAtNext;
            GradAtCurr = gradAtCurr;
            GradAtNext = gradAtNext;
            PseudoGradAtNext = pseudoGradAtNext;
            CurrPoint = currPoint;
            NextPoint = nextPoint;
            SignVector = signVector;
            FctEvalCount = fctEvalCount;
        }

        public double FuncChangeRate => (ValueAtCurr - ValueAtNext) / ValueAtCurr;

        public double StepSize { get; set; }

        public double ValueAtCurr { get; set; }

        public double ValueAtNext { get; set; }

        public double[] GradAtCurr
        {
            get => gradAtCurr;
            set => gradAtCurr = value;
        }

        public double[] GradAtNext
        {
            get => gradAtNext;
            set => gradAtNext = value;
        }

        public double[]? PseudoGradAtNext { get; set; }

        public double[] CurrPoint
        {
            get => currPoint;
            set => currPoint = value;
        }

        public double[] NextPoint
        {
            get => nextPoint;
            set => nextPoint = value;
        }

        public double[]? SignVector { get; set; }

        public int FctEvalCount { get; set; }

        /// <summary>
        /// Initial linear search object.
        /// </summary>
        public static LineSearchResult GetInitialObject(
            double valueAtX,
            double[] gradAtX,
            double[] x)
        {
            return GetInitialObject(valueAtX, gradAtX, null, x, null, 0);
        }

        /// <summary>
        /// Initial linear search object for L1-regularization.
        /// </summary>
        public static LineSearchResult GetInitialObjectForL1(
            double valueAtX,
            double[] gradAtX,
            double[] pseudoGradAtX,
            double[] x)
        {
            return GetInitialObject(valueAtX, gradAtX, pseudoGradAtX, x, new double[x.Length], 0);
        }

        public static LineSearchResult GetInitialObject(
            double valueAtX,
            double[] gradAtX,
            double[]? pseudoGradAtX,
            double[] x,
            double[]? signX,
            int fctEvalCount)
        {
            return new LineSearchResult(0.0, 0.0, valueAtX, new double[x.Length], gradAtX,
                pseudoGradAtX, new double[x.Length], x, signX, fctEvalCount);
        }
    }
}
