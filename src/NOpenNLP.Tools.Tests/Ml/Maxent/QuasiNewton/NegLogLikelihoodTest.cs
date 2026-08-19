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
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Maxent.Quasinewton;

public class NegLogLikelihoodTest
{
    private const double TOLERANCE01 = 1.0E-06;
    private const double TOLERANCE02 = 1.0E-10;

    private IDataIndexer testDataIndexer = null!;

    [SetUp]
    public void InitIndexer()
    {
        TrainingParameters trainingParameters = new();
        trainingParameters.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        testDataIndexer = new OnePassRealValueDataIndexer();
        testDataIndexer.Init(trainingParameters, new Dictionary<string, string>());
    }

    // NOpenNLP: upstream reads the corpus from a path under src/test/resources. It is
    // an embedded resource here, so it is copied to a temporary file for the
    // file-based event stream to open.
    private NegLogLikelihood CreateObjectFunction()
    {
        using TempResourceFile data =
            new("/data/opennlp/maxent/real-valued-weights-training-data.txt");
        using RealValueFileEventStream rvfes1 = new(data.Path);
        testDataIndexer.Index(rvfes1);
        return new NegLogLikelihood(testDataIndexer);
    }

    [Test]
    public void TestDomainDimensionSanity()
    {
        // given
        NegLogLikelihood objectFunction = CreateObjectFunction();
        // when
        int correctDomainDimension = testDataIndexer.PredLabels.Length
            * testDataIndexer.OutcomeLabels.Length;
        // then
        ClassicAssert.AreEqual(correctDomainDimension, objectFunction.Dimension);
    }

    [Test]
    public void TestInitialSanity()
    {
        // given
        NegLogLikelihood objectFunction = CreateObjectFunction();
        // when
        double[] initial = objectFunction.GetInitialPoint();
        // then
        foreach (double anInitial in initial)
        {
            ClassicAssert.AreEqual(0.0, anInitial, TOLERANCE01);
        }
    }

    [Test]
    public void TestGradientSanity()
    {
        // given
        NegLogLikelihood objectFunction = CreateObjectFunction();
        // when
        double[] initial = objectFunction.GetInitialPoint();
        double[] gradientAtInitial = objectFunction.GradientAt(initial);
        // then
        ClassicAssert.NotNull(gradientAtInitial);
    }

    [Test]
    public void TestValueAtInitialPoint()
    {
        // given
        NegLogLikelihood objectFunction = CreateObjectFunction();
        // when
        double value = objectFunction.ValueAt(objectFunction.GetInitialPoint());
        double expectedValue = 13.86294361;
        // then
        ClassicAssert.AreEqual(expectedValue, value, TOLERANCE01);
    }

    [Test]
    public void TestValueAtNonInitialPoint01()
    {
        // given
        NegLogLikelihood objectFunction = CreateObjectFunction();
        // when
        double[] nonInitialPoint = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
        double value = objectFunction.ValueAt(nonInitialPoint);
        double expectedValue = 13.862943611198894;
        // then
        ClassicAssert.AreEqual(expectedValue, value, TOLERANCE01);
    }

    [Test]
    public void TestValueAtNonInitialPoint02()
    {
        // given
        NegLogLikelihood objectFunction = CreateObjectFunction();
        // when
        double[] nonInitialPoint = [3, 2, 3, 2, 3, 2, 3, 2, 3, 2];
        double value = objectFunction.ValueAt(DealignDoubleArrayForTestData(nonInitialPoint,
            testDataIndexer.PredLabels,
            testDataIndexer.OutcomeLabels));
        double expectedValue = 53.163219721099026;
        // then
        ClassicAssert.AreEqual(expectedValue, value, TOLERANCE02);
    }

    [Test]
    public void TestGradientAtInitialPoint()
    {
        // given
        NegLogLikelihood objectFunction = CreateObjectFunction();
        // when
        double[] gradientAtInitialPoint = objectFunction.GradientAt(objectFunction.GetInitialPoint());
        double[] expectedGradient = [-9.0, -14.0, -17.0, 20.0, 8.5, 9.0, 14.0, 17.0, -20.0, -8.5];
        // then
        ClassicAssert.IsTrue(CompareDoubleArray(expectedGradient, gradientAtInitialPoint,
            testDataIndexer, TOLERANCE01));
    }

