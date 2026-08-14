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

using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Util.Featuregen;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// Class for creating a maximum-entropy-based name finder.
/// </summary>
public class NameFinderME : ITokenNameFinder
{
    // NOpenNLP: made various fields readonly/const
    private static readonly string[][] EMPTY = [];

    public const int DEFAULT_BEAM_SIZE = 3;

    private static readonly Regex typedOutcomePattern = new("(.+)-\\w+", RegexOptions.Compiled);

    public const string START = "start";
    public const string CONTINUE = "cont";
    public const string OTHER = "other";

    private readonly ISequenceCodec<string> seqCodec = new BioCodec();
    protected readonly ISequenceClassificationModel<string>? model;
    protected readonly INameContextGenerator contextGenerator;
    private Sequence? bestSequence;
    private readonly AdditionalContextFeatureGenerator additionalContextFeatureGenerator = new();
    private readonly ISequenceValidator<string> sequenceValidator;

    public NameFinderME(TokenNameFinderModel model)
    {
        TokenNameFinderFactory factory = model.Factory;
        seqCodec = factory.CreateSequenceCodec();
        sequenceValidator = seqCodec.CreateSequenceValidator();
        this.model = model.NameFinderSequenceModel;
        contextGenerator = factory.CreateContextGenerator();

        // TODO: We should deprecate this. And come up with a better solution!
        contextGenerator.AddFeatureGenerator(new WindowFeatureGenerator(additionalContextFeatureGenerator, 8, 8));
    }

    private static IAdaptiveFeatureGenerator? CreateFeatureGenerator(byte[]? generatorDescriptor, Dictionary<string, object>? resources)
    {
        IAdaptiveFeatureGenerator? featureGenerator;
        if (generatorDescriptor != null)
        {
            featureGenerator = GeneratorFactory.Create(new System.IO.MemoryStream(generatorDescriptor), key => resources?[key]);
        }
        else
        {
            featureGenerator = null;
        }

        return featureGenerator;
    }

    public virtual Span[] Find(string[] tokens) => Find(tokens, EMPTY);

    /// <summary>
    /// Generates name tags for the given sequence, typically a sentence, returning
    /// token spans for any identified names.
    /// </summary>
    /// <param name="tokens">an array of the tokens or words of the sequence, typically a sentence.</param>
    /// <param name="additionalContext">features which are based on context outside of the
    ///     sentence but which should also be used.</param>
    /// <returns>an array of spans for each of the names identified.</returns>
    public virtual Span[] Find(string[] tokens, string[][] additionalContext)
    {
        additionalContextFeatureGenerator.SetCurrentContext(additionalContext);
        bestSequence = model.BestSequence(tokens, additionalContext, contextGenerator, sequenceValidator);
        IList<string> c = bestSequence.Outcomes;
        contextGenerator.UpdateAdaptiveData(tokens, [.. c]);
        Span[] spans = seqCodec.Decode(c);
        spans = SetProbs(spans);
        return spans;
    }

    /// <summary>
    /// Forgets all adaptive data which was collected during previous calls to one
    /// of the find methods.
    ///
    /// This method is typical called at the end of a document.
    /// </summary>
    public virtual void ClearAdaptiveData()
    {
        contextGenerator.ClearAdaptiveData();
    }

    /// <summary>
    /// Populates the specified array with the probabilities of the last decoded
    /// sequence. The sequence was determined based on the previous call to
    /// <see cref="Find(string[], string[][])"/>. The specified array should be at least as large as the
    /// number of tokens in the previous call to <see cref="Find(string[], string[][])"/>.
    /// </summary>
    /// <param name="probs">An array used to hold the probabilities of the last decoded
    ///     sequence.</param>
    public virtual void Probs(double[] probs)
    {
        bestSequence.GetProbs(probs);
    }

    /// <summary>
    /// Returns an array with the probabilities of the last decoded sequence. The
    /// sequence was determined based on the previous call to <see cref="Find(string[], string[][])"/>.
    /// </summary>
    /// <returns>An array with the same number of probabilities as tokens were sent
    ///     to <see cref="Find(string[], string[][])"/> when it was last called.</returns>
    public virtual double[] Probs() => bestSequence.Probs;

    /// <summary>
    /// sets the probs for the spans
    /// </summary>
    /// <param name="spans"></param>
    /// <returns></returns>
    private Span[] SetProbs(Span[] spans)
    {
        double[] probs = Probs(spans);
        if (probs != null)
        {
            for (int i = 0; i < probs.Length; i++)
            {
                double prob = probs[i];
                spans[i] = new Span(spans[i], prob);
            }
        }

        return spans;
    }

    /// <summary>
    /// Returns an array of probabilities for each of the specified spans which is
    /// the arithmetic mean of the probabilities for each of the outcomes which
    /// make up the span.
    /// </summary>
    /// <param name="spans">The spans of the names for which probabilities are desired.</param>
    /// <returns>an array of probabilities for each of the specified spans.</returns>
    public virtual double[] Probs(Span[] spans)
    {
        double[] sprobs = new double[spans.Length];
        double[] probs = bestSequence.Probs;
        for (int si = 0; si < spans.Length; si++)
        {
            double p = 0;
            for (int oi = spans[si].Start; oi < spans[si].End; oi++)
            {
                p += probs[oi];
            }

            p /= spans[si].Length;
            sprobs[si] = p;
        }

        return sprobs;
    }

