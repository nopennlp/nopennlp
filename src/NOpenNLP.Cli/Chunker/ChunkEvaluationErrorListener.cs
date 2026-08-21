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
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Cmdline.Chunker;

/// <summary>
/// A default implementation of <see cref="IEvaluationMonitor{T}"/> that prints
/// to an output stream.
/// </summary>
public class ChunkEvaluationErrorListener : EvaluationErrorPrinter<ChunkSample>,
    IChunkerEvaluationMonitor
{
    /// <summary>
    /// Creates a listener that will print to <see cref="Console.Error"/>.
    /// </summary>
    public ChunkEvaluationErrorListener()
        : base(Console.Error)
    {
    }

    /// <summary>
    /// Creates a listener that will print to a given <see cref="TextWriter"/>.
    /// </summary>
    public ChunkEvaluationErrorListener(TextWriter outputStream)
        : base(outputStream)
    {
    }

    /// <summary>
    /// Creates a listener that will print to a given <see cref="Stream"/>.
    /// </summary>
    public ChunkEvaluationErrorListener(Stream outputStream)
        : this(new StreamWriter(outputStream) { AutoFlush = true })
    {
    }

    /// <inheritdoc/>
    public override void Misclassified(ChunkSample reference, ChunkSample prediction) =>
        PrintError(reference.PhrasesAsSpanList,
            prediction.PhrasesAsSpanList, reference, prediction,
            reference.Sentence);
}
