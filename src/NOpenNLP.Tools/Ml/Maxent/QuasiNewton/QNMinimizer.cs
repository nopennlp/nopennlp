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
using System.Diagnostics;
using System.Globalization;
using static NOpenNLP.Tools.Ml.Maxent.Quasinewton.LineSearch;

namespace NOpenNLP.Tools.Ml.Maxent.Quasinewton;

/// <summary>
/// Implementation of L-BFGS which supports L1-, L2-regularization
/// and Elastic Net for solving convex optimization problems.
/// <para/>
/// Usage example:
/// <code>
///  // Quadratic function f(x) = (x-1)^2 + 10
///  // f obtains its minimum value 10 at x = 1
///  IFunction f = new QuadraticFunction();
///
///  QNMinimizer minimizer = new QNMinimizer();
///  double[] x = minimizer.Minimize(f);
///  double min = f.ValueAt(x);
/// </code>
/// </summary>
public class QNMinimizer
{
    // Function change rate tolerance
    public const double CONVERGE_TOLERANCE = 1e-4;

    // Relative gradient norm tolerance
    public const double REL_GRAD_NORM_TOL = 1e-4;

    // Initial step size
    public const double INITIAL_STEP_SIZE = 1.0;

    // Minimum step size
    public const double MIN_STEP_SIZE = 1e-10;

    // Default L1-cost
    public const double L1COST_DEFAULT = 0;

    // Default L2-cost
    public const double L2COST_DEFAULT = 0;

    // Default number of iterations
    public const int NUM_ITERATIONS_DEFAULT = 100;

    // Default number of Hessian updates to store
    public const int M_DEFAULT = 15;

    // Default maximum number of function evaluations
    public const int MAX_FCT_EVAL_DEFAULT = 30000;

    // L1-regularization cost
    private readonly double l1Cost; // NOpenNLP: made readonly

    // L2-regularization cost
    private readonly double l2Cost; // NOpenNLP: made readonly

    // Maximum number of iterations
    private readonly int iterations; // NOpenNLP: made readonly

    // Number of Hessian updates to store
    private readonly int m; // NOpenNLP: made readonly

    // Maximum number of function evaluations
    private readonly int maxFctEval; // NOpenNLP: made readonly

    // Verbose output
    private readonly bool verbose; // NOpenNLP: made readonly

    // Objective function's dimension
    private int dimension;

    // Hessian updates
    private UpdateInfo? updateInfo;

    public QNMinimizer()
        : this(L1COST_DEFAULT, L2COST_DEFAULT)
    {
    }

    public QNMinimizer(double l1Cost, double l2Cost)
        : this(l1Cost, l2Cost, NUM_ITERATIONS_DEFAULT)
    {
    }

    public QNMinimizer(double l1Cost, double l2Cost, int iterations)
        : this(l1Cost, l2Cost, iterations, M_DEFAULT, MAX_FCT_EVAL_DEFAULT)
    {
    }

    public QNMinimizer(double l1Cost, double l2Cost,
        int iterations, int m, int maxFctEval)
        : this(l1Cost, l2Cost, iterations, m, maxFctEval, true)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="l1Cost">L1-regularization cost</param>
    /// <param name="l2Cost">L2-regularization cost</param>
    /// <param name="iterations">maximum number of iterations</param>
    /// <param name="m">number of Hessian updates to store</param>
    /// <param name="maxFctEval">maximum number of function evaluations</param>
    /// <param name="verbose">verbose output</param>
    public QNMinimizer(double l1Cost, double l2Cost, int iterations,
        int m, int maxFctEval, bool verbose)
    {
        // Check arguments
        if (l1Cost < 0 || l2Cost < 0)
            throw new ArgumentException(
                "L1-cost and L2-cost must not be less than zero");

        if (iterations <= 0)
            throw new ArgumentException(
                "Number of iterations must be larger than zero");

        if (m <= 0)
            throw new ArgumentException(
                "Number of Hessian updates must be larger than zero");

        if (maxFctEval <= 0)
            throw new ArgumentException(
                "Maximum number of function evaluations must be larger than zero");

        this.l1Cost = l1Cost;
        this.l2Cost = l2Cost;
        this.iterations = iterations;
        this.m = m;
        this.maxFctEval = maxFctEval;
        this.verbose = verbose;
    }

    /// <summary>
    /// For evaluating quality of training parameters.
    /// This is optional and can be omitted.
    /// </summary>
    public IEvaluator? Evaluator { get; set; }

