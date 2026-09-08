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

namespace NOpenNLP.Tools.Compat;

/// <summary>
/// The shared contract for the model compatibility harness: which corpora are
/// trained on, with which hyperparameters, and which inputs the resulting models
/// are asked to analyze.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. This is a
/// deliberate line-for-line counterpart of <c>CompatCorpus.java</c> in
/// <c>src/java/model-compat</c>. The two must stay in sync: the harness proves
/// that a model trained by one runtime behaves identically when loaded by the
/// other, and that only holds if both sides train on the same bytes with the
/// same settings and then ask the same questions. Change both together.
/// <para/>
/// Nothing here states an expected <i>answer</i>. The models are trained on a
/// few hundred sentences and their output is not interesting in its own right;
/// what matters is that the two runtimes agree. Each side therefore records what
/// its own model produced into a text file that the other side reads back and
/// compares against, which makes a disagreement a diff between two concrete
/// outputs rather than a failed assertion against a magic constant. See
/// <see cref="CompatResults"/>.
/// </remarks>
internal static class CompatCorpus
{
    /// <summary>The language code every model in the harness is trained for.</summary>
    public const string Language = "eng";

    /// <summary>
    /// Iterations and cutoff, fixed so both runtimes run the same number of
    /// training passes. The defaults differ per tool upstream, and a default that
    /// changed between the port and the release it targets would show up here as
    /// a behavioural difference that is really a configuration difference.
    /// </summary>
    public const int Iterations = 100;
    public const int Cutoff = 0;

    /// <summary>
    /// The name finder trains with the perceptron trainer, which is what
    /// <c>NameFinderME.Train</c> defaults to and the code path the pre-trained
    /// SourceForge name models use. Everything else here is maxent (GIS), the
    /// default for the other tools. Between them the two model readers and both
    /// scoring paths are exercised.
    /// </summary>
    public const int NameFinderIterations = 70;
    public const int NameFinderCutoff = 1;

    // ------------------------------------------------------------------
    // Corpora
    // ------------------------------------------------------------------

    /// <summary>File names under the <c>corpora/</c> folder, one per tool.</summary>
    public const string SentenceCorpus = "sentences.train";
    public const string TokenizerCorpus = "token.train";
    public const string NameFinderCorpus = "namefinder.train";
    public const string PosTaggerCorpus = "postagger.train";
    public const string ChunkerCorpus = "chunker.train";

    // ------------------------------------------------------------------
    // Model file names
    // ------------------------------------------------------------------

    public const string SentenceModelFile = "compat-sentence.bin";
    public const string TokenizerModelFile = "compat-token.bin";
    public const string NameFinderModelFile = "compat-ner.bin";
    public const string PosTaggerModelFile = "compat-pos.bin";
    public const string ChunkerModelFile = "compat-chunker.bin";

    /// <summary>The file each runtime writes its own model's inference output into.</summary>
    public const string ResultsFile = "results.txt";

    // ------------------------------------------------------------------
    // Inference inputs
    // ------------------------------------------------------------------

    /// <summary>
    /// Text for the sentence detector, chosen so the answer is not trivial: it
    /// contains an abbreviation followed by a capital letter ("Mr. Vinken"), a
    /// sentence-ending abbreviation ("Nov. 29."), and an internal one ("N.V.,").
    /// </summary>
    public const string SentenceInput =
        "Pierre Vinken, 61 years old, will join the board as a nonexecutive director Nov. 29. " +
        "Mr. Vinken is chairman of Elsevier N.V., the Dutch publishing group. " +
        "Rudolph Agnew, 55 years old and former chairman of Consolidated Gold Fields PLC, " +
        "was named a director of this British industrial conglomerate.";

    /// <summary>
    /// Text for the tokenizer. The learnable tokenizer has to decide where to
    /// split inside "N.V.," and around the contraction and the currency amount.
    /// </summary>
    public const string TokenizerInput =
        "Mr. Vinken isn't chairman of Elsevier N.V., the Dutch publishing group, " +
        "which paid $2.5 million in 1989.";

    /// <summary>
    /// Tokens for the name finder, POS tagger and chunker. Pre-tokenized so the
    /// three sequence models are exercised on identical input regardless of what
    /// the tokenizer above happens to do.
    /// </summary>
    public static readonly string[] TokenInput =
    [
        "Pierre", "Vinken", ",", "61", "years", "old", ",", "will", "join",
        "the", "board", "as", "a", "nonexecutive", "director", "Nov.", "29", "."
    ];

    /// <summary>
    /// A second token sequence, drawn from the name finder training corpus's
    /// subject matter rather than the newswire above, so the name finder has a
    /// realistic chance of tagging something. A model that finds nothing at all
    /// would make the two runtimes agree vacuously.
    /// </summary>
    public static readonly string[] NameInput =
    [
        "So", "I", "called", "Julie", ",", "a", "friend", "who", "'s", "still",
        "in", "contact", "with", "Alan", "McKennedy", "in", "Edinburgh", "."
    ];

    // ------------------------------------------------------------------
    // Locating the corpora
    // ------------------------------------------------------------------

    /// <summary>
    /// Resolves the <c>corpora/</c> folder by walking up from the test assembly,
    /// mirroring how <see cref="Integration.TestData"/> finds <c>testdata/</c>.
    /// Unlike the models, these are committed to the repository, so a failure to
    /// find them is a broken checkout rather than a missing download.
    /// </summary>
    /// <exception cref="InvalidOperationException">if the folder cannot be found</exception>
    public static string CorporaDirectory()
    {
        string? configured = Environment.GetEnvironmentVariable("NOPENNLP_COMPAT_CORPORA_DIR");
        if (!string.IsNullOrEmpty(configured))
        {
            return configured!;
        }

        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "src", "java", "model-compat", "corpora");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate src/java/model-compat/corpora from {AppContext.BaseDirectory}. " +
            "Set NOPENNLP_COMPAT_CORPORA_DIR to override.");
    }

    /// <summary>The full path of one corpus file.</summary>
    public static FileInfo Corpus(string name)
    {
        var file = new FileInfo(Path.Combine(CorporaDirectory(), name));
        if (!file.Exists)
        {
            throw new InvalidOperationException($"Training corpus not found: {file.FullName}");
        }

        return file;
    }
}
