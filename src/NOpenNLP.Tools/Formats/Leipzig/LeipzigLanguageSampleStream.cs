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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using J2N;
using J2N.Collections.Generic.Extensions;
using NOpenNLP.Tools.Langdetect;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Leipzig;

public class LeipzigLanguageSampleStream : ObjectStreamBase<LanguageSample?>
{
    // NOpenNLP: upstream matches the first three characters of a file name against
    // "[a-z]+" with String.matches, which requires the whole input to match. .NET's
    // IsMatch searches, so the pattern is anchored to keep the same meaning.
    private static readonly Regex LanguagePrefixPattern = new("^[a-z]+$", RegexOptions.CultureInvariant);

    private class LeipzigSentencesStream : ObjectStreamBase<LanguageSample?>
    {
        private readonly LeipzigLanguageSampleStream outerInstance;

        private readonly string lang;

        // NOpenNLP: upstream holds a java.util.Iterator over the shuffled sentences.
        // That is the manual hasNext()/next() advance an IEnumerator<string> expresses;
        // an IEnumerable<string> cannot carry the position across Read calls.
        private readonly IEnumerator<string> lineIterator;

        /// <exception cref="IOException">if there is an error during reading</exception>
        internal LeipzigSentencesStream(LeipzigLanguageSampleStream outerInstance, string lang,
            FileInfo sentencesFile, int sentencesPerSample, int numberOfSamples)
        {
            this.outerInstance = outerInstance;
            this.lang = lang;

            // The file name contains the number of lines, but to make this more stable
            // the file is once scanned for the count even tough this is slower
            // NOpenNLP: Files.lines(..).count() streams the file rather than materializing
            // it; File.ReadLines does the same.
            int totalLineCount = File.ReadLines(sentencesFile.FullName).Count();
            int requiredLines = sentencesPerSample * numberOfSamples;

            if (totalLineCount < requiredLines)
            {
                throw new InvalidFormatException(string.Format(CultureInfo.InvariantCulture,
                    "{0} does not contain enough lines ({1} lines < {2} required lines).",
                    sentencesFile.FullName, totalLineCount, requiredLines));
            }

            var indexes = new JCG.List<int>(Enumerable.Range(0, totalLineCount));

            // NOpenNLP: upstream shuffles with a fixed-seed java.util.Random so the chosen
            // lines are deterministic. J2N's Randomizer reproduces java.util.Random and its
            // Shuffle reproduces Collections.shuffle, so the selection matches upstream
            // exactly. System.Random uses a different algorithm and would silently diverge.
            indexes.Shuffle(outerInstance.random);

            var selectedLines = new JCG.HashSet<int>(indexes.Take(requiredLines));

            JCG.List<string> sentences = [];

            using (IObjectStream<string?> lineStream = new PlainTextByLineStream(
                new MarkableFileInputStreamFactory(sentencesFile), Encoding.UTF8))
            {
                int lineIndex = 0;
                while (lineStream.Read() is { } line)
                {
                    int tabIndex = line.IndexOf('\t');
                    if (tabIndex != -1)
                    {
                        if (selectedLines.Contains(lineIndex))
                        {
                            sentences.Add(line);
                        }
                    }

                    lineIndex++;
                }
            }

            // NOpenNLP: see the note above -- the same fixed-seed Random instance is
            // reused here, so this shuffle must reproduce java.util.Random as well.
            sentences.Shuffle(outerInstance.random);

            lineIterator = sentences.GetEnumerator();
        }

        /// <inheritdoc/>
        /// <exception cref="IOException">if there is an error during reading</exception>
        public override LanguageSample? Read()
        {
            var sampleString = new StringBuilder();

            int count = 0;
            while (count < outerInstance.sentencesPerSample && lineIterator.MoveNext())
            {
                string line = lineIterator.Current;
                int textStart = line.IndexOf('\t') + 1;

                sampleString.Append(line[textStart..]).Append(' ');

                count++;
            }

            if (sampleString.Length > 0)
            {
                return new LanguageSample(new Language(lang), sampleString.ToString());
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lineIterator.Dispose();
            }
        }
    }