    /// <summary>
    /// Find the parameters that minimize the objective function
    /// </summary>
    /// <param name="function">objective function</param>
    /// <returns>minimizing parameters</returns>
    public double[] Minimize(IFunction function)
    {
        IFunction l2RegFunction = new L2RegFunction(function, l2Cost);
        this.dimension = l2RegFunction.Dimension;
        this.updateInfo = new UpdateInfo(this.m, this.dimension);

        // Current point is at the origin
        double[] currPoint = new double[dimension];

        double currValue = l2RegFunction.ValueAt(currPoint);

        // Gradient at the current point
        double[] currGrad = new double[dimension];
        Array.Copy(l2RegFunction.GradientAt(currPoint), 0,
            currGrad, 0, dimension);

        // Pseudo-gradient - only use when L1-regularization is enabled
        double[]? pseudoGrad = null;
        if (l1Cost > 0)
        {
            currValue += l1Cost * ArrayMath.L1norm(currPoint);
            pseudoGrad = new double[dimension];
            ComputePseudoGrad(currPoint, currGrad, pseudoGrad);
        }

        LineSearchResult lsr;
        if (l1Cost > 0)
        {
            lsr = LineSearchResult.GetInitialObjectForL1(
                currValue, currGrad, pseudoGrad!, currPoint);
        }
        else
        {
            lsr = LineSearchResult.GetInitialObject(
                currValue, currGrad, currPoint);
        }

        if (verbose)
        {
            Display("\nSolving convex optimization problem.");
            Display("\nObjective function has " + dimension + " variable(s).");
            // NOpenNLP: Java's Double.toString always renders a decimal point, so an
            // integral cost prints "0.0" where .NET's default formatting prints "0".
            // J2N's "J" format reproduces Java's rendering.
            Display("\n\nPerforming " + iterations + " iterations with "
                + "L1Cost=" + J2N.Numerics.Double.ToString(l1Cost, "J", CultureInfo.InvariantCulture)
                + " and L2Cost=" + J2N.Numerics.Double.ToString(l2Cost, "J", CultureInfo.InvariantCulture) + "\n");
        }

        double[] direction = new double[dimension];
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Initial step size for the 1st iteration
        double initialStepSize = l1Cost > 0
            ? ArrayMath.InvL2norm(lsr.PseudoGradAtNext!)
            : ArrayMath.InvL2norm(lsr.GradAtNext);

        for (int iter = 1; iter <= iterations; iter++)
        {
            // Find direction
            if (l1Cost > 0)
            {
                Array.Copy(lsr.PseudoGradAtNext!, 0, direction, 0, direction.Length);
            }
            else
            {
                Array.Copy(lsr.GradAtNext, 0, direction, 0, direction.Length);
            }

            ComputeDirection(direction);

            // Line search
            if (l1Cost > 0)
            {
                // Constrain the search direction
                pseudoGrad = lsr.PseudoGradAtNext!;
                for (int i = 0; i < dimension; i++)
                {
                    if (direction[i] * pseudoGrad[i] >= 0)
                    {
                        direction[i] = 0;
                    }
                }

                DoConstrainedLineSearch(l2RegFunction, direction, lsr, l1Cost, initialStepSize);
                ComputePseudoGrad(lsr.NextPoint, lsr.GradAtNext, pseudoGrad);
                lsr.PseudoGradAtNext = pseudoGrad;
            }
            else
            {
                DoLineSearch(l2RegFunction, direction, lsr, initialStepSize);
            }

            // Save Hessian updates
            updateInfo.Update(lsr);

            if (verbose)
            {
                if (iter < 10)
                    Display("  " + iter + ":  ");
                else if (iter < 100)
                    Display(" " + iter + ":  ");
                else
                    Display(iter + ":  ");

                // NOpenNLP: Java's Double.toString always renders a decimal point and
                // formats exponents differently from .NET, so these values print as
                // "1.0" and "1.0E-5" upstream. J2N's "J" format reproduces that.
                if (Evaluator != null)
                {
                    Display("\t" + J2N.Numerics.Double.ToString(lsr.ValueAtNext, "J", CultureInfo.InvariantCulture)
                        + "\t" + J2N.Numerics.Double.ToString(lsr.FuncChangeRate, "J", CultureInfo.InvariantCulture)
                        + "\t" + J2N.Numerics.Double.ToString(
                            Evaluator.Evaluate(lsr.NextPoint), "J", CultureInfo.InvariantCulture)
                        + "\n");
                }
                else
                {
                    Display("\t " + J2N.Numerics.Double.ToString(lsr.ValueAtNext, "J", CultureInfo.InvariantCulture)
                        + "\t" + J2N.Numerics.Double.ToString(lsr.FuncChangeRate, "J", CultureInfo.InvariantCulture)
                        + "\n");
                }
            }

            if (IsConverged(lsr))
                break;

            initialStepSize = INITIAL_STEP_SIZE;
        }

        // Undo L2-shrinkage if Elastic Net is used (since
        // in that case, the shrinkage is done twice)
        if (l1Cost > 0 && l2Cost > 0)
        {
            double[] x = lsr.NextPoint;
            for (int i = 0; i < dimension; i++)
            {
                x[i] = Math.Sqrt(1 + l2Cost) * x[i];
            }
        }

        stopwatch.Stop();
        // NOpenNLP: Java's Double.toString always renders a decimal point, so a whole
        // number of seconds prints "3.0" where .NET's default formatting prints "3".
        // J2N's "J" format reproduces Java's rendering.
        Display("Running time: "
            + J2N.Numerics.Double.ToString(stopwatch.ElapsedMilliseconds / 1000.0, "J", CultureInfo.InvariantCulture)
            + "s\n");

        // Release memory
        // NOpenNLP: upstream also calls System.gc() here; forcing a collection is not
        // something a library should do, so dropping the reference is all we do.
        this.updateInfo = null;

        // Avoid returning the reference to LineSearchResult's member so that GC can
        // collect memory occupied by lsr after this function completes (is it necessary?)
        double[] parameters = new double[dimension];
        Array.Copy(lsr.NextPoint, 0, parameters, 0, dimension);

        return parameters;
    }

