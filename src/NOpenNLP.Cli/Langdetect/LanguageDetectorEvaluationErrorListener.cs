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
using System.IO;
using NOpenNLP.Tools.Langdetect;
using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Cmdline.Langdetect;

/// <summary>
/// A default implementation of <see cref="IEvaluationMonitor{T}"/> that prints to an
/// output stream.
/// </summary>
public class LanguageDetectorEvaluationErrorListener : EvaluationErrorPrinter<LanguageSample>,
    ILanguageDetectorEvaluationMonitor
{
    /// <summary>
    /// Creates a listener that will print to <see cref="Console.Error"/>.
    /// </summary>
    // NOpenNLP: upstream's no-argument constructor calls super(System.err) directly,
    // so -- unlike the TextWriter one -- it does not print the column header. That is
    // reproduced here rather than chaining the two constructors.
    public LanguageDetectorEvaluationErrorListener()
        : base(Console.Error)
    {
    }

    /// <summary>
    /// Creates a listener that will print to a given <see cref="TextWriter"/>.
    /// </summary>
    public LanguageDetectorEvaluationErrorListener(TextWriter outputStream)
        : base(outputStream)
    {
        printStream.WriteLine("Expected\tPredicted\tContext");
    }

    /// <summary>
    /// Creates a listener that will print to a given <see cref="Stream"/>.
    /// </summary>
    public LanguageDetectorEvaluationErrorListener(Stream outputStream)
        : this(new StreamWriter(outputStream) { AutoFlush = true })
    {
    }

    /// <inheritdoc/>
    public override void Misclassified(LanguageSample reference, LanguageSample prediction) =>
        PrintError(reference, prediction);

    /// <inheritdoc/>
    protected override void PrintError(LanguageSample referenceSample,
        LanguageSample predictedSample) =>
        printStream.WriteLine(string.Join("\t", referenceSample.Language.Lang,
            predictedSample.Language.Lang,
            referenceSample.Context));
}