    [Test]
    public void TestGradientAtNonInitialPoint()
    {
        // given
        NegLogLikelihood objectFunction = CreateObjectFunction();
        // when
        double[] nonInitialPoint = [0.2, 0.5, 0.2, 0.5, 0.2, 0.5, 0.2, 0.5, 0.2, 0.5];
        double[] gradientAtNonInitialPoint =
            objectFunction.GradientAt(DealignDoubleArrayForTestData(nonInitialPoint,
                testDataIndexer.PredLabels,
                testDataIndexer.OutcomeLabels));
        double[] expectedGradient =
        [
            -12.755042847945553, -21.227127506102434,
            -72.57790706276435, 38.03525795198456,
            15.348650889354925, 12.755042847945557,
            21.22712750610244, 72.57790706276438,
            -38.03525795198456, -15.348650889354925
        ];
        // then
        ClassicAssert.IsTrue(CompareDoubleArray(expectedGradient, gradientAtNonInitialPoint,
            testDataIndexer, TOLERANCE01));
    }

    private static double[] AlignDoubleArrayForTestData(double[] expected,
        string[] predLabels, string[] outcomeLabels)
    {
        double[] aligned = new double[predLabels.Length * outcomeLabels.Length];

        string[] sortedPredLabels = (string[])predLabels.Clone();
        string[] sortedOutcomeLabels = (string[])outcomeLabels.Clone();
        Array.Sort(sortedPredLabels, StringComparer.Ordinal);
        Array.Sort(sortedOutcomeLabels, StringComparer.Ordinal);

        Dictionary<string, int> invertedPredIndex = [];
        Dictionary<string, int> invertedOutcomeIndex = [];
        for (int i = 0; i < predLabels.Length; i++)
        {
            invertedPredIndex[predLabels[i]] = i;
        }

        for (int i = 0; i < outcomeLabels.Length; i++)
        {
            invertedOutcomeIndex[outcomeLabels[i]] = i;
        }

        for (int i = 0; i < sortedOutcomeLabels.Length; i++)
        {
            for (int j = 0; j < sortedPredLabels.Length; j++)
            {
                aligned[i * sortedPredLabels.Length + j] =
                    expected[invertedOutcomeIndex[sortedOutcomeLabels[i]]
                        * sortedPredLabels.Length
                        + invertedPredIndex[sortedPredLabels[j]]];
            }
        }

        return aligned;
    }

    private static double[] DealignDoubleArrayForTestData(double[] expected,
        string[] predLabels, string[] outcomeLabels)
    {
        double[] dealigned = new double[predLabels.Length * outcomeLabels.Length];

        string[] sortedPredLabels = (string[])predLabels.Clone();
        string[] sortedOutcomeLabels = (string[])outcomeLabels.Clone();
        Array.Sort(sortedPredLabels, StringComparer.Ordinal);
        Array.Sort(sortedOutcomeLabels, StringComparer.Ordinal);

        Dictionary<string, int> invertedPredIndex = [];
        Dictionary<string, int> invertedOutcomeIndex = [];
        for (int i = 0; i < predLabels.Length; i++)
        {
            invertedPredIndex[predLabels[i]] = i;
        }

        for (int i = 0; i < outcomeLabels.Length; i++)
        {
            invertedOutcomeIndex[outcomeLabels[i]] = i;
        }

        for (int i = 0; i < sortedOutcomeLabels.Length; i++)
        {
            for (int j = 0; j < sortedPredLabels.Length; j++)
            {
                dealigned[invertedOutcomeIndex[sortedOutcomeLabels[i]]
                    * sortedPredLabels.Length
                    + invertedPredIndex[sortedPredLabels[j]]] =
                    expected[i * sortedPredLabels.Length + j];
            }
        }

        return dealigned;
    }

    private static bool CompareDoubleArray(double[] expected, double[] actual,
        IDataIndexer indexer, double tolerance)
    {
        double[] alignedActual = AlignDoubleArrayForTestData(
            actual, indexer.PredLabels, indexer.OutcomeLabels);

        if (expected.Length != alignedActual.Length)
        {
            return false;
        }

        for (int i = 0; i < alignedActual.Length; i++)
        {
            if (Math.Abs(alignedActual[i] - expected[i]) > tolerance)
            {
                return false;
            }
        }

        return true;
    }
}
