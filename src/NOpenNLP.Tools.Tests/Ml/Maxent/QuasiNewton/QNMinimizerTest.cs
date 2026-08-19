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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Maxent.Quasinewton;

public class QNMinimizerTest
{
    [Test]
    public void TestQuadraticFunction()
    {
        QNMinimizer minimizer = new();
        IFunction f = new QuadraticFunction();
        double[] x = minimizer.Minimize(f);
        double minValue = f.ValueAt(x);

        ClassicAssert.AreEqual(x[0], 1.0, 1e-5);
        ClassicAssert.AreEqual(x[1], 5.0, 1e-5);
        ClassicAssert.AreEqual(minValue, 10.0, 1e-10);
    }

    [Test]
    public void TestRosenbrockFunction()
    {
        QNMinimizer minimizer = new();
        IFunction f = new Rosenbrock();
        double[] x = minimizer.Minimize(f);
        double minValue = f.ValueAt(x);

        ClassicAssert.AreEqual(x[0], 1.0, 1e-5);
        ClassicAssert.AreEqual(x[1], 1.0, 1e-5);
        ClassicAssert.AreEqual(minValue, 0, 1e-10);
    }

    /// <summary>
    /// Quadratic function: f(x,y) = (x-1)^2 + (y-5)^2 + 10
    /// </summary>
    public class QuadraticFunction : IFunction
    {
        public int Dimension => 2;

        public double ValueAt(double[] x) =>
            Math.Pow(x[0] - 1, 2) + Math.Pow(x[1] - 5, 2) + 10;

        public double[] GradientAt(double[] x) => [2 * (x[0] - 1), 2 * (x[1] - 5)];
    }

    /// <summary>
    /// Rosenbrock function (http://en.wikipedia.org/wiki/Rosenbrock_function)
    /// f(x,y) = (1-x)^2 + 100*(y-x^2)^2
    /// f(x,y) is non-convex and has global minimum at (x,y) = (1,1) where f(x,y) = 0
    /// <para/>
    /// f_x = -2*(1-x) - 400*(y-x^2)*x
    /// f_y = 200*(y-x^2)
    /// </summary>
    public class Rosenbrock : IFunction
    {
        public int Dimension => 2;

        public double ValueAt(double[] x) =>
            Math.Pow(1 - x[0], 2) + 100 * Math.Pow(x[1] - Math.Pow(x[0], 2), 2);

        public double[] GradientAt(double[] x)
        {
            double[] g = new double[2];
            g[0] = -2 * (1 - x[0]) - 400 * (x[1] - Math.Pow(x[0], 2)) * x[0];
            g[1] = 200 * (x[1] - Math.Pow(x[0], 2));
            return g;
        }
    }
}