    /// <summary>
    /// Pseudo-gradient for L1-regularization (see equation 4 in the paper
    /// "Scalable Training of L1-Regularized Log-Linear Models", Andrew et al. 2007)
    /// </summary>
    /// <param name="x">current point</param>
    /// <param name="g">gradient at x</param>
    /// <param name="pg">pseudo-gradient at x which is to be computed</param>
    private void ComputePseudoGrad(double[] x, double[] g, double[] pg)
    {
        for (int i = 0; i < dimension; i++)
        {
            if (x[i] < 0)
            {
                pg[i] = g[i] - l1Cost;
            }
            else if (x[i] > 0)
            {
                pg[i] = g[i] + l1Cost;
            }
            else
            {
                if (g[i] < -l1Cost)
                {
                    // right partial derivative
                    pg[i] = g[i] + l1Cost;
                }
                else if (g[i] > l1Cost)
                {
                    // left partial derivative
                    pg[i] = g[i] - l1Cost;
                }
                else
                {
                    pg[i] = 0;
                }
            }
        }
    }

    /// <summary>
    /// L-BFGS two-loop recursion (see Nocedal &amp; Wright 2006, Numerical Optimization, p. 178)
    /// </summary>
    private void ComputeDirection(double[] direction)
    {
        // Implemented two-loop Hessian update method.
        int k = updateInfo!.KCounter;
        double[] rho = updateInfo.Rho;
        double[] alpha = updateInfo.Alpha; // just to avoid recreating alpha
        double[][] S = updateInfo.S;
        double[][] Y = updateInfo.Y;

        // First loop
        for (int i = k - 1; i >= 0; i--)
        {
            alpha[i] = rho[i] * ArrayMath.InnerProduct(S[i], direction);
            for (int j = 0; j < dimension; j++)
            {
                direction[j] = direction[j] - alpha[i] * Y[i][j];
            }
        }

        // Second loop
        for (int i = 0; i < k; i++)
        {
            double beta = rho[i] * ArrayMath.InnerProduct(Y[i], direction);
            for (int j = 0; j < dimension; j++)
            {
                direction[j] = direction[j] + S[i][j] * (alpha[i] - beta);
            }
        }

        for (int i = 0; i < dimension; i++)
        {
            direction[i] = -direction[i];
        }
    }

