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
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Langdetect;

public class LanguageDetectorEvaluatorTest
{
    [Test]
    public void ProcessSample()
    {
        LanguageDetectorModel model = LanguageDetectorMETest.TrainModel();
        LanguageDetectorME langdetector = new(model);

        int correctCount = 0;
        int incorrectCount = 0;

        StringBuilder outputStream = new();

        LanguageDetectorEvaluator evaluator = new(langdetector,
            new CountingMonitor(() => correctCount++, () => incorrectCount++),
            new LanguageDetectorEvaluationErrorListener(outputStream));

        evaluator.EvaluateSample(new LanguageSample(new Language("pob"),
            "escreve e faz palestras pelo mundo inteiro sobre anjos"));

        evaluator.EvaluateSample(new LanguageSample(new Language("fra"),
            "escreve e faz palestras pelo mundo inteiro sobre anjos"));

        evaluator.EvaluateSample(new LanguageSample(new Language("fra"),
            "escreve e faz palestras pelo mundo inteiro sobre anjos"));

        ClassicAssert.AreEqual(1, correctCount);
        ClassicAssert.AreEqual(2, incorrectCount);

        ClassicAssert.AreEqual(3, evaluator.DocumentCount);
        ClassicAssert.AreEqual(0.33, evaluator.Accuracy, 0.01);

        string report = outputStream.ToString();

        ClassicAssert.AreEqual("Expected\tPredicted\tContext" + Environment.NewLine +
            "fra\tpob\tescreve e faz palestras pelo mundo inteiro sobre anjos" + Environment.NewLine +
            "fra\tpob\tescreve e faz palestras pelo mundo inteiro sobre anjos" + Environment.NewLine, report);
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

    /// <summary>
    /// NOpenNLP: upstream uses
    /// <c>opennlp.tools.cmdline.langdetect.LanguageDetectorEvaluationErrorListener</c>,
    /// which writes a tab-separated report to an <c>OutputStream</c>. The <c>cmdline</c>
    /// package is not ported, so this test-local stand-in reproduces the same
    /// format, which the assertion above checks verbatim.
    /// </summary>
    private sealed class LanguageDetectorEvaluationErrorListener
        : ILanguageDetectorEvaluationMonitor
    {
        private readonly StringBuilder output;

        public LanguageDetectorEvaluationErrorListener(StringBuilder output)
        {
            this.output = output;
            output.Append("Expected\tPredicted\tContext").Append(Environment.NewLine);
        }

        public void CorrectlyClassified(LanguageSample reference, LanguageSample prediction)
        {
        }

        public void Misclassified(LanguageSample reference, LanguageSample prediction) =>
            output.Append(string.Join("\t", reference.Language.Lang,
                prediction.Language.Lang, reference.Context)).Append(Environment.NewLine);
    }
}
