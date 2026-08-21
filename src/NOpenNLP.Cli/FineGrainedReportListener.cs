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
using System.IO;
using System.Text;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Eval;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Cmdline;

public abstract class FineGrainedReportListener
{
    private static readonly char[] alpha =
    [
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h',
        'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
        'w', 'x', 'y', 'z',
    ];

    private readonly TextWriter printStream;
    private readonly Stats stats;

    // NOpenNLP: upstream's PrintStream constructor. A TextWriter is the .NET
    // counterpart, and it is what Console.Error already is.
    protected FineGrainedReportListener(TextWriter printStream)
    {
        this.printStream = printStream;
        this.stats = new Stats(this);
    }

    /// <summary>
    /// Writes the report to the <see cref="Stream"/>. Should be called only after
    /// the evaluation process.
    /// </summary>
    // NOpenNLP: upstream's OutputStream constructor, which wraps it in a PrintStream.
    // The StreamWriter is left to be flushed by whoever disposes the stream, matching
    // the PrintStream upstream never closes either.
    protected FineGrainedReportListener(Stream outputStream)
        : this(new StreamWriter(outputStream) { AutoFlush = true })
    {
    }

    private static string GenerateAlphaLabel(int index)
    {
        char[] labelChars = new char[3];
        int i;

        for (i = 2; i >= 0; i--)
        {
            if (index >= 0)
            {
                labelChars[i] = alpha[index % alpha.Length];
                index = index / alpha.Length - 1;
            }
            else
            {
                labelChars[i] = ' ';
            }
        }

        return new string(labelChars);
    }

    public abstract void WriteReport();

    // api methods
    // general stats

    protected Stats GetStats() => this.stats;

    private long GetNumberOfSentences() => stats.GetNumberOfSentences();

    private double GetAverageSentenceSize() => stats.GetAverageSentenceSize();

    private int GetMinSentenceSize() => stats.GetMinSentenceSize();

    private int GetMaxSentenceSize() => stats.GetMaxSentenceSize();

    private int GetNumberOfTags() => stats.GetNumberOfTags();

    // token stats

    private double GetAccuracy() => stats.GetAccuracy();

    private double GetTokenAccuracy(string token) => stats.GetTokenAccuracy(token);

    private IReadOnlyCollection<string> GetTokensOrderedByFrequency() =>
        stats.GetTokensOrderedByFrequency();

    private int GetTokenFrequency(string token) => stats.GetTokenFrequency(token);

    private int GetTokenErrors(string token) => stats.GetTokenErrors(token);

    private IReadOnlyCollection<string> GetTokensOrderedByNumberOfErrors() =>
        stats.GetTokensOrderedByNumberOfErrors();

    private IReadOnlyCollection<string> GetTagsOrderedByErrors() => stats.GetTagsOrderedByErrors();

    private int GetTagFrequency(string tag) => stats.GetTagFrequency(tag);

    private int GetTagErrors(string tag) => stats.GetTagErrors(tag);

    private double GetTagPrecision(string tag) => stats.GetTagPrecision(tag);

    private double GetTagRecall(string tag) => stats.GetTagRecall(tag);

    private double GetTagFMeasure(string tag) => stats.GetTagFMeasure(tag);

    private IReadOnlyCollection<string> GetConfusionMatrixTagset() => stats.GetConfusionMatrixTagset();

    private IReadOnlyCollection<string> GetConfusionMatrixTagset(string token) =>
        stats.GetConfusionMatrixTagset(token);

    private double[][] GetConfusionMatrix() => stats.GetConfusionMatrix();

    private double[][] GetConfusionMatrix(string token) => stats.GetConfusionMatrix(token);

    private static string MatrixToString(IReadOnlyCollection<string> tagset, double[][] data,
        bool filter)
    {
        // we dont want to print trivial cases (acc=1)
        int initialIndex = 0;
        string[] tags = [.. tagset];
        var sb = new StringBuilder();
        int minColumnSize = int.MinValue;
        string[][] matrix = new string[data.Length][];

        for (int k = 0; k < data.Length; k++)
        {
            matrix[k] = new string[data[0].Length];
        }

        for (int i = 0; i < data.Length; i++)
        {
            int j = 0;
            for (; j < data[i].Length - 1; j++)
            {
                matrix[i][j] = data[i][j] > 0
                    ? ((int)data[i][j]).ToString(CultureInfo.InvariantCulture)
                    : ".";
                if (minColumnSize < matrix[i][j].Length)
                {
                    minColumnSize = matrix[i][j].Length;
                }
            }

            matrix[i][j] = FormatPercent(data[i][j]);
            if (data[i][j] == 1 && filter)
            {
                initialIndex = i + 1;
            }
        }

        int columnWidth = minColumnSize + 2;

        for (int i = initialIndex; i < tagset.Count; i++)
        {
            // headerFormat is "%<columnWidth>s "
            sb.Append(PadLeft(GenerateAlphaLabel(i - initialIndex).Trim(), columnWidth)).Append(' ');
        }

        sb.Append("| Accuracy | <-- classified as\n");

        for (int i = initialIndex; i < data.Length; i++)
        {
            int j = initialIndex;
            for (; j < data[i].Length - 1; j++)
            {
                if (i == j)
                {
                    // diagFormat is " %<columnWidth>s"
                    string val = "<" + matrix[i][j] + ">";
                    sb.Append(' ').Append(PadLeft(val, columnWidth));
                }
                else
                {
                    // cellFormat is "%<columnWidth>s "
                    sb.Append(PadLeft(matrix[i][j], columnWidth)).Append(' ');
                }
            }

            // "|   %-6s |   %3s = "
            sb.Append("|   ").Append(PadRight(matrix[i][j], 6)).Append(" |   ")
                .Append(PadLeft(GenerateAlphaLabel(i - initialIndex), 3)).Append(" = ")
                .Append(tags[i]);
            sb.Append("\n");
        }

        return sb.ToString();
    }

