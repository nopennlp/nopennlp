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

using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Ngram;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Featuregen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static NOpenNLP.Tools.Ml.TrainerFactory;
using JCG = J2N.Collections.Generic;

// disable obsolete warnings
#pragma warning disable CS0618

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// A part-of-speech tagger that uses maximum entropy.  Tries to predict whether
/// words are nouns, verbs, or any of 70 other POS tags depending on their
/// surrounding context.
/// </summary>
public class POSTaggerME : IPOSTagger
{
    // NOpenNLP: made various fields const/readonly
    public const int DEFAULT_BEAM_SIZE = 3;
    private readonly POSModel modelPackage;
    /// <summary>
    /// The feature context generator.
    /// </summary>
    protected readonly IPOSContextGenerator contextGen;
    /// <summary>
    /// Tag dictionary used for restricting words to a fixed set of tags.
    /// </summary>
    protected ITagDictionary tagDictionary;
    protected NOpenNLP.Tools.Dictionary.Dictionary? ngramDictionary;
    /// <summary>
    /// Says whether a filter should be used to check whether a tag assignment
    /// is to a word outside of a closed class.
    /// </summary>
    protected bool useClosedClassTagsFilter = false;
    /// <summary>
    /// The size of the beam to be used in determining the best sequence of pos tags.
    /// </summary>
    protected readonly int size;
    private Sequence? bestSequence;
    private readonly ISequenceClassificationModel<string> model;
    private readonly ISequenceValidator<string> sequenceValidator;

    /// <summary>
    /// Initializes the current instance with the provided model.
    /// </summary>
    /// <param name="model"></param>
    public POSTaggerME(POSModel model)
    {
        POSTaggerFactory factory = model.Factory;
        int beamSize = POSTaggerME.DEFAULT_BEAM_SIZE;
        string? beamSizeString = model.GetManifestProperty(BeamSearch.BEAM_SIZE_PARAMETER);
        if (beamSizeString != null)
        {
            beamSize = int.Parse(beamSizeString);
        }

        modelPackage = model;
        contextGen = factory.GetPOSContextGenerator(beamSize);
        tagDictionary = factory.GetTagDictionary();
        size = beamSize;
        sequenceValidator = factory.GetSequenceValidator();
        if (model.PosSequenceModel is { } posSequenceModel)
        {
            this.model = posSequenceModel;
        }
        else
        {
            Debug.Assert(model.PosModel is not null);
            this.model = new BeamSearch<string>(beamSize, model.PosModel, 0);
        }
    }

    /// <summary>
    /// Retrieves an array of all possible part-of-speech tags from the
    /// tagger.
    /// </summary>
    /// <returns>String[]</returns>
    public virtual string[] AllPosTags => model.Outcomes;

    public virtual string[] Tag(string[] sentence)
    {
        return this.Tag(sentence, null);
    }

    public virtual string[] Tag(string[] sentence, object[]? additionalContext)
    {
        bestSequence = model.BestSequence(sentence, additionalContext, contextGen, sequenceValidator);
        IList<string> t = bestSequence.Outcomes;
        return [.. t];
    }

    /// <summary>
    /// Returns at most the specified number of taggings for the specified sentence.
    /// </summary>
    /// <param name="numTaggings">The number of tagging to be returned.</param>
    /// <param name="sentence">An array of tokens which make up a sentence.</param>
    /// <returns>At most the specified number of taggings for the specified sentence.</returns>
    public virtual string[][] Tag(int numTaggings, string[] sentence)
    {
        Sequence[] bestSequences = model.BestSequences(numTaggings, sentence, null, contextGen, sequenceValidator);
        string[][] tags = new string[bestSequences.Length][];
        for (int si = 0; si < tags.Length; si++)
        {
            IList<string> t = bestSequences[si].Outcomes;
            tags[si] = [.. t];
        }

        return tags;
    }

    public virtual Sequence[] TopKSequences(string[] sentence)
    {
        return this.TopKSequences(sentence, null);
    }

    public virtual Sequence[] TopKSequences(string[] sentence, object[]? additionaContext)
    {
        return model.BestSequences(size, sentence, additionaContext, contextGen, sequenceValidator);
    }

