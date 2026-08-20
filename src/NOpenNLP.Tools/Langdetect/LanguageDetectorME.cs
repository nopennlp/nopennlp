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
using System.Linq;
using J2N;
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Langdetect;

/// <summary>
/// Implements learnable Language Detector.
/// <para/>
/// This will process the entire string when called with
/// <see cref="PredictLanguage(string)"/> or
/// <see cref="PredictLanguages(string)"/>.
/// <para/>
/// If you want this to stop early, use <see cref="ProbingPredictLanguages(string)"/>
/// or <see cref="ProbingPredictLanguages(string, LanguageDetectorConfig)"/>.
/// When run in probing mode, this starts at the beginning of the charsequence
/// and runs language detection on chunks of text. If the end of the
/// string is reached or there are <see cref="LanguageDetectorConfig.MinConsecImprovements"/>
/// consecutive predictions for the best language and the confidence
/// increases over those last predictions and if the difference
/// in confidence between the highest confidence language
/// and the second highest confidence language is greater than
/// <see cref="LanguageDetectorConfig.MinDiff"/>, the language detector will
/// stop and report the results.
/// <para/>
/// The authors wish to thank Ken Krugler and
/// <a href="https://github.com/kkrugler/yalder">Yalder</a>
/// for the inspiration for many of the design components of this detector.
/// </summary>
public class LanguageDetectorME : ILanguageDetector
{
    protected readonly LanguageDetectorModel model; // NOpenNLP: made readonly

    // NOpenNLP: made readonly
    private readonly ILanguageDetectorContextGenerator mContextGenerator;

    /// <summary>
    /// Initializes the current instance with a language detector model. Default feature
    /// generation is used.
    /// </summary>
    /// <param name="model">the language detector model</param>
    public LanguageDetectorME(LanguageDetectorModel model)
    {
        this.model = model;
        this.mContextGenerator = model.Factory.GetContextGenerator();
    }

    /// <summary>
    /// This will process the full content length.
    /// </summary>
    /// <param name="content">content to be processed</param>
    /// <returns>the predicted languages</returns>
    public virtual Language[] PredictLanguages(string content) =>
        Predict(ArrayToCounts(mContextGenerator.GetContext(content)));

    /// <summary>
    /// This will process the full content length.
    /// </summary>
    /// <param name="content">content to be processed</param>
    /// <returns>the language with the highest confidence</returns>
    public virtual Language PredictLanguage(string content) => PredictLanguages(content)[0];

    public virtual string[] SupportedLanguages
    {
        get
        {
            int numberLanguages = model.MaxentModel.NumOutcomes;
            string[] languages = new string[numberLanguages];
            for (int i = 0; i < numberLanguages; i++)
            {
                languages[i] = model.MaxentModel.GetOutcome(i);
            }

            return languages;
        }
    }

    /// <summary>
    /// This will stop processing early if the stopping criteria
    /// specified in <see cref="LanguageDetectorConfig.DEFAULT_LANGUAGE_DETECTOR_CONFIG"/>
    /// are met.
    /// </summary>
    /// <param name="content">content to be processed</param>
    /// <returns>result</returns>
    public virtual ProbingLanguageDetectionResult ProbingPredictLanguages(string content) =>
        ProbingPredictLanguages(content, LanguageDetectorConfig.DEFAULT_LANGUAGE_DETECTOR_CONFIG);

    /// <summary>
    /// This will stop processing early if the stopping criteria
    /// specified in <paramref name="config"/> are met.
    /// </summary>
    /// <param name="content">content to process</param>
    /// <param name="config">config to customize detection</param>
    /// <returns>result</returns>
    public virtual ProbingLanguageDetectionResult ProbingPredictLanguages(string content,
        LanguageDetectorConfig config)
    {
        // list of the languages that received the highest
        // confidence over the last n chunk detections
        List<Language[]> predictions = [];
        int start = 0; // where to start the next chunk in codepoints
        Language[]? currPredictions = null;
        // cache ngram counts across chunks
        IDictionary<string, MutableInt> ngramCounts = new JCG.Dictionary<string, MutableInt>();
        while (true)
        {
            int actualChunkSize = (start + config.ChunkSize > config.MaxLength)
                ? config.MaxLength - start
                : config.ChunkSize;
            var chunk = Chunk(content, start, actualChunkSize);

            if (chunk.Length == 0)
            {
                if (currPredictions == null)
                {
                    return new ProbingLanguageDetectionResult(Predict(ngramCounts), start);
                }
                else
                {
                    return new ProbingLanguageDetectionResult(currPredictions, start);
                }
            }

            start += chunk.Length;
            UpdateCounts(mContextGenerator.GetContext(chunk.String), ngramCounts);
            currPredictions = Predict(ngramCounts);
            if (SeenEnough(predictions, currPredictions, ngramCounts, config))
            {
                return new ProbingLanguageDetectionResult(currPredictions, start);
            }
        }
    }

    private static void UpdateCounts(string[] context, IDictionary<string, MutableInt> ngrams)
    {
        foreach (string ngram in context)
        {
            if (!ngrams.TryGetValue(ngram, out var i))
            {
                i = new MutableInt(1);
                ngrams[ngram] = i;
            }
            else
            {
                i.Increment();
            }
        }
    }