    protected void PrintGeneralStatistics()
    {
        PrintHeader("Evaluation summary");
        printStream.Write(Format21And6("Number of sentences",
            GetNumberOfSentences().ToString(CultureInfo.InvariantCulture)));
        printStream.Write("\n");
        printStream.Write(Format21And6("Min sentence size",
            GetMinSentenceSize().ToString(CultureInfo.InvariantCulture)));
        printStream.Write("\n");
        printStream.Write(Format21And6("Max sentence size",
            GetMaxSentenceSize().ToString(CultureInfo.InvariantCulture)));
        printStream.Write("\n");
        printStream.Write(Format21And6("Average sentence size",
            FormatNumber(GetAverageSentenceSize(), 2)));
        printStream.Write("\n");
        printStream.Write(Format21And6("Tags count",
            GetNumberOfTags().ToString(CultureInfo.InvariantCulture)));
        printStream.Write("\n");
        printStream.Write(Format21And6("Accuracy", FormatPercent(GetAccuracy())));
        printStream.Write("\n");
        PrintFooter("Evaluation Corpus Statistics");
    }

    protected void PrintTokenOcurrenciesRank()
    {
        PrintHeader("Most frequent tokens");

        IReadOnlyCollection<string> toks = GetTokensOrderedByFrequency();
        const int maxLines = 20;

        int maxTokSize = 5;

        int count = 0;
        foreach (string tok in toks)
        {
            if (count++ >= maxLines)
            {
                break;
            }

            if (tok.Length > maxTokSize)
            {
                maxTokSize = tok.Length;
            }
        }

        int tableSize = maxTokSize + 19;

        // format is "| %3s | %6s | %<maxTokSize>s |"
        string Row(string pos, string cnt, string token) =>
            "| " + PadLeft(pos, 3) + " | " + PadLeft(cnt, 6) + " | " + PadLeft(token, maxTokSize) + " |";

        PrintLine(tableSize);
        printStream.Write(Row("Pos", "Count", "Token"));
        printStream.Write("\n");
        PrintLine(tableSize);

        // get the first 20 errors
        count = 0;
        foreach (string tok in toks)
        {
            if (count++ >= maxLines)
            {
                break;
            }

            int ocurrencies = GetTokenFrequency(tok);

            printStream.Write(Row(count.ToString(CultureInfo.InvariantCulture),
                ocurrencies.ToString(CultureInfo.InvariantCulture), tok));
            printStream.Write("\n");
        }

        PrintLine(tableSize);
        PrintFooter("Most frequent tokens");
    }

    protected void PrintTokenErrorRank()
    {
        PrintHeader("Tokens with the highest number of errors");
        printStream.Write("\n");

        IReadOnlyCollection<string> toks = GetTokensOrderedByNumberOfErrors();
        int maxTokenSize = 5;

        int count = 0;
        foreach (string tok in toks)
        {
            if (count++ >= 20)
            {
                break;
            }

            if (tok.Length > maxTokenSize)
            {
                maxTokenSize = tok.Length;
            }
        }

        int tableSize = 31 + maxTokenSize;

        // format is "| %<maxTokenSize>s | %6s | %5s | %7s |\n"
        string Row(string token, string errs, string cnt, string rate) =>
            "| " + PadLeft(token, maxTokenSize) + " | " + PadLeft(errs, 6) + " | "
                + PadLeft(cnt, 5) + " | " + PadLeft(rate, 7) + " |\n";

        PrintLine(tableSize);
        printStream.Write(Row("Token", "Errors", "Count", "% Err"));
        PrintLine(tableSize);

        // get the first 20 errors
        count = 0;
        foreach (string tok in toks)
        {
            if (count++ >= 20)
            {
                break;
            }

            int ocurrencies = GetTokenFrequency(tok);
            int errors = GetTokenErrors(tok);
            string rate = FormatPercent((double)errors / ocurrencies);

            printStream.Write(Row(tok, errors.ToString(CultureInfo.InvariantCulture),
                ocurrencies.ToString(CultureInfo.InvariantCulture), rate));
        }

        PrintLine(tableSize);
        PrintFooter("Tokens with the highest number of errors");
    }