    private readonly int sentencesPerSample;

    private readonly IDictionary<string, int> langSampleCounts; // NOpenNLP: made readonly
    private readonly FileInfo[] sentencesFiles; // NOpenNLP: made readonly

    // NOpenNLP: upstream iterates the file array with a java.util.Iterator that reset()
    // replaces; an index over the array says the same thing without an enumerator to
    // dispose, and read() only ever asks "is there another file".
    private int sentencesFileIndex;
    private IObjectStream<LanguageSample?>? sampleStream;

    private readonly Random random;

    /// <exception cref="IOException">if there is an error during reading</exception>
    public LeipzigLanguageSampleStream(DirectoryInfo leipzigFolder, int sentencesPerSample,
        int samplesPerLanguage)
    {
        this.sentencesPerSample = sentencesPerSample;

        // NOpenNLP: File.listFiles(FileFilter) returns files and directories; upstream's
        // filter keeps only non-hidden regular files, so enumerating files alone covers it.
        // A leading dot is what marks a file hidden on the Unix-like systems the corpus is
        // read on, and FileAttributes.Hidden covers Windows.
        JCG.List<FileInfo> files = [];

        foreach (var file in leipzigFolder.EnumerateFiles())
        {
            if (IsHidden(file))
            {
                continue;
            }

            if (file.Name.Length >= 3 && LanguagePrefixPattern.IsMatch(file.Name[..3]))
            {
                files.Add(file);
            }
        }

        // NOpenNLP: Arrays.sort on java.io.File compares path names lexicographically by
        // char; an ordinal comparison is the .NET equivalent and, unlike the default
        // string comparison, does not vary by culture.
        files.Sort((x, y) => string.CompareOrdinal(x.FullName, y.FullName));

        sentencesFiles = [.. files];

        // NOpenNLP: upstream groups the files by their three-character language prefix,
        // counts each group, then divides samplesPerLanguage by that count.
        JCG.Dictionary<string, int> langCounts = [];

        foreach (var file in sentencesFiles)
        {
            string prefix = file.Name[..3];
            langCounts.TryGetValue(prefix, out int count);
            langCounts[prefix] = count + 1;
        }

        JCG.Dictionary<string, int> sampleCounts = [];

        foreach (var entry in langCounts)
        {
            sampleCounts[entry.Key] = samplesPerLanguage / entry.Value;
        }

        langSampleCounts = sampleCounts;

        // NOpenNLP: the fixed seed makes the shuffles in LeipzigSentencesStream
        // reproducible. J2N's Randomizer reproduces java.util.Random bit for bit;
        // System.Random uses a different algorithm and would silently diverge.
        random = new Randomizer(23);

        // NOpenNLP: upstream ends the constructor with a call to reset(). Calling a
        // virtual member from a constructor would run a derived override before the
        // derived object is initialized, so the two assignments are inlined instead.
        sentencesFileIndex = 0;
        sampleStream = null;
    }

    // NOpenNLP: stands in for java.io.File.isHidden().
    private static bool IsHidden(FileInfo file) =>
        file.Name.StartsWith(".", StringComparison.Ordinal)
        || (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override LanguageSample? Read()
    {
        // NOpenNLP: converted recursion to iteration
        while (true)
        {
            LanguageSample? sample;
            if (sampleStream != null && (sample = sampleStream.Read()) != null)
            {
                return sample;
            }
            else
            {
                if (sentencesFileIndex < sentencesFiles.Length)
                {
                    var sentencesFile = sentencesFiles[sentencesFileIndex];
                    sentencesFileIndex++;

                    string lang = sentencesFile.Name[..3];

                    sampleStream = new LeipzigSentencesStream(this, lang, sentencesFile, sentencesPerSample, langSampleCounts[lang]);

                    continue;
                }
            }

            return null;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    public override void Reset()
    {
        sentencesFileIndex = 0;
        sampleStream = null;
    }
}
