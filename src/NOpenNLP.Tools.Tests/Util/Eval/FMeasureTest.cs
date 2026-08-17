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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Eval;

/// <summary>
/// Tests for the <see cref="FMeasure"/> class.
/// </summary>
public class FMeasureTest
{
    private const double DELTA = 1.0E-9d;

    private readonly Span[] gold =
    [
        new Span(8, 9),
        new Span(9, 10),
        new Span(10, 12),
        new Span(13, 14),
        new Span(14, 15),
        new Span(15, 16)
    ];

    private readonly Span[] predicted =
    [
        new Span(14, 15),
        new Span(15, 16),
        new Span(100, 120),
        new Span(210, 220),
        new Span(220, 230)
    ];

    private readonly Span[] predictedCompletelyDistinct =
    [
        new Span(100, 120),
        new Span(210, 220),
        new Span(211, 220),
        new Span(212, 220),
        new Span(220, 230)
    ];

    private readonly Span[] goldToMerge =
    [
        new Span(8, 9),
        new Span(9, 10),
        new Span(11, 11),
        new Span(13, 14),
        new Span(14, 15),
        new Span(15, 16),
        new Span(18, 19),
    ];

    private readonly Span[] predictedToMerge =
    [
        new Span(8, 9),
        new Span(14, 15),
        new Span(15, 16),
        new Span(100, 120),
        new Span(210, 220),
        new Span(220, 230)
    ];

    /// <summary>
    /// Test for the <see cref="FMeasure.CountTruePositives"/> method.
    /// </summary>
    [Test]
    public void TestCountTruePositives()
    {
        ClassicAssert.AreEqual(0, FMeasure.CountTruePositives([], []));
        ClassicAssert.AreEqual(gold.Length, FMeasure.CountTruePositives(gold, gold));
        ClassicAssert.AreEqual(0, FMeasure.CountTruePositives(gold, predictedCompletelyDistinct));
        ClassicAssert.AreEqual(2, FMeasure.CountTruePositives(gold, predicted));
    }

    /// <summary>
    /// Test for the <see cref="FMeasure.Precision"/> method.
    /// </summary>
    [Test]
    public void TestPrecision()
    {
        ClassicAssert.AreEqual(1.0d, FMeasure.Precision(gold, gold), DELTA);
        ClassicAssert.AreEqual(0, FMeasure.Precision(gold, predictedCompletelyDistinct), DELTA);
        ClassicAssert.AreEqual(double.NaN, FMeasure.Precision(gold, []), DELTA);
        ClassicAssert.AreEqual(0, FMeasure.Precision([], gold), DELTA);
        ClassicAssert.AreEqual(2d / predicted.Length, FMeasure.Precision(gold, predicted), DELTA);
    }

    /// <summary>
    /// Test for the <see cref="FMeasure.Recall"/> method.
    /// </summary>
    [Test]
    public void TestRecall()
    {
        ClassicAssert.AreEqual(1.0d, FMeasure.Recall(gold, gold), DELTA);
        ClassicAssert.AreEqual(0, FMeasure.Recall(gold, predictedCompletelyDistinct), DELTA);
        ClassicAssert.AreEqual(0, FMeasure.Recall(gold, []), DELTA);
        ClassicAssert.AreEqual(double.NaN, FMeasure.Recall([], gold), DELTA);
        ClassicAssert.AreEqual(2d / gold.Length, FMeasure.Recall(gold, predicted), DELTA);
    }

    [Test]
    public void TestEmpty()
    {
        FMeasure fm = new();
        ClassicAssert.AreEqual(-1, fm.Value, DELTA);
        ClassicAssert.AreEqual(0, fm.RecallScore, DELTA);
        ClassicAssert.AreEqual(0, fm.PrecisionScore, DELTA);
    }

    [Test]
    public void TestPerfect()
    {
        FMeasure fm = new();
        fm.UpdateScores(gold, gold);
        ClassicAssert.AreEqual(1, fm.Value, DELTA);
        ClassicAssert.AreEqual(1, fm.RecallScore, DELTA);
        ClassicAssert.AreEqual(1, fm.PrecisionScore, DELTA);
    }

    [Test]
    public void TestMerge()
    {
        FMeasure fm = new();
        fm.UpdateScores(gold, predicted);
        fm.UpdateScores(goldToMerge, predictedToMerge);

        FMeasure fmMerge = new();
        fmMerge.UpdateScores(gold, predicted);
        FMeasure toMerge = new();
        toMerge.UpdateScores(goldToMerge, predictedToMerge);
        fmMerge.MergeInto(toMerge);

        double selected1 = predicted.Length;
        double target1 = gold.Length;
        double tp1 = FMeasure.CountTruePositives(gold, predicted);

        double selected2 = predictedToMerge.Length;
        double target2 = goldToMerge.Length;
        double tp2 = FMeasure.CountTruePositives(goldToMerge, predictedToMerge);

        ClassicAssert.AreEqual((tp1 + tp2) / (target1 + target2), fm.RecallScore, DELTA);
        ClassicAssert.AreEqual((tp1 + tp2) / (selected1 + selected2), fm.PrecisionScore, DELTA);

        ClassicAssert.AreEqual(fm.RecallScore, fmMerge.RecallScore, DELTA);
        ClassicAssert.AreEqual(fm.PrecisionScore, fmMerge.PrecisionScore, DELTA);
    }
}