    protected void PrintTagsErrorRank()
    {
        PrintHeader("Detailed Accuracy By Tag");
        IReadOnlyCollection<string> tags = GetTagsOrderedByErrors();
        printStream.Write("\n");

        int maxTagSize = 3;

        foreach (string t in tags)
        {
            if (t.Length > maxTagSize)
            {
                maxTagSize = t.Length;
            }
        }

        int tableSize = 65 + maxTagSize;

        // headerFormat is "| %<maxTagSize>s | %6s | %6s | %7s | %9s | %6s | %9s |\n"
        string HeaderRow(string tag, string errs, string cnt, string err,
            string precision, string recall, string fmeasure) =>
            "| " + PadLeft(tag, maxTagSize) + " | " + PadLeft(errs, 6) + " | " + PadLeft(cnt, 6)
                + " | " + PadLeft(err, 7) + " | " + PadLeft(precision, 9) + " | "
                + PadLeft(recall, 6) + " | " + PadLeft(fmeasure, 9) + " |\n";

        // format is "| %<maxTagSize>s | %6s | %6s | %-7s | %-9s | %-6s | %-9s |\n"
        string Row(string tag, string errs, string cnt, string err,
            string precision, string recall, string fmeasure) =>
            "| " + PadLeft(tag, maxTagSize) + " | " + PadLeft(errs, 6) + " | " + PadLeft(cnt, 6)
                + " | " + PadRight(err, 7) + " | " + PadRight(precision, 9) + " | "
                + PadRight(recall, 6) + " | " + PadRight(fmeasure, 9) + " |\n";

        PrintLine(tableSize);
        printStream.Write(HeaderRow("Tag", "Errors", "Count", "% Err", "Precision", "Recall",
            "F-Measure"));
        PrintLine(tableSize);

        foreach (string tag in tags)
        {
            int ocurrencies = GetTagFrequency(tag);
            int errors = GetTagErrors(tag);
            string rate = FormatNumber((double)errors / ocurrencies, 3);

            double p = GetTagPrecision(tag);
            double r = GetTagRecall(tag);
            double f = GetTagFMeasure(tag);

            printStream.Write(Row(tag, errors.ToString(CultureInfo.InvariantCulture),
                ocurrencies.ToString(CultureInfo.InvariantCulture), rate,
                FormatNumber(p > 0 ? p : 0, 3),
                FormatNumber(r > 0 ? r : 0, 3),
                FormatNumber(f > 0 ? f : 0, 3)));
        }

        PrintLine(tableSize);

        PrintFooter("Tags with the highest number of errors");
    }

    protected void PrintGeneralConfusionTable()
    {
        PrintHeader("Confusion matrix");

        IReadOnlyCollection<string> labels = GetConfusionMatrixTagset();

        double[][] confusionMatrix = GetConfusionMatrix();

        printStream.Write("\nTags with 100% accuracy: ");
        int line = 0;
        foreach (string label in labels)
        {
            if (confusionMatrix[line][confusionMatrix[0].Length - 1] == 1)
            {
                printStream.Write(label);
                printStream.Write(" (");
                printStream.Write(((int)confusionMatrix[line][line]).ToString(CultureInfo.InvariantCulture));
                printStream.Write(") ");
            }

            line++;
        }

        printStream.Write("\n\n");

        printStream.Write(MatrixToString(labels, confusionMatrix, true));

        PrintFooter("Confusion matrix");
    }

    protected void PrintDetailedConfusionMatrix()
    {
        PrintHeader("Confusion matrix for tokens");
        printStream.Write("  sorted by number of errors\n");
        IReadOnlyCollection<string> toks = GetTokensOrderedByNumberOfErrors();

        foreach (string t in toks)
        {
            double acc = GetTokenAccuracy(t);
            if (acc < 1)
            {
                printStream.Write("\n[");
                printStream.Write(t);
                printStream.Write("]\n");
                printStream.Write(Format12AndLeft8("Accuracy", FormatPercent(acc)));
                printStream.Write("\n");
                printStream.Write(Format12AndLeft8("Ocurrencies",
                    GetTokenFrequency(t).ToString(CultureInfo.InvariantCulture)));
                printStream.Write("\n");
                printStream.Write(Format12AndLeft8("Errors",
                    GetTokenErrors(t).ToString(CultureInfo.InvariantCulture)));
                printStream.Write("\n");

                IReadOnlyCollection<string> labels = GetConfusionMatrixTagset(t);

                double[][] confusionMatrix = GetConfusionMatrix(t);

                printStream.Write(MatrixToString(labels, confusionMatrix, false));
            }
        }

        PrintFooter("Confusion matrix for tokens");
    }