    private static IDictionary<string, MutableInt> ArrayToCounts(string[] context)
    {
        IDictionary<string, MutableInt> ngrams = new JCG.Dictionary<string, MutableInt>();
        UpdateCounts(context, ngrams);
        return ngrams;
    }

    private Language[] Predict(IDictionary<string, MutableInt> ngramCounts)
    {
        string[] allGrams = new string[ngramCounts.Count];
        float[] counts = new float[ngramCounts.Count];
        int i = 0;
        foreach (var e in ngramCounts)
        {
            allGrams[i] = e.Key;
            // TODO -- once OPENNLP-1261 is fixed,
            // change this to e.Value.Value.
            counts[i] = 1;
            i++;
        }

        double[] eval = model.MaxentModel.Eval(allGrams, counts);
        var arr = new Language[eval.Length];
        for (int j = 0; j < eval.Length; j++)
        {
            arr[j] = new Language(model.MaxentModel.GetOutcome(j), eval[j]);
        }

        // NOpenNLP: Java's Arrays.sort on an object array is a stable merge sort,
        // so equally-confident languages keep their outcome order. Array.Sort is
        // an unstable introsort and would reorder ties, changing which language
        // is reported first; OrderByDescending is stable and preserves it.
        return [.. arr.OrderByDescending(o => o.Confidence)];
    }

    /// <summary>
    /// Override this for different behavior to determine if there is enough
    /// confidence in the predictions to stop.
    /// </summary>
    /// <param name="predictionsQueue">queue of earlier predictions</param>
    /// <param name="newPredictions">most recent predictions</param>
    /// <param name="ngramCounts">not currently used, but might be useful</param>
    /// <param name="config">config to customize detection</param>
    /// <returns>whether or not enough text has been processed to make a determination</returns>
    internal virtual bool SeenEnough(IList<Language[]> predictionsQueue, Language[] newPredictions,
        IDictionary<string, MutableInt> ngramCounts, LanguageDetectorConfig config)
    {
        if (predictionsQueue.Count < config.MinConsecImprovements)
        {
            predictionsQueue.Add(newPredictions);
            return false;
        }
        else if (predictionsQueue.Count > config.MinConsecImprovements
            && predictionsQueue.Count > 0)
        {
            predictionsQueue.RemoveAt(0);
        }

        predictionsQueue.Add(newPredictions);
        if (config.MinDiff > 0.0 &&
            newPredictions[0].Confidence - newPredictions[1].Confidence < config.MinDiff)
        {
            return false;
        }

        string? lastLang = null;
        double lastConf = -1.0;
        // iterate through the last predictions
        // and check that the lang with the highest confidence
        // hasn't changed, and that the confidence in it
        // hasn't decreased
        foreach (var predictions in predictionsQueue)
        {
            if (lastLang == null)
            {
                lastLang = predictions[0].Lang;
                lastConf = predictions[0].Confidence;
                continue;
            }
            else
            {
                if (!lastLang.Equals(predictions[0].Lang, StringComparison.Ordinal))
                {
                    return false;
                }

                if (lastConf > predictions[0].Confidence)
                {
                    return false;
                }
            }

            lastLang = predictions[0].Lang;
            lastConf = predictions[0].Confidence;
        }

        return true;
    }

    // NOpenNLP: Java's CharSequence.codePoints().skip(start).limit(chunkSize)
    // walks code points, not UTF-16 chars, so the chunk boundaries must be
    // computed with J2N's code point helpers to avoid splitting surrogate pairs.
    private static StringCPLengthPair Chunk(string content, int start, int chunkSize)
    {
        if (start == 0 && chunkSize > content.Length)
        {
            int codePointLength = content.CodePointCount(0, content.Length);
            return new StringCPLengthPair(content, codePointLength);
        }

        int totalCodePoints = content.CodePointCount(0, content.Length);
        if (start >= totalCodePoints || chunkSize <= 0)
        {
            return new StringCPLengthPair(string.Empty, 0);
        }

        int startIndex = content.OffsetByCodePoints(0, start);
        int available = totalCodePoints - start;
        int take = Math.Min(chunkSize, available);
        int endIndex = content.OffsetByCodePoints(startIndex, take);

        return new StringCPLengthPair(content.Substring(startIndex, endIndex - startIndex), take);
    }

    public static LanguageDetectorModel Train(IObjectStream<LanguageSample?> samples,
        TrainingParameters mlParams, LanguageDetectorFactory factory)
    {
        var manifestInfoEntries = new JCG.Dictionary<string, string>();

        mlParams.PutIfAbsent(AbstractEventTrainer.DATA_INDEXER_PARAM,
            AbstractEventTrainer.DATA_INDEXER_ONE_PASS_VALUE);

        var trainer = TrainerFactory.GetEventTrainer(mlParams, manifestInfoEntries);

        var model = trainer.Train(
            new LanguageDetectorEventStream(samples, factory.GetContextGenerator()));

        return new LanguageDetectorModel(model, manifestInfoEntries, factory);
    }

    private sealed class StringCPLengthPair(string s, int length)
    {
        public int Length { get; } = length;

        public string String { get; } = s;
    }
}
