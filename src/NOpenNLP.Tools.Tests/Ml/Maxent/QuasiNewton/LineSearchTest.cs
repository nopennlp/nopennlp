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
using static NOpenNLP.Tools.Ml.Maxent.Quasinewton.LineSearch;

namespace NOpenNLP.Tools.Ml.Maxent.Quasinewton;

public class LineSearchTest
{
    private const double TOLERANCE = 0.01;

    [Test]
    public void TestLineSearchDeterminesSaneStepLength1()
    {
        IFunction objectiveFunction = new QuadraticFunction1();
        // given
        double[] testX = [0];
        double testValueX = objectiveFunction.ValueAt(testX);
        double[] testGradX = objectiveFunction.GradientAt(testX);
        double[] testDirection = [1];
        // when
        LineSearchResult lsr = LineSearchResult.GetInitialObject(testValueX, testGradX, testX);
        DoLineSearch(objectiveFunction, testDirection, lsr, 1.0);
        double stepSize = lsr.StepSize;
        // then
        bool succCond = TOLERANCE < stepSize && stepSize <= 1;
        ClassicAssert.IsTrue(succCond);
    }

    [Test]
    public void TestLineSearchDeterminesSaneStepLength2()
    {
        IFunction objectiveFunction = new QuadraticFunction2();
        // given
        double[] testX = [-2];
        double testValueX = objectiveFunction.ValueAt(testX);
        double[] testGradX = objectiveFunction.GradientAt(testX);
        double[] testDirection = [1];
        // when
        LineSearchResult lsr = LineSearchResult.GetInitialObject(testValueX, testGradX, testX);
        DoLineSearch(objectiveFunction, testDirection, lsr, 1.0);
        double stepSize = lsr.StepSize;
        // then
        bool succCond = TOLERANCE < stepSize && stepSize <= 1;
        ClassicAssert.IsTrue(succCond);
    }

    [Test]
    public void TestLineSearchFailsWithWrongDirection1()
    {
        IFunction objectiveFunction = new QuadraticFunction1();
        // given
        double[] testX = [0];
        double testValueX = objectiveFunction.ValueAt(testX);
        double[] testGradX = objectiveFunction.GradientAt(testX);
        double[] testDirection = [-1];
        // when
        LineSearchResult lsr = LineSearchResult.GetInitialObject(testValueX, testGradX, testX);
        DoLineSearch(objectiveFunction, testDirection, lsr, 1.0);
        double stepSize = lsr.StepSize;
        // then
        bool succCond = TOLERANCE < stepSize && stepSize <= 1;
        ClassicAssert.IsFalse(succCond);
        ClassicAssert.AreEqual(0.0, stepSize, TOLERANCE);
    }

    [Test]
    public void TestLineSearchFailsWithWrongDirection2()
    {
        IFunction objectiveFunction = new QuadraticFunction2();
        // given
        double[] testX = [-2];
        double testValueX = objectiveFunction.ValueAt(testX);
        double[] testGradX = objectiveFunction.GradientAt(testX);
        double[] testDirection = [-1];
        // when
        LineSearchResult lsr = LineSearchResult.GetInitialObject(testValueX, testGradX, testX);
        DoLineSearch(objectiveFunction, testDirection, lsr, 1.0);
        double stepSize = lsr.StepSize;
        // then
        bool succCond = TOLERANCE < stepSize && stepSize <= 1;
        ClassicAssert.IsFalse(succCond);
        ClassicAssert.AreEqual(0.0, stepSize, TOLERANCE);
    }

    [Test]
    public void TestLineSearchFailsWithWrongDirection3()
    {
        IFunction objectiveFunction = new QuadraticFunction1();
        // given
        double[] testX = [4];
        double testValueX = objectiveFunction.ValueAt(testX);
        double[] testGradX = objectiveFunction.GradientAt(testX);
        double[] testDirection = [1];
        // when
        LineSearchResult lsr = LineSearchResult.GetInitialObject(testValueX, testGradX, testX);
        DoLineSearch(objectiveFunction, testDirection, lsr, 1.0);
        double stepSize = lsr.StepSize;
        // then
        bool succCond = TOLERANCE < stepSize && stepSize <= 1;
        ClassicAssert.IsFalse(succCond);
        ClassicAssert.AreEqual(0.0, stepSize, TOLERANCE);
    }

    [Test]
    public void TestLineSearchFailsWithWrongDirection4()
    {
        IFunction objectiveFunction = new QuadraticFunction2();
        // given
        double[] testX = [2];
        double testValueX = objectiveFunction.ValueAt(testX);
        double[] testGradX = objectiveFunction.GradientAt(testX);
        double[] testDirection = [1];
        // when
        LineSearchResult lsr = LineSearchResult.GetInitialObject(testValueX, testGradX, testX);
        DoLineSearch(objectiveFunction, testDirection, lsr, 1.0);
        double stepSize = lsr.StepSize;
        // then
        bool succCond = TOLERANCE < stepSize && stepSize <= 1;
        ClassicAssert.IsFalse(succCond);
        ClassicAssert.AreEqual(0.0, stepSize, TOLERANCE);
    }

    [Test]
    public void TestLineSearchFailsAtMinimum1()
    {
        IFunction objectiveFunction = new QuadraticFunction2();
        // given
        double[] testX = [0];
        double testValueX = objectiveFunction.ValueAt(testX);
        double[] testGradX = objectiveFunction.GradientAt(testX);
        double[] testDirection = [-1];
        // when
        LineSearchResult lsr = LineSearchResult.GetInitialObject(testValueX, testGradX, testX);
        DoLineSearch(objectiveFunction, testDirection, lsr, 1.0);
        double stepSize = lsr.StepSize;
        // then
        bool succCond = TOLERANCE < stepSize && stepSize <= 1;
        ClassicAssert.IsFalse(succCond);
        ClassicAssert.AreEqual(0.0, stepSize, TOLERANCE);
    }

    [Test]
    public void TestLineSearchFailsAtMinimum2()
    {
        IFunction objectiveFunction = new QuadraticFunction2();
        // given
        double[] testX = [0];
        double testValueX = objectiveFunction.ValueAt(testX);
        double[] testGradX = objectiveFunction.GradientAt(testX);
        double[] testDirection = [1];
        // when
        LineSearchResult lsr = LineSearchResult.GetInitialObject(testValueX, testGradX, testX);
        DoLineSearch(objectiveFunction, testDirection, lsr, 1.0);
        double stepSize = lsr.StepSize;
        // then
        bool succCond = TOLERANCE < stepSize && stepSize <= 1;
        ClassicAssert.IsFalse(succCond);
        ClassicAssert.AreEqual(0.0, stepSize, TOLERANCE);
    }

    /// <summary>
    /// Quadratic function: f(x) = (x-2)^2 + 4
    /// </summary>
    public class QuadraticFunction1 : IFunction
    {
        public double ValueAt(double[] x)
        {
            // (x-2)^2 + 4;
            return Math.Pow(x[0] - 2, 2) + 4;
        }

        public double[] GradientAt(double[] x)
        {
            // 2(x-2)
            return [2 * (x[0] - 2)];
        }

        public int Dimension => 1;
    }

    /// <summary>
    /// Quadratic function: f(x) = x^2
    /// </summary>
    public class QuadraticFunction2 : IFunction
    {
        public double ValueAt(double[] x)
        {
            // x^2;
            return Math.Pow(x[0], 2);
        }

        public double[] GradientAt(double[] x)
        {
            // 2x
            return [2 * x[0]];
        }

        public int Dimension => 1;
    }
}