    /// <summary>Auxiliary method that prints a emphasised report header.</summary>
    private void PrintHeader(string text)
    {
        printStream.Write("=== ");
        printStream.Write(text);
        printStream.Write(" ===\n");
    }

    /// <summary>Auxiliary method that prints a marker to the end of a report.</summary>
    private void PrintFooter(string text)
    {
        printStream.Write("\n<-end> ");
        printStream.Write(text);
        printStream.Write("\n\n");
    }

    /// <summary>Auxiliary method that prints a horizontal line of a given size.</summary>
    private void PrintLine(int size)
    {
        for (int i = 0; i < size; i++)
        {
            printStream.Write("-");
        }

        printStream.Write("\n");
    }

    // NOpenNLP: the format helpers below stand in for Java's String.format and
    // MessageFormat. Java's "%<n>s" right-aligns to a minimum width and "%-<n>s"
    // left-aligns; neither truncates. MessageFormat's "{0,number,#.##}" and
    // "{0,number,#.##%}" round HALF_EVEN to at most the given number of fraction digits
    // and drop trailing zeros, which .NET's "0.##" custom format also does -- but .NET
    // rounds away from zero by default, so MidpointRounding.ToEven is applied first.
    // Numbers are rendered with the current culture, matching MessageFormat's use of the
    // default locale.

    private static string PadLeft(string value, int width) =>
        value.Length >= width ? value : value.PadLeft(width);

    private static string PadRight(string value, int width) =>
        value.Length >= width ? value : value.PadRight(width);

    /// <summary>Reproduces <c>String.format("%21s: %6s", label, value)</c>.</summary>
    private static string Format21And6(string label, string value) =>
        PadLeft(label, 21) + ": " + PadLeft(value, 6);

    /// <summary>Reproduces <c>String.format("%12s: %-8s", label, value)</c>.</summary>
    private static string Format12AndLeft8(string label, string value) =>
        PadLeft(label, 12) + ": " + PadRight(value, 8);

    /// <summary>Reproduces <c>MessageFormat.format("{0,number,#.##%}", value)</c>.</summary>
    private static string FormatPercent(double value) =>
        FormatHalfEven(value * 100, 2, CultureInfo.CurrentCulture) + "%";

    /// <summary>
    /// Reproduces <c>MessageFormat.format("{0,number,#.##}", value)</c> and
    /// <c>"{0,number,#.###}"</c>.
    /// </summary>
    private static string FormatNumber(double value, int fractionDigits) =>
        FormatHalfEven(value, fractionDigits, CultureInfo.CurrentCulture);

    /// <summary>
    /// Rounds <paramref name="value"/> to <paramref name="fractionDigits"/> the way Java's
    /// <c>DecimalFormat</c> does, and renders it with a <c>#0.##</c>-style pattern.
    /// </summary>
    /// <remarks>
    /// Java's <c>DecimalFormat</c> applies HALF_EVEN to the <b>exact binary value</b> of
    /// the double, not to its shortest decimal representation. <see cref="Math.Round(double, int, MidpointRounding)"/>
    /// does not reproduce that: it works on the binary value but returns a double, and the
    /// subsequent format re-rounds. Going through the 17-significant-digit representation
    /// keeps enough of the exact value to resolve the midpoint the same way Java does, so
    /// 0.955 (which is really 0.95499999999999996) rounds down to 0.95 as upstream prints
    /// it, while 0.12345 * 100 rounds up to 12.35. Checked against a real JVM on the
    /// values this report produces.
    /// </remarks>
    private static string FormatHalfEven(double value, int fractionDigits, CultureInfo culture)
    {
        decimal exact = decimal.Parse(
            value.ToString("G17", CultureInfo.InvariantCulture),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);

        decimal rounded = Math.Round(exact, fractionDigits, MidpointRounding.ToEven);

        return rounded.ToString("#0." + new string('#', fractionDigits), culture);
    }

    /// <summary>
    /// A comparator that sorts the confusion matrix labels according to the
    /// accuracy of each line.
    /// </summary>
    public class MatrixLabelComparator(IDictionary<string, ConfusionMatrixLine> confusionMatrix)
        : IComparer<string>
    {
        private readonly IDictionary<string, ConfusionMatrixLine> confusionMatrix = confusionMatrix;

        public virtual int Compare(string? o1, string? o2)
        {
            if (string.Equals(o1, o2, StringComparison.Ordinal))
            {
                return 0;
            }

            ConfusionMatrixLine? t1 = Lookup(confusionMatrix, o1);
            ConfusionMatrixLine? t2 = Lookup(confusionMatrix, o2);

            if (t1 == null || t2 == null)
            {
                if (t1 == null)
                {
                    return 1;
                }

                return -1;
            }

            double r1 = t1.Accuracy;
            double r2 = t2.Accuracy;
            if (r1 == r2)
            {
                return string.CompareOrdinal(o1, o2);
            }

            if (r2 > r1)
            {
                return 1;
            }

            return -1;
        }
    }

