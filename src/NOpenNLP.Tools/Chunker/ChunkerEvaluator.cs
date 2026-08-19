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

using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// The <see cref="ChunkerEvaluator"/> measures the performance
/// of the given <see cref="IChunker"/> with the provided
/// reference <see cref="ChunkSample"/>s.
/// </summary>
/// <seealso cref="Evaluator{T}"/>
/// <seealso cref="IChunker"/>
/// <seealso cref="ChunkSample"/>
public class ChunkerEvaluator : Evaluator<ChunkSample>
{
    private readonly FMeasure fmeasure = new(); // NOpenNLP: made readonly

    /// <summary>
    /// The <see cref="IChunker"/> used to create the predicted
    /// <see cref="ChunkSample"/> objects.
    /// </summary>
    private readonly IChunker chunker; // NOpenNLP: made readonly

    /// <summary>
    /// Initializes the current instance with the given
    /// <see cref="IChunker"/>.
    /// </summary>
    /// <param name="chunker">the <see cref="IChunker"/> to evaluate.</param>
    /// <param name="listeners">evaluation listeners</param>
    public ChunkerEvaluator(IChunker chunker, params IChunkerEvaluationMonitor?[]? listeners)
        : base(listeners)
        => this.chunker = chunker;

    /// <summary>
    /// Evaluates the given reference <see cref="ChunkSample"/> object.
    /// <para/>
    /// This is done by finding the phrases with the
    /// <see cref="IChunker"/> in the sentence from the reference
    /// <see cref="ChunkSample"/>. The found phrases are then used to
    /// calculate and update the scores.
    /// </summary>
    /// <param name="reference">the reference <see cref="ChunkSample"/>.</param>
    /// <returns>the predicted sample</returns>
    protected override ChunkSample ProcessSample(ChunkSample reference)
    {
        string[] preds = chunker.Chunk(reference.Sentence, reference.Tags);
        ChunkSample result = new(reference.Sentence, reference.Tags, preds);

        fmeasure.UpdateScores(reference.PhrasesAsSpanList, result.PhrasesAsSpanList);

        return result;
    }

    public virtual FMeasure FMeasure => fmeasure;
}