    /// <summary>
    /// Populates the specified array with the probabilities for each tag of the last tagged sentence.
    /// </summary>
    /// <param name="probs">An array to put the probabilities into.</param>
    public virtual void Probs(double[] probs)
    {
        // NOpenNLP: check to ensure bestSequence is not null, to avoid NRE
        if (bestSequence is null)
        {
            throw new InvalidOperationException($"You must call {nameof(Tag)} before calling {nameof(Probs)}");
        }
        bestSequence.GetProbs(probs);
    }

    /// <summary>
    /// Returns an array with the probabilities for each tag of the last tagged sentence.
    /// </summary>
    /// <returns>an array with the probabilities for each tag of the last tagged sentence.</returns>
    public virtual double[] Probs()
    {
        // NOpenNLP: check to ensure bestSequence is not null, to avoid NRE
        if (bestSequence is null)
        {
            throw new InvalidOperationException($"You must call {nameof(Tag)} before calling {nameof(Probs)}");
        }

        return bestSequence.Probs;
    }

    public virtual string[] GetOrderedTags(IList<string> words, IList<string> tags, int index)
    {
        return GetOrderedTags(words, tags, index, null);
    }

    public virtual string[] GetOrderedTags(IList<string> words, IList<string> tags, int index, double[]? tprobs)
    {
        if (modelPackage.PosModel is { } posModel)
        {
            double[] probs = posModel.Eval(contextGen.GetContext(index, [.. words], [.. tags], null));
            string[] orderedTags = new string[probs.Length];
            for (int i = 0; i < probs.Length; i++)
            {
                int max = 0;
                for (int ti = 1; ti < probs.Length; ti++)
                {
                    if (probs[ti] > probs[max])
                    {
                        max = ti;
                    }
                }

                orderedTags[i] = posModel.GetOutcome(max);
                if (tprobs != null)
                {
                    tprobs[i] = probs[max];
                }

                probs[max] = 0;
            }

            return orderedTags;
        }
        else
        {
            throw new NotSupportedException("This method can only be called if the " + "classifcation model is an event model!");
        }
    }

    /// <summary>
    /// Trains a <see cref="POSModel"/>.
    /// </summary>
    /// <param name="languageCode">the language of the training data</param>
    /// <param name="samples">the samples used for the training</param>
    /// <param name="trainParams">the machine learning train parameters</param>
    /// <param name="posFactory">a <see cref="POSTaggerFactory"/> to get resources from</param>
    /// <returns>the trained <see cref="POSModel"/></returns>
    /// <exception cref="IOException">if reading from the <see cref="IObjectStream{T}"/> fails.</exception>
    public static POSModel Train(string languageCode, IObjectStream<POSSample?> samples,
        TrainingParameters trainParams, POSTaggerFactory posFactory)
    {
        int beamSize = trainParams.GetIntParameter(BeamSearch.BEAM_SIZE_PARAMETER,
            POSTaggerME.DEFAULT_BEAM_SIZE);

        IPOSContextGenerator contextGenerator = posFactory.GetPOSContextGenerator();

        IDictionary<string, string> manifestInfoEntries = new JCG.Dictionary<string, string>();

        TrainerType? trainerType = TrainerFactory.GetTrainerType(trainParams);

        IMaxentModel? posModel = null;
        ISequenceClassificationModel<string>? seqPosModel = null;
        if (TrainerType.EVENT_MODEL_TRAINER.Equals(trainerType))
        {
            IObjectStream<Event?> es = new POSSampleEventStream(samples, contextGenerator);

            IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, manifestInfoEntries);
            posModel = trainer.Train(es);
        }
        else if (TrainerType.EVENT_MODEL_SEQUENCE_TRAINER.Equals(trainerType))
        {
            POSSampleSequenceStream ss = new(samples, contextGenerator);
            IEventModelSequenceTrainer<POSSample> trainer =
                TrainerFactory.GetEventModelSequenceTrainer<POSSample>(trainParams, manifestInfoEntries);
            posModel = trainer.Train(ss);
        }
        else if (TrainerType.SEQUENCE_TRAINER.Equals(trainerType))
        {
            ISequenceTrainer<POSSample> trainer =
                TrainerFactory.GetSequenceModelTrainer<POSSample>(trainParams, manifestInfoEntries);

            // TODO: This will probably cause issue, since the feature generator uses the outcomes array

            POSSampleSequenceStream ss = new(samples, contextGenerator);
            seqPosModel = trainer.Train(ss);
        }
        else
        {
            throw new ArgumentException("Trainer type is not supported: " + trainerType);
        }