    public class GroupedMatrixLabelComparator : IComparer<string>
    {
        private readonly JCG.Dictionary<string, double> categoryAccuracy;
        private readonly IDictionary<string, ConfusionMatrixLine> confusionMatrix;

        public GroupedMatrixLabelComparator(IDictionary<string, ConfusionMatrixLine> confusionMatrix)
        {
            this.confusionMatrix = confusionMatrix;
            this.categoryAccuracy = new JCG.Dictionary<string, double>();

            // compute grouped categories
            foreach (KeyValuePair<string, ConfusionMatrixLine> entry in confusionMatrix)
            {
                string key = entry.Key;
                ConfusionMatrixLine confusionMatrixLine = entry.Value;
                string category = key.Contains("-") ? key.Split('-')[0] : key;

                categoryAccuracy.TryGetValue(category, out double currentAccuracy);
                categoryAccuracy[category] = currentAccuracy + confusionMatrixLine.Accuracy;
            }
        }

        public virtual int Compare(string? o1, string? o2)
        {
            if (string.Equals(o1, o2, StringComparison.Ordinal))
            {
                return 0;
            }

            string? c1 = o1;
            string? c2 = o2;

            if (o1 != null && o1.Contains("-"))
            {
                c1 = o1.Split('-')[0];
            }

            if (o2 != null && o2.Contains("-"))
            {
                c2 = o2.Split('-')[0];
            }

            if (string.Equals(c1, c2, StringComparison.Ordinal))
            {
                // same category - sort by confusion matrix
                ConfusionMatrixLine? t1 = Lookup(confusionMatrix, o1);
                ConfusionMatrixLine? t2 = Lookup(confusionMatrix, o2);

                if (t1 == null || t2 == null)
                {
                    if (t1 == null)
                    {
                        return 1;
                    }

                    return -1;
                }

                double r1 = t1.Accuracy;
                double r2 = t2.Accuracy;
                if (r1 == r2)
                {
                    return string.CompareOrdinal(o1, o2);
                }

                if (r2 > r1)
                {
                    return 1;
                }

                return -1;
            }
            else
            {
                // different category - sort by category
                double? t1 = c1 != null && categoryAccuracy.TryGetValue(c1, out double a1)
                    ? a1 : (double?)null;
                double? t2 = c2 != null && categoryAccuracy.TryGetValue(c2, out double a2)
                    ? a2 : (double?)null;

                if (t1 == null || t2 == null)
                {
                    if (t1 == null)
                    {
                        return 1;
                    }

                    return -1;
                }

                if (t1.Value.Equals(t2.Value))
                {
                    return string.CompareOrdinal(o1, o2);
                }

                if (t2 > t1)
                {
                    return 1;
                }

                return -1;
            }
        }
    }

    public virtual IComparer<string> GetMatrixLabelComparator(
        IDictionary<string, ConfusionMatrixLine> confusionMatrix) =>
        new MatrixLabelComparator(confusionMatrix);

    public class SimpleLabelComparator(IDictionary<string, Counter> map) : IComparer<string>
    {
        private readonly IDictionary<string, Counter> map = map;

        public virtual int Compare(string? o1, string? o2)
        {
            if (string.Equals(o1, o2, StringComparison.Ordinal))
            {
                return 0;
            }

            int e1 = 0, e2 = 0;

            if (o1 != null && map.TryGetValue(o1, out Counter? c1))
            {
                e1 = c1.Value;
            }

            if (o2 != null && map.TryGetValue(o2, out Counter? c2))
            {
                e2 = c2.Value;
            }

            if (e1 == e2)
            {
                return string.CompareOrdinal(o1, o2);
            }

            return e2 - e1;
        }
    }

    public virtual IComparer<string> GetLabelComparator(IDictionary<string, Counter> map) =>
        new SimpleLabelComparator(map);

    public class GroupedLabelComparator : IComparer<string>
    {
        private readonly JCG.Dictionary<string, int> categoryCounter;
        private readonly IDictionary<string, Counter> labelCounter;

        public GroupedLabelComparator(IDictionary<string, Counter> map)
        {
            this.labelCounter = map;
            this.categoryCounter = new JCG.Dictionary<string, int>();

            // compute grouped categories
            foreach (KeyValuePair<string, Counter> entry in labelCounter)
            {
                string key = entry.Key;
                Counter value = entry.Value;
                string category = key.Contains("-") ? key.Split('-')[0] : key;

                categoryCounter.TryGetValue(category, out int currentCount);
                categoryCounter[category] = currentCount + value.Value;
            }
        }

