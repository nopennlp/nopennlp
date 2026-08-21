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
using System.Globalization;
using System.Text;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Eval;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// This listener will gather detailed information about the sample under evaluation and will
/// allow detailed FMeasure for each outcome.
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public abstract class DetailedFMeasureListener<T> : IEvaluationMonitor<T>
{
    private int samples = 0;
    private readonly Stats generalStats = new Stats(); // NOpenNLP: made readonly
    private readonly JCG.Dictionary<string, Stats> statsForOutcome =
        new JCG.Dictionary<string, Stats>(); // NOpenNLP: made readonly

    protected abstract Span[] AsSpanArray(T sample);

    public virtual void CorrectlyClassified(T reference, T prediction)
    {
        samples++;
        // add all true positives!
        Span[] spans = AsSpanArray(reference);
        foreach (Span span in spans)
        {
            AddTruePositive(span.Type);
        }
    }

    public virtual void Misclassified(T reference, T prediction)
    {
        samples++;
        Span[] references = AsSpanArray(reference);
        Span[] predictions = AsSpanArray(prediction);

        var refSet = new JCG.HashSet<Span>(references);
        var predSet = new JCG.HashSet<Span>(predictions);

        foreach (Span @ref in refSet)
        {
            if (predSet.Contains(@ref))
            {
                AddTruePositive(@ref.Type);
            }
            else
            {
                AddFalseNegative(@ref.Type);
            }
        }

        foreach (Span pred in predSet)
        {
            if (!refSet.Contains(pred))
            {
                AddFalsePositive(pred.Type);
            }
        }
    }

    private void AddTruePositive(string? type)
    {
        Stats s = InitStatsForOutcomeAndGet(type);
        s.IncrementTruePositive();
        s.IncrementTarget();

        generalStats.IncrementTruePositive();
        generalStats.IncrementTarget();
    }

    private void AddFalsePositive(string? type)
    {
        Stats s = InitStatsForOutcomeAndGet(type);
        s.IncrementFalsePositive();
        generalStats.IncrementFalsePositive();
    }

    private void AddFalseNegative(string? type)
    {
        Stats s = InitStatsForOutcomeAndGet(type);
        s.IncrementTarget();
        generalStats.IncrementTarget();
    }

    // NOpenNLP: Span.Type is nullable here where upstream's getType() is not annotated;
    // a null type would be a HashMap key of null upstream, which the ported dictionary
    // does not allow, so it is mapped to the string "null" -- the text Java would print
    // for it in the report anyway.
    private Stats InitStatsForOutcomeAndGet(string? type)
    {
        string key = type ?? "null";

        if (!statsForOutcome.ContainsKey(key))
        {
            statsForOutcome[key] = new Stats();
        }

        // NOpenNLP: J2N's indexer is annotated as returning a nullable value; the key
        // was just ensured above, so it is not null here.
        return statsForOutcome[key]!;
    }

    // NOpenNLP: upstream's "% 7.2f%%" is "% 7.2f%%" -- a space flag (a leading space
    // for non-negative values) with width 7 and 2 decimals. .NET composite formatting has
    // no space flag, so the whole line is composed by FormatPercent below.
    private const string TotalLabel = "TOTAL";

    public string CreateReport() => CreateReport(CultureInfo.CurrentCulture);

    public string CreateReport(CultureInfo locale)
    {
        var ret = new StringBuilder();
        int tp = generalStats.TruePositives;
        int found = generalStats.FalsePositives + tp;
        ret.Append("Evaluated ").Append(samples).Append(" samples with ")
            .Append(generalStats.Target).Append(" entities; found: ")
            .Append(found).Append(" entities; correct: ").Append(tp).Append(".\n");

        ret.Append(Format(locale, TotalLabel,
            ZeroOrPositive(generalStats.PrecisionScore * 100),
            ZeroOrPositive(generalStats.RecallScore * 100),
            ZeroOrPositive(generalStats.FMeasure * 100)));
        ret.Append("\n");

        var set = new JCG.SortedSet<string>(new F1Comparator(this));
        set.UnionWith(statsForOutcome.Keys);

        foreach (string type in set)
        {
            Stats stats = statsForOutcome[type]!;

            ret.Append(FormatExtra(locale, type,
                ZeroOrPositive(stats.PrecisionScore * 100),
                ZeroOrPositive(stats.RecallScore * 100),
                ZeroOrPositive(stats.FMeasure * 100),
                stats.Target, stats.TruePositives, stats.FalsePositives));
            ret.Append("\n");
        }

        return ret.ToString();
    }

    // NOpenNLP: reproduces "%12s: precision: % 7.2f%%;  recall: % 7.2f%%; F1: % 7.2f%%."
    private static string Format(CultureInfo locale, string label,
        double precision, double recall, double fmeasure) =>
        PadLeft(label, 12) + ": precision: " + FormatPercent(locale, precision)
            + "%;  recall: " + FormatPercent(locale, recall)
            + "%; F1: " + FormatPercent(locale, fmeasure) + "%.";

    // NOpenNLP: reproduces FORMAT_EXTRA, which is FORMAT + " [target: %3d; tp: %3d; fp: %3d]"
    private static string FormatExtra(CultureInfo locale, string label,
        double precision, double recall, double fmeasure,
        int target, int truePositives, int falsePositives) =>
        Format(locale, label, precision, recall, fmeasure)
            + " [target: " + PadLeft(target.ToString(locale), 3)
            + "; tp: " + PadLeft(truePositives.ToString(locale), 3)
            + "; fp: " + PadLeft(falsePositives.ToString(locale), 3) + "]";

    // NOpenNLP: Java's "% 7.2f" prints a leading space for non-negative values and pads
    // the result to a width of 7.
    private static string FormatPercent(CultureInfo locale, double value)
    {
        string text = value.ToString("F2", locale);

        if (value >= 0)
        {
            text = " " + text;
        }

        return PadLeft(text, 7);
    }

    private static string PadLeft(string value, int width) =>
        value.Length >= width ? value : value.PadLeft(width);

    public override string ToString() => CreateReport();

    private static double ZeroOrPositive(double v) => v < 0 ? 0 : v;

    private sealed class F1Comparator(DetailedFMeasureListener<T> owner) : IComparer<string>
    {
        private readonly DetailedFMeasureListener<T> owner = owner;

        public int Compare(string? o1, string? o2)
        {
            if (string.Equals(o1, o2, StringComparison.Ordinal))
            {
                return 0;
            }

            double t1 = 0;
            double t2 = 0;

            if (o1 != null && owner.statsForOutcome.TryGetValue(o1, out Stats? s1))
            {
                t1 += s1.FMeasure;
            }

            if (o2 != null && owner.statsForOutcome.TryGetValue(o2, out Stats? s2))
            {
                t2 += s2.FMeasure;
            }

            t1 = ZeroOrPositive(t1);
            t2 = ZeroOrPositive(t2);

            if (t1 + t2 > 0d)
            {
                if (t1 > t2)
                {
                    return -1;
                }

                return 1;
            }

            return string.CompareOrdinal(o1, o2);
        }
    }

    /// <summary>
    /// Store the statistics.
    /// </summary>
    private sealed class Stats
    {
        // maybe we could use FMeasure class, but it wouldn't allow us to get
        // details like total number of false positives and true positives.

        private int falsePositiveCounter = 0;
        private int truePositiveCounter = 0;
        private int targetCounter = 0;

        public void IncrementFalsePositive() => falsePositiveCounter++;

        public void IncrementTruePositive() => truePositiveCounter++;

        public void IncrementTarget() => targetCounter++;

        public int FalsePositives => falsePositiveCounter;

        public int TruePositives => truePositiveCounter;

        public int Target => targetCounter;

        /// <summary>
        /// Retrieves the arithmetic mean of the precision scores calculated for each
        /// evaluated sample.
        /// </summary>
        public double PrecisionScore
        {
            get
            {
                int tp = TruePositives;
                int selected = tp + FalsePositives;
                return selected > 0 ? (double)tp / selected : 0;
            }
        }

        /// <summary>
        /// Retrieves the arithmetic mean of the recall score calculated for each
        /// evaluated sample.
        /// </summary>
        public double RecallScore
        {
            get
            {
                int target = Target;
                int tp = TruePositives;
                return target > 0 ? (double)tp / target : 0;
            }
        }

        /// <summary>
        /// Retrieves the f-measure score.
        /// <para/>
        /// f-measure = 2 * precision * recall / (precision + recall)
        /// </summary>
        /// <returns>the f-measure or -1 if precision + recall &lt;= 0</returns>
        public double FMeasure
        {
            get
            {
                if (PrecisionScore + RecallScore > 0)
                {
                    return 2 * (PrecisionScore * RecallScore)
                        / (PrecisionScore + RecallScore);
                }

                // cannot divide by zero, return error code
                return -1;
            }
        }
    }
}
