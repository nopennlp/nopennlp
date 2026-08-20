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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Langdetect;

public class LanguageDetectorCrossValidatorTest
{
    [Test]
    public void Evaluate()
    {
        TrainingParameters @params = new();
        @params.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        @params.Put(TrainingParameters.CUTOFF_PARAM, 5);
        @params.Put("PrintMessages", false);

        int correctCount = 0;
        int incorrectCount = 0;

        LanguageDetectorCrossValidator cv = new(@params,
            new LanguageDetectorFactory(),
            new CountingMonitor(() => correctCount++, () => incorrectCount++));

        LanguageDetectorSampleStream sampleStream = LanguageDetectorMETest.CreateSampleStream();

        cv.Evaluate(sampleStream, 2);

        ClassicAssert.AreEqual(99, cv.DocumentCount);
        ClassicAssert.AreEqual(0.98989898989899, cv.DocumentAccuracy, 0.01);
    }

    /// <summary>
    /// NOpenNLP: upstream uses an anonymous <c>LanguageDetectorEvaluationMonitor</c>
    /// that bumps two <c>AtomicInteger</c>s. C# has no anonymous classes, so the
    /// counting is delegated to the callbacks the test supplies.
    /// </summary>
    private sealed class CountingMonitor(Action onCorrect, Action onIncorrect)
        : ILanguageDetectorEvaluationMonitor
    {
        public void CorrectlyClassified(LanguageSample reference, LanguageSample prediction) =>
            onCorrect();

        public void Misclassified(LanguageSample reference, LanguageSample prediction) =>
            onIncorrect();
    }
}