        public virtual int Compare(string? o1, string? o2)
        {
            if (string.Equals(o1, o2, StringComparison.Ordinal))
            {
                return 0;
            }

            string? c1 = o1;
            string? c2 = o2;

            if (o1 != null && o1.Contains("-"))
            {
                c1 = o1.Split('-')[0];
            }

            if (o2 != null && o2.Contains("-"))
            {
                c2 = o2.Split('-')[0];
            }

            if (string.Equals(c1, c2, StringComparison.Ordinal))
            {
                // same category - sort by confusion matrix
                Counter? t1 = o1 != null && labelCounter.TryGetValue(o1, out Counter? l1) ? l1 : null;
                Counter? t2 = o2 != null && labelCounter.TryGetValue(o2, out Counter? l2) ? l2 : null;

                if (t1 == null || t2 == null)
                {
                    if (t1 == null)
                    {
                        return 1;
                    }

                    return -1;
                }

                int r1 = t1.Value;
                int r2 = t2.Value;
                if (r1 == r2)
                {
                    return string.CompareOrdinal(o1, o2);
                }

                if (r2 > r1)
                {
                    return 1;
                }

                return -1;
            }
            else
            {
                // different category - sort by category
                int? t1 = c1 != null && categoryCounter.TryGetValue(c1, out int v1) ? v1 : (int?)null;
                int? t2 = c2 != null && categoryCounter.TryGetValue(c2, out int v2) ? v2 : (int?)null;

                if (t1 == null || t2 == null)
                {
                    if (t1 == null)
                    {
                        return 1;
                    }

                    return -1;
                }

                if (t1.Value.Equals(t2.Value))
                {
                    return string.CompareOrdinal(o1, o2);
                }

                if (t2 > t1)
                {
                    return 1;
                }

                return -1;
            }
        }
    }

    // NOpenNLP: Java's Map.get returns null for an absent key, and the comparators lean
    // on that; the ported dictionaries throw, so the lookups go through this helper.
    private static ConfusionMatrixLine? Lookup(IDictionary<string, ConfusionMatrixLine> map,
        string? key) =>
        key != null && map.TryGetValue(key, out ConfusionMatrixLine? value) ? value : null;

    /// <summary>
    /// Represents a line in the confusion table.
    /// </summary>
    public class ConfusionMatrixLine
    {
        internal readonly JCG.Dictionary<string, Counter> line =
            new JCG.Dictionary<string, Counter>(); // NOpenNLP: made readonly
        private readonly string @ref; // NOpenNLP: made readonly
        private int total = 0;
        private int correct = 0;
        private double acc = -1;

        /// <summary>
        /// Creates a new <see cref="ConfusionMatrixLine"/>.
        /// </summary>
        /// <param name="ref">the reference column</param>
        internal ConfusionMatrixLine(string @ref)
        {
            this.@ref = @ref;
        }

        /// <summary>
        /// Increments the counter for the given column and updates the statistics.
        /// </summary>
        /// <param name="column">the column to be incremented</param>
        internal void Increment(string column)
        {
            total++;
            if (column.Equals(@ref, StringComparison.Ordinal))
            {
                correct++;
            }

            if (!line.ContainsKey(column))
            {
                line[column] = new Counter();
            }

            // NOpenNLP: J2N's indexer is annotated as returning a nullable value; the
            // key was just ensured above, so it is not null here.
            line[column]!.Increment();
        }

        /// <summary>
        /// Gets the calculated accuracy of this element.
        /// </summary>
        public double Accuracy
        {
            get
            {
                // we save the accuracy because it is frequently used by the comparator
                if (Math.Abs(acc - 1.0d) < 0.0000000001)
                {
                    if (total == 0)
                    {
                        acc = 0.0d;
                    }

                    acc = (double)correct / total;
                }

                return acc;
            }
        }

        /// <summary>
        /// Gets the value given a column.
        /// </summary>
        /// <param name="column">the column</param>
        /// <returns>the counter value</returns>
        public int GetValue(string column) =>
            line.TryGetValue(column, out Counter? c) ? c.Value : 0;
    }

    /// <summary>
    /// Implements a simple counter.
    /// </summary>
    public class Counter
    {
        private int c = 0;

        internal void Increment() => c++;

        public int Value => c;
    }

    public class Stats
    {
        private readonly FineGrainedReportListener owner;