        if (posModel != null)
        {
            return new POSModel(languageCode, posModel, beamSize, manifestInfoEntries, posFactory);
        }
        else
        {
            return new POSModel(languageCode, seqPosModel!, manifestInfoEntries, posFactory);
        }
    }

    /// <exception cref="IOException">if reading from the <see cref="IObjectStream{T}"/> fails.</exception>
    public static NOpenNLP.Tools.Dictionary.Dictionary BuildNGramDictionary(
        IObjectStream<POSSample?> samples, int cutoff)
    {
        NGramModel ngramModel = new();

        while (samples.Read() is { } sample)
        {
            string[] words = sample.Sentence;

            if (words.Length > 0)
                ngramModel.Add(new StringList(words), 1, 1);
        }

        ngramModel.Cutoff(cutoff, int.MaxValue);

        return ngramModel.ToDictionary(true);
    }

    /// <exception cref="IOException">if reading from the <see cref="IObjectStream{T}"/> fails.</exception>
    public static void PopulatePOSDictionary(IObjectStream<POSSample?> samples,
        IMutableTagDictionary dict, int cutoff)
    {
        Console.Out.WriteLine("Expanding POS Dictionary ...");
        long start = Stopwatch.GetTimestamp();

        // the data structure will store the word, the tag, and the number of
        // occurrences
        // NOpenNLP: upstream uses AtomicInteger purely as a mutable box, not for
        // thread safety; MutableInt is the port's equivalent and is what the other
        // ported counters use.
        IDictionary<string, IDictionary<string, MutableInt>> newEntries =
            new JCG.Dictionary<string, IDictionary<string, MutableInt>>();

        while (samples.Read() is { } sample)
        {
            string[] words = sample.Sentence;
            string[] tags = sample.Tags;

            for (int i = 0; i < words.Length; i++)
            {
                // only store words
                if (!StringPattern.Recognize(words[i]).ContainsDigit)
                {
                    string word;
                    if (dict.IsCaseSensitive)
                    {
                        word = words[i];
                    }
                    else
                    {
                        word = StringUtil.ToLowerCase(words[i]);
                    }

                    if (!newEntries.TryGetValue(word, out IDictionary<string, MutableInt>? value))
                    {
                        value = new JCG.Dictionary<string, MutableInt>();
                        newEntries[word] = value;
                    }

                    string[]? dictTags = dict.GetTags(word);
                    if (dictTags != null)
                    {
                        foreach (string tag in dictTags)
                        {
                            // for this tags we start with the cutoff
                            if (!value.ContainsKey(tag))
                            {
                                value[tag] = new MutableInt(cutoff);
                            }
                        }
                    }

                    if (!value.TryGetValue(tags[i], out MutableInt? count))
                    {
                        value[tags[i]] = new MutableInt(1);
                    }
                    else
                    {
                        count.Increment();
                    }
                }
            }
        }

        // now we check if the word + tag pairs have enough occurrences, if yes we
        // add it to the dictionary
        foreach (KeyValuePair<string, IDictionary<string, MutableInt>> wordEntry in newEntries)
        {
            JCG.List<string> tagsForWord = [];
            foreach (KeyValuePair<string, MutableInt> entry in wordEntry.Value)
            {
                if (entry.Value.Value >= cutoff)
                {
                    tagsForWord.Add(entry.Key);
                }
            }

            if (tagsForWord.Count > 0)
            {
                dict.Put(wordEntry.Key, [.. tagsForWord]);
            }
        }

        // NOpenNLP: upstream reports the elapsed time from System.nanoTime(); Stopwatch
        // is the .NET equivalent clock, and its ticks are converted to milliseconds here.
        long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;
        Console.Out.WriteLine("... finished expanding POS Dictionary. [" + elapsedMs + "ms]");
    }
}