    // public static TokenNameFinderModel Train(string languageCode, string type, ObjectStream<NameSample> samples, TrainingParameters trainParams, TokenNameFinderFactory factory)
    // {
    //     trainParams.PutIfAbsent(TrainingParameters.ALGORITHM_PARAM, PerceptronTrainer.PERCEPTRON_VALUE);
    //     trainParams.PutIfAbsent(TrainingParameters.CUTOFF_PARAM, 0);
    //     trainParams.PutIfAbsent(TrainingParameters.ITERATIONS_PARAM, 300);
    //     int beamSize = trainParams.GetIntParameter(BeamSearch.BEAM_SIZE_PARAMETER, NameFinderME.DEFAULT_BEAM_SIZE);
    //     Dictionary<string, string> manifestInfoEntries = new Dictionary<string, string>();
    //     IMaxentModel nameFinderModel = null;
    //     ISequenceClassificationModel<string> seqModel = null;
    //     TrainerType trainerType = TrainerFactory.GetTrainerType(trainParams);
    //     if (TrainerType.EVENT_MODEL_TRAINER.Equals(trainerType))
    //     {
    //         ObjectStream<Event> eventStream = new NameFinderEventStream(samples, type, factory.CreateContextGenerator(), factory.CreateSequenceCodec());
    //         EventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, manifestInfoEntries);
    //         nameFinderModel = trainer.Train(eventStream);
    //     } // TODO: Maybe it is not a good idea, that these two don't use the context generator ?!
    //     else
// // These also don't use the sequence codec ?!
    //     if (TrainerType.EVENT_MODEL_SEQUENCE_TRAINER.Equals(trainerType))
    //     {
    //         NameSampleSequenceStream ss = new NameSampleSequenceStream(samples, factory.CreateContextGenerator());
    //         EventModelSequenceTrainer trainer = TrainerFactory.GetEventModelSequenceTrainer(trainParams, manifestInfoEntries);
    //         nameFinderModel = trainer.Train(ss);
    //     }
    //     else if (TrainerType.SEQUENCE_TRAINER.Equals(trainerType))
    //     {
    //         SequenceTrainer trainer = TrainerFactory.GetSequenceModelTrainer(trainParams, manifestInfoEntries);
    //         NameSampleSequenceStream ss = new NameSampleSequenceStream(samples, factory.CreateContextGenerator(), false);
    //         seqModel = trainer.Train(ss);
    //     }
    //     else
    //     {
    //         throw new InvalidOperationException("Unexpected trainer type!");
    //     }
//
    //     if (seqModel != null)
    //     {
    //         return new TokenNameFinderModel(languageCode, seqModel, factory.GetFeatureGenerator(), factory.GetResources(), manifestInfoEntries, factory.GetSequenceCodec(), factory);
    //     }
    //     else
    //     {
    //         return new TokenNameFinderModel(languageCode, nameFinderModel, beamSize, factory.GetFeatureGenerator(), factory.GetResources(), manifestInfoEntries, factory.GetSequenceCodec(), factory);
    //     }
    // }

    /// <summary>
    /// Gets the name type from the outcome
    /// </summary>
    /// <param name="outcome">the outcome</param>
    /// <returns>the name type, or null if not set</returns>
    internal static string? ExtractNameType(string outcome)
    {
        var matcher = typedOutcomePattern.Match(outcome);
        if (matcher.Success)
        {
            return matcher.Groups[1].Value;
        }

        return null;
    }

    /// <summary>
    /// Removes spans with are intersecting or crossing in anyway.
    ///
    /// <para/>
    /// The following rules are used to remove the spans:<br/>
    /// Identical spans: The first span in the array after sorting it remains<br/>
    /// Intersecting spans: The first span after sorting remains<br/>
    /// Contained spans: All spans which are contained by another are removed<br/>
    /// </summary>
    /// <param name="spans"></param>
    /// <returns>non-overlapping spans</returns>
    public static Span[] DropOverlappingSpans(Span[] spans)
    {
        IList<Span> sortedSpans = new List<Span>(spans.Length);
        sortedSpans.AddRange(spans);
        sortedSpans.Sort();
        // NOpenNLP: upstream uses Iterator.remove() to delete from the backing
        // list mid-traversal, which .NET enumerators do not support. An index-based
        // loop that steps back on removal preserves the same semantics.
        Span? lastSpan = null;
        for (int i = 0; i < sortedSpans.Count; i++)
        {
            Span span = sortedSpans[i];
            if (lastSpan != null)
            {
                if (lastSpan.Intersects(span))
                {
                    sortedSpans.RemoveAt(i);
                    i--;
                    span = lastSpan;
                }
            }

            lastSpan = span;
        }

        return [.. sortedSpans];
    }
}