    private bool IsConverged(LineSearchResult lsr)
    {
        // Check function's change rate
        if (lsr.FuncChangeRate < CONVERGE_TOLERANCE)
        {
            if (verbose)
                // NOpenNLP: Java's Double.toString renders this threshold as "1.0E-4"
                // where .NET's default formatting gives "0.0001". J2N's "J" format
                // reproduces Java's rendering.
                Display("Function change rate is smaller than the threshold "
                    + J2N.Numerics.Double.ToString(CONVERGE_TOLERANCE, "J", CultureInfo.InvariantCulture)
                    + ".\nTraining will stop.\n\n");
            return true;
        }

        // Check gradient's norm using the criteria: ||g(x)|| / max(1, ||x||) < threshold
        double xNorm = Math.Max(1, ArrayMath.L2norm(lsr.NextPoint));
        double gradNorm = l1Cost > 0
            ? ArrayMath.L2norm(lsr.PseudoGradAtNext!)
            : ArrayMath.L2norm(lsr.GradAtNext);
        if (gradNorm / xNorm < REL_GRAD_NORM_TOL)
        {
            if (verbose)
                // NOpenNLP: Java's Double.toString renders this threshold as "1.0E-4"
                // where .NET's default formatting gives "0.0001". J2N's "J" format
                // reproduces Java's rendering.
                Display("Relative L2-norm of the gradient is smaller than the threshold "
                    + J2N.Numerics.Double.ToString(REL_GRAD_NORM_TOL, "J", CultureInfo.InvariantCulture)
                    + ".\nTraining will stop.\n\n");
            return true;
        }

        // Check step size
        if (lsr.StepSize < MIN_STEP_SIZE)
        {
            if (verbose)
                // NOpenNLP: Java's Double.toString renders this threshold as "1.0E-10"
                // where .NET's default formatting gives "1E-10". J2N's "J" format
                // reproduces Java's rendering.
                Display("Step size is smaller than the minimum step size "
                    + J2N.Numerics.Double.ToString(MIN_STEP_SIZE, "J", CultureInfo.InvariantCulture)
                    + ".\nTraining will stop.\n\n");
            return true;
        }

        // Check number of function evaluations
        if (lsr.FctEvalCount > this.maxFctEval)
        {
            if (verbose)
                Display("Maximum number of function evaluations has exceeded the threshold "
                    + this.maxFctEval + ".\nTraining will stop.\n\n");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Shorthand for Console.Write
    /// </summary>
    private static void Display(string s) => Console.Write(s);

    /// <summary>
    /// Class to store vectors for Hessian approximation update.
    /// </summary>
    private sealed class UpdateInfo
    {
        private readonly int m; // NOpenNLP: made readonly
        private readonly int dimension; // NOpenNLP-specific: the outer class's dimension is not visible from a nested type in C#

        // Constructor
        internal UpdateInfo(int numCorrection, int dimension)
        {
            this.m = numCorrection;
            this.dimension = dimension;
            KCounter = 0;
            S = new double[this.m][];
            Y = new double[this.m][];
            for (int i = 0; i < this.m; i++)
            {
                S[i] = new double[dimension];
                Y[i] = new double[dimension];
            }

            Rho = new double[this.m];
            Alpha = new double[this.m];
        }

        internal double[][] S { get; }

        internal double[][] Y { get; }

        internal double[] Rho { get; }

        internal double[] Alpha { get; }

        internal int KCounter { get; private set; }

        internal void Update(LineSearchResult lsr)
        {
            double[] currPoint = lsr.CurrPoint;
            double[] gradAtCurr = lsr.GradAtCurr;
            double[] nextPoint = lsr.NextPoint;
            double[] gradAtNext = lsr.GradAtNext;

            // Inner product of S_k and Y_k
            double SYk = 0.0;

            // Add new ones.
            if (KCounter < m)
            {
                for (int j = 0; j < dimension; j++)
                {
                    S[KCounter][j] = nextPoint[j] - currPoint[j];
                    Y[KCounter][j] = gradAtNext[j] - gradAtCurr[j];
                    SYk += S[KCounter][j] * Y[KCounter][j];
                }

                Rho[KCounter] = 1.0 / SYk;
            }
            else
            {
                // Discard oldest vectors and add new ones.
                for (int i = 0; i < m - 1; i++)
                {
                    S[i] = S[i + 1];
                    Y[i] = Y[i + 1];
                    Rho[i] = Rho[i + 1];
                }

                for (int j = 0; j < dimension; j++)
                {
                    S[m - 1][j] = nextPoint[j] - currPoint[j];
                    Y[m - 1][j] = gradAtNext[j] - gradAtCurr[j];
                    SYk += S[m - 1][j] * Y[m - 1][j];
                }

                Rho[m - 1] = 1.0 / SYk;
            }

            if (KCounter < m)
                KCounter++;
        }
    }

    /// <summary>
    /// L2-regularized objective function
    /// </summary>
    public class L2RegFunction(IFunction f, double l2Cost) : IFunction
    {
        public int Dimension => f.Dimension;

        public double ValueAt(double[] x)
        {
            CheckDimension(x);
            double value = f.ValueAt(x);
            if (l2Cost > 0)
            {
                value += l2Cost * ArrayMath.InnerProduct(x, x);
            }

            return value;
        }

        public double[] GradientAt(double[] x)
        {
            CheckDimension(x);
            double[] gradient = f.GradientAt(x);
            if (l2Cost > 0)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    gradient[i] += 2 * l2Cost * x[i];
                }
            }

            return gradient;
        }

        private void CheckDimension(double[] x)
        {
            if (x.Length != Dimension)
                throw new ArgumentException(
                    "x's dimension is not the same as function's dimension");
        }
    }

    /// <summary>
    /// Evaluate quality of training parameters. For example,
    /// it can be used to report model's training accuracy when
    /// we train a Maximum Entropy classifier.
    /// </summary>
    public interface IEvaluator
    {
        /// <summary>
        /// Measure quality of the training parameters
        /// </summary>
        /// <param name="parameters">The training parameters to evaluate.</param>
        /// <returns>evaluated result</returns>
        double Evaluate(double[] parameters);
    }
}