        // general statistics
        private readonly Mean accuracy = new Mean();
        private readonly Mean averageSentenceLength = new Mean();
        // token statistics
        private readonly JCG.Dictionary<string, Mean> tokAccuracies = new JCG.Dictionary<string, Mean>();
        private readonly JCG.Dictionary<string, Counter> tokOcurrencies = new JCG.Dictionary<string, Counter>();
        private readonly JCG.Dictionary<string, Counter> tokErrors = new JCG.Dictionary<string, Counter>();
        // tag statistics
        private readonly JCG.Dictionary<string, Counter> tagOcurrencies = new JCG.Dictionary<string, Counter>();
        private readonly JCG.Dictionary<string, Counter> tagErrors = new JCG.Dictionary<string, Counter>();
        private readonly JCG.Dictionary<string, FMeasure> tagFMeasure = new JCG.Dictionary<string, FMeasure>();
        // represents a Confusion Matrix that aggregates all tokens
        private readonly JCG.Dictionary<string, ConfusionMatrixLine> generalConfusionMatrix =
            new JCG.Dictionary<string, ConfusionMatrixLine>();
        // represents a set of Confusion Matrix for each token
        private readonly JCG.Dictionary<string, JCG.Dictionary<string, ConfusionMatrixLine>> tokenConfusionMatrix =
            new JCG.Dictionary<string, JCG.Dictionary<string, ConfusionMatrixLine>>();
        private int minimalSentenceLength = int.MaxValue;
        private int maximumSentenceLength = int.MinValue;

        internal Stats(FineGrainedReportListener owner)
        {
            this.owner = owner;
        }

        public void Add(string[] toks, string[] refs, string[] preds)
        {
            int length = toks.Length;
            averageSentenceLength.Add(length);

            if (minimalSentenceLength > length)
            {
                minimalSentenceLength = length;
            }

            if (maximumSentenceLength < length)
            {
                maximumSentenceLength = length;
            }

            UpdateTagFMeasure(refs, preds);

            for (int i = 0; i < toks.Length; i++)
            {
                Commit(toks[i], refs[i], preds[i]);
            }
        }

        public void Add(int length, string @ref, string pred)
        {
            averageSentenceLength.Add(length);

            if (minimalSentenceLength > length)
            {
                minimalSentenceLength = length;
            }

            if (maximumSentenceLength < length)
            {
                maximumSentenceLength = length;
            }

            // String[] toks = reference.getSentence();
            string[] refs = [@ref];
            string[] preds = [pred];

            UpdateTagFMeasure(refs, preds);

            Commit("", @ref, pred);
        }

        public void Add(string[] text, string @ref, string pred)
        {
            int length = text.Length;
            this.Add(length, @ref, pred);
        }

        // NOpenNLP: upstream takes a CharSequence; the callers pass a String, and the
        // only thing it reads is the length.
        public void Add(string text, string @ref, string pred)
        {
            int length = text.Length;
            this.Add(length, @ref, pred);
        }

        /// <summary>
        /// Includes a new evaluation data.
        /// </summary>
        /// <param name="tok">the evaluated token</param>
        /// <param name="ref">the reference pos tag</param>
        /// <param name="pred">the predicted pos tag</param>
        private void Commit(string tok, string @ref, string pred)
        {
            // token stats
            if (!tokAccuracies.ContainsKey(tok))
            {
                tokAccuracies[tok] = new Mean();
                tokOcurrencies[tok] = new Counter();
                tokErrors[tok] = new Counter();
            }

            tokOcurrencies[tok]!.Increment();

            // tag stats
            if (!tagOcurrencies.ContainsKey(@ref))
            {
                tagOcurrencies[@ref] = new Counter();
                tagErrors[@ref] = new Counter();
            }

            tagOcurrencies[@ref]!.Increment();

            // updates general, token and tag error stats
            if (@ref.Equals(pred, StringComparison.Ordinal))
            {
                tokAccuracies[tok]!.Add(1);
                accuracy.Add(1);
            }
            else
            {
                tokAccuracies[tok]!.Add(0);
                tokErrors[tok]!.Increment();
                tagErrors[@ref]!.Increment();
                accuracy.Add(0);
            }

            // populate confusion matrixes
            if (!generalConfusionMatrix.ContainsKey(@ref))
            {
                generalConfusionMatrix[@ref] = new ConfusionMatrixLine(@ref);
            }

            generalConfusionMatrix[@ref]!.Increment(pred);

            if (!tokenConfusionMatrix.ContainsKey(tok))
            {
                tokenConfusionMatrix[tok] = new JCG.Dictionary<string, ConfusionMatrixLine>();
            }

            if (!tokenConfusionMatrix[tok]!.ContainsKey(@ref))
            {
                tokenConfusionMatrix[tok]![@ref] = new ConfusionMatrixLine(@ref);
            }

            tokenConfusionMatrix[tok]![@ref]!.Increment(pred);
        }

