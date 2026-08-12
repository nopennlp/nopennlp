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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using System;
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// The class represents a maximum-entropy-based chunker.  Such a chunker can be used to
/// find flat structures based on sequence inputs such as noun phrases or named entities.
/// </summary>
public class ChunkerME : IChunker
{
    public const int DEFAULT_BEAM_SIZE = 10;

    private Sequence? bestSequence;

    /// <summary>
    /// The model used to assign chunk tags to a sequence of tokens.
    /// </summary>
    protected readonly ISequenceClassificationModel<TokenTag> model; // NOpenNLP: made readonly
    private readonly IChunkerContextGenerator contextGenerator; // NOpenNLP: made readonly
    private readonly ISequenceValidator<TokenTag> sequenceValidator; // NOpenNLP: made readonly

    /// <summary>
    /// Initializes the current instance with the specified model and
    /// the specified beam size.
    /// </summary>
    /// <param name="model">The model for this chunker.</param>
    /// <param name="beamSize">The size of the beam that should be used when decoding sequences.</param>
    /// <param name="sequenceValidator">The <see cref="ISequenceValidator{T}"/> to determines whether the outcome
    ///        is valid for the preceding sequence. This can be used to implement constraints
    ///        on what sequences are valid.</param>
    /// <remarks>
    /// Deprecated: Use <c>ChunkerME(ChunkerModel, int)</c> instead and use the <see cref="ChunkerFactory"/>
    ///     to configure the <see cref="ISequenceValidator{T}"/> and <see cref="IChunkerContextGenerator"/>.
    /// </remarks>
    private ChunkerME(ChunkerModel model, int beamSize, ISequenceValidator<TokenTag> sequenceValidator, IChunkerContextGenerator contextGenerator)
    {
        this.sequenceValidator = sequenceValidator;
        this.contextGenerator = contextGenerator;
        if (model.ChunkerSequenceModel is { } chunkerSequenceModel)
        {
            this.model = chunkerSequenceModel;
        }
        else
        {
            this.model = new BeamSearch<TokenTag>(beamSize, model.ChunkerModelValue, 0);
        }
    }

    /// <summary>
    /// Initializes the current instance with the specified model and
    /// the specified beam size.
    /// </summary>
    /// <param name="model">The model for this chunker.</param>
    /// <param name="beamSize">The size of the beam that should be used when decoding sequences.</param>
    /// <remarks>Deprecated: Beam size is now stored inside the model</remarks>
    private ChunkerME(ChunkerModel model, int beamSize)
    {
        contextGenerator = model.Factory.ContextGenerator;
        sequenceValidator = model.Factory.SequenceValidator;
        if (model.ChunkerSequenceModel is { } chunkerSequenceModel)
        {
            this.model = chunkerSequenceModel;
        }
        else
        {
            this.model = new BeamSearch<TokenTag>(beamSize, model.ChunkerModelValue, 0);
        }
    }

    /// <summary>
    /// Initializes the current instance with the specified model.
    /// The default beam size is used.
    /// </summary>
    /// <param name="model"></param>
    public ChunkerME(ChunkerModel model) : this(model, DEFAULT_BEAM_SIZE)
    {
    }

    public virtual string[] Chunk(string[] toks, string[] tags)
    {
        TokenTag[] tuples = TokenTag.Create(toks, tags);
        bestSequence = model.BestSequence(tuples, [], contextGenerator, sequenceValidator);
        IList<string> c = bestSequence.Outcomes;
        return [.. c];
    }

    public virtual Span[] ChunkAsSpans(string[] toks, string[] tags)
    {
        string[] preds = Chunk(toks, tags);
        return ChunkSample.GetPhrasesAsSpanList(toks, tags, preds);
    }

    public virtual Sequence[] TopKSequences(string[] sentence, string[] tags)
    {
        TokenTag[] tuples = TokenTag.Create(sentence, tags);
        return model.BestSequences(DEFAULT_BEAM_SIZE, tuples, [], contextGenerator, sequenceValidator);
    }

    public virtual Sequence[] TopKSequences(string[] sentence, string[] tags, double minSequenceScore)
    {
        TokenTag[] tuples = TokenTag.Create(sentence, tags);
        return model.BestSequences(DEFAULT_BEAM_SIZE, tuples, [], minSequenceScore, contextGenerator, sequenceValidator);
    }

    /// <summary>
    /// Populates the specified array with the probabilities of the last decoded sequence.  The
    /// sequence was determined based on the previous call to <see cref="Chunk"/>.  The
    /// specified array should be at least as large as the numbe of tokens in the previous
    /// call to <see cref="Chunk"/>.
    /// </summary>
    /// <param name="probs">An array used to hold the probabilities of the last decoded sequence.</param>
    public virtual void Probs(double[] probs)
    {
        // NOpenNLP: check to ensure bestSequence is not null, to avoid NRE
        if (bestSequence is null)
        {
            throw new InvalidOperationException($"You must call {nameof(Chunk)} before calling {nameof(Probs)}");
        }

        bestSequence.GetProbs(probs);
    }

    /// <summary>
    /// Returns an array with the probabilities of the last decoded sequence.  The
    /// sequence was determined based on the previous call to <see cref="Chunk"/>.
    /// </summary>
    /// <returns>An array with the same number of probabilities as tokens were sent to <see cref="Chunk"/>
    ///     when it was last called.</returns>
    public virtual double[] Probs()
    {
        // NOpenNLP: check to ensure bestSequence is not null, to avoid NRE
        if (bestSequence is null)
        {
            throw new InvalidOperationException($"You must call {nameof(Chunk)} before calling {nameof(Probs)}");
        }

        return bestSequence.Probs;
    }

    // public static ChunkerModel Train(string lang, ObjectStream<ChunkSample> @in, TrainingParameters mlParams, ChunkerFactory factory)
    // {
    //     int beamSize = mlParams.GetIntParameter(BeamSearch.BEAM_SIZE_PARAMETER, ChunkerME.DEFAULT_BEAM_SIZE);
    //     Dictionary<string, string> manifestInfoEntries = new Dictionary<string, string>();
    //     TrainerType trainerType = TrainerFactory.GetTrainerType(mlParams);
    //     IMaxentModel chunkerModel = null;
    //     ISequenceClassificationModel<string> seqChunkerModel = null;
    //     if (TrainerType.EVENT_MODEL_TRAINER.Equals(trainerType))
    //     {
    //         ObjectStream<Event> es = new ChunkerEventStream(@in, factory.GetContextGenerator());
    //         EventTrainer trainer = TrainerFactory.GetEventTrainer(mlParams, manifestInfoEntries);
    //         chunkerModel = trainer.Train(es);
    //     }
    //     else if (TrainerType.SEQUENCE_TRAINER.Equals(trainerType))
    //     {
    //         SequenceTrainer trainer = TrainerFactory.GetSequenceModelTrainer(mlParams, manifestInfoEntries);
    //
    //         // TODO: This will probably cause issue, since the feature generator uses the outcomes array
    //         ChunkSampleSequenceStream ss = new ChunkSampleSequenceStream(@in, factory.GetContextGenerator());
    //         seqChunkerModel = trainer.Train(ss);
    //     }
    //     else
    //     {
    //         throw new ArgumentException("Trainer type is not supported: " + trainerType);
    //     }
    //
    //     if (chunkerModel != null)
    //     {
    //         return new ChunkerModel(lang, chunkerModel, beamSize, manifestInfoEntries, factory);
    //     }
    //     else
    //     {
    //         return new ChunkerModel(lang, seqChunkerModel, manifestInfoEntries, factory);
    //     }
    // }
}
