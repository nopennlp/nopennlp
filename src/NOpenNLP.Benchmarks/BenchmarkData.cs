/*
 * Copyright 2026 NOpenNLP Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.IO;

namespace NOpenNLP.Benchmarks;

/// <summary>
/// Locates the pre-trained models and supplies the text every benchmark runs on.
/// </summary>
/// <remarks>
/// The models are the same Apache OpenNLP 1.5 set the integration tests use,
/// fetched by <c>build/download-test-models.ps1</c> into a gitignored
/// <c>testdata/</c> at the repository root. <c>NOPENNLP_TEST_DATA_DIR</c>
/// overrides the location, matching the test suite's variable so one download
/// serves both.
/// <para/>
/// Unlike the tests, a missing model is a hard failure here. A benchmark that
/// silently skipped would report an empty summary that looks like a successful
/// run.
/// </remarks>
internal static class BenchmarkData
{
    private const string DataDirVariable = "NOPENNLP_TEST_DATA_DIR";

    /// <summary>
    /// The text the sentence detector runs on, and the source of the sentences
    /// every other tool consumes.
    /// </summary>
    /// <remarks>
    /// The opening of the Penn Treebank Wall Street Journal sample: newswire of
    /// the kind these models were trained on, with the abbreviations and the
    /// mid-sentence "N.V.," that make sentence detection and tokenization
    /// non-trivial. Same text as the integration tests, so a benchmark and a
    /// test failure can be read against each other.
    /// </remarks>
    public const string SampleText =
        "Pierre Vinken, 61 years old, will join the board as a nonexecutive director Nov. 29. " +
        "Mr. Vinken is chairman of Elsevier N.V., the Dutch publishing group. " +
        "Rudolph Agnew, 55 years old and former chairman of Consolidated Gold Fields PLC, " +
        "was named a director of this British industrial conglomerate.";

    /// <summary>
    /// The tokens of the first sentence of <see cref="SampleText"/>.
    /// </summary>
    /// <remarks>
    /// Stated as a constant rather than produced by the tokenizer so that the
    /// POS, chunker and name finder benchmarks measure only themselves. Feeding
    /// them a tokenizer's output would fold its cost into every downstream
    /// number, and would also make those benchmarks fail for a tokenizer defect.
    /// </remarks>
    public static readonly string[] SentenceTokens =
    [
        "Pierre", "Vinken", ",", "61", "years", "old", ",", "will", "join", "the",
        "board", "as", "a", "nonexecutive", "director", "Nov.", "29", "."
    ];

    /// <summary>
    /// The POS tags of <see cref="SentenceTokens"/>, which the chunker needs
    /// alongside the tokens.
    /// </summary>
    public static readonly string[] SentenceTags =
    [
        "NNP", "NNP", ",", "CD", "NNS", "JJ", ",", "MD", "VB", "DT", "NN", "IN",
        "DT", "JJ", "NN", "NNP", "CD", "."
    ];

    /// <summary>
    /// A sentence carrying an entity of every type the name finder models
    /// recognize, so one input exercises all seven.
    /// </summary>
    public static readonly string[] EntityTokens =
    [
        "John", "Smith", "paid", "$", "25.5", "million", "to", "Acme", "Corp.",
        "in", "Chicago", "yesterday", "afternoon", ",", "a", "15", "%", "increase", "."
    ];

    /// <summary>
    /// The full path to the named model under <c>testdata/models-sf/</c>.
    /// </summary>
    /// <exception cref="FileNotFoundException">
    /// The model has not been downloaded.
    /// </exception>
    public static string ModelPath(string name)
    {
        string directory = ResolveDataDirectory()
            ?? throw new DirectoryNotFoundException(
                "Benchmark models not found. Run build/download-test-models.ps1, or set " +
                $"{DataDirVariable} to the directory holding models-sf/.");

        string path = Path.Combine(directory, "models-sf", name);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Model '{name}' not found under {directory}. " +
                "Run build/download-test-models.ps1 to fetch the models.", path);
        }

        return path;
    }

    private static string? ResolveDataDirectory()
    {
        string? configured = Environment.GetEnvironmentVariable(DataDirVariable);
        if (!string.IsNullOrEmpty(configured))
        {
            return Directory.Exists(configured) ? configured : null;
        }

        // Walk up from the benchmark assembly to the repository root, which is
        // the directory holding testdata/. BenchmarkDotNet runs each benchmark
        // in a generated project whose output sits at a different depth than the
        // host, so the marker directory is searched for rather than assumed.
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "testdata");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