        private void UpdateTagFMeasure(string[] refs, string[] preds)
        {
            // create a set with all tags
            var tags = new JCG.HashSet<string>(refs);
            tags.UnionWith(preds);

            // create samples for each tag
            foreach (string tag in tags)
            {
                var reference = new JCG.List<Span>();
                var prediction = new JCG.List<Span>();

                for (int i = 0; i < refs.Length; i++)
                {
                    if (refs[i].Equals(tag, StringComparison.Ordinal))
                    {
                        reference.Add(new Span(i, i + 1));
                    }

                    if (preds[i].Equals(tag, StringComparison.Ordinal))
                    {
                        prediction.Add(new Span(i, i + 1));
                    }
                }

                if (!this.tagFMeasure.ContainsKey(tag))
                {
                    this.tagFMeasure[tag] = new FMeasure();
                }

                // populate the fmeasure
                this.tagFMeasure[tag]!.UpdateScores([.. reference], [.. prediction]);
            }
        }

        internal double GetAccuracy() => accuracy.Value;

        internal int GetNumberOfTags() => this.tagOcurrencies.Keys.Count;

        internal long GetNumberOfSentences() => this.averageSentenceLength.Count;

        internal double GetAverageSentenceSize() => this.averageSentenceLength.Value;

        internal int GetMinSentenceSize() => this.minimalSentenceLength;

        internal int GetMaxSentenceSize() => this.maximumSentenceLength;

        internal double GetTokenAccuracy(string token) => tokAccuracies[token]!.Value;

        internal int GetTokenErrors(string token) => tokErrors[token]!.Value;

        internal int GetTokenFrequency(string token) => tokOcurrencies[token]!.Value;

        internal IReadOnlyCollection<string> GetTokensOrderedByFrequency()
        {
            var toks = new JCG.SortedSet<string>(new SimpleLabelComparator(tokOcurrencies));
            toks.UnionWith(tokOcurrencies.Keys);
            return toks;
        }

        internal IReadOnlyCollection<string> GetTokensOrderedByNumberOfErrors()
        {
            var toks = new JCG.SortedSet<string>(new SimpleLabelComparator(tokErrors));
            toks.UnionWith(tokErrors.Keys);
            return toks;
        }

        internal int GetTagFrequency(string tag) => tagOcurrencies[tag]!.Value;

        internal int GetTagErrors(string tag) => tagErrors[tag]!.Value;

        internal double GetTagFMeasure(string tag) => tagFMeasure[tag]!.Value;

        internal double GetTagRecall(string tag) => tagFMeasure[tag]!.RecallScore;

        internal double GetTagPrecision(string tag) => tagFMeasure[tag]!.PrecisionScore;

        internal IReadOnlyCollection<string> GetTagsOrderedByErrors()
        {
            var tags = new JCG.SortedSet<string>(owner.GetLabelComparator(tagErrors));
            tags.UnionWith(tagErrors.Keys);
            return tags;
        }

        internal IReadOnlyCollection<string> GetConfusionMatrixTagset() =>
            GetConfusionMatrixTagset(generalConfusionMatrix);

        internal double[][] GetConfusionMatrix() =>
            CreateConfusionMatrix(GetConfusionMatrixTagset(), generalConfusionMatrix);

        internal IReadOnlyCollection<string> GetConfusionMatrixTagset(string token) =>
            GetConfusionMatrixTagset(tokenConfusionMatrix[token]!);

        internal double[][] GetConfusionMatrix(string token) =>
            CreateConfusionMatrix(GetConfusionMatrixTagset(token), tokenConfusionMatrix[token]!);

        /// <summary>
        /// Creates a matrix with N lines and N + 1 columns with the data from
        /// confusion matrix. The last column is the accuracy.
        /// </summary>
        private static double[][] CreateConfusionMatrix(IReadOnlyCollection<string> tagset,
            IDictionary<string, ConfusionMatrixLine> data)
        {
            int size = tagset.Count;
            double[][] matrix = new double[size][];
            for (int i = 0; i < size; i++)
            {
                matrix[i] = new double[size + 1];
            }

            int line = 0;
            foreach (string @ref in tagset)
            {
                int column = 0;
                ConfusionMatrixLine? refLine = Lookup(data, @ref);

                foreach (string pred in tagset)
                {
                    matrix[line][column] = refLine != null ? refLine.GetValue(pred) : 0;
                    column++;
                }

                // set accuracy
                matrix[line][column] = refLine != null ? refLine.Accuracy : 0;
                line++;
            }

            return matrix;
        }

        private IReadOnlyCollection<string> GetConfusionMatrixTagset(
            IDictionary<string, ConfusionMatrixLine> data)
        {
            var tags = new JCG.SortedSet<string>(owner.GetMatrixLabelComparator(data));
            tags.UnionWith(data.Keys);

            var col = new JCG.List<string>();
            foreach (string t in tags)
            {
                col.AddRange(data[t]!.line.Keys);
            }

            tags.UnionWith(col);
            return tags;
        }
    }
}
