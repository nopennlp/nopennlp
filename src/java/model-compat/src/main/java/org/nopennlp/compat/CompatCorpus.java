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
package org.nopennlp.compat;

import java.io.File;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * The shared contract for the model compatibility harness: which corpora are
 * trained on, with which hyperparameters, and which inputs the resulting models
 * are asked to analyze.
 *
 * <p>This is a deliberate line-for-line counterpart of {@code CompatCorpus.cs}
 * in {@code src/NOpenNLP.Tools.Tests/Integration}. The two must stay in sync:
 * the harness proves that a model trained by one runtime behaves identically
 * when loaded by the other, and that only holds if both sides train on the same
 * bytes with the same settings and then ask the same questions. Change both
 * together.
 *
 * <p>Nothing here states an expected <em>answer</em>. The models are trained on
 * a few hundred sentences and their output is not interesting in its own right;
 * what matters is that the two runtimes agree. Each side therefore records what
 * its own model produced into a JSON-ish text file that the other side reads
 * back and compares against, which makes a disagreement a diff between two
 * concrete outputs rather than a failed assertion against a magic constant.
 * See {@link CompatResults}.
 */
public final class CompatCorpus {

    private CompatCorpus() {
    }

    /** The language code every model in the harness is trained for. */
    public static final String LANGUAGE = "eng";

    /**
     * Iterations and cutoff, fixed so both runtimes run the same number of
     * training passes. The defaults differ per tool upstream, and a default that
     * changed between the port and the release it targets would show up here as a
     * behavioural difference that is really a configuration difference.
     */
    public static final int ITERATIONS = 100;
    public static final int CUTOFF = 0;

    /**
     * The name finder trains with the perceptron trainer, which is what
     * {@code NameFinderME.train} defaults to and the code path the pre-trained
     * SourceForge name models use. Everything else here is maxent (GIS), the
     * default for the other tools. Between them the two model readers and both
     * scoring paths are exercised.
     */
    public static final int NAME_FINDER_ITERATIONS = 70;
    public static final int NAME_FINDER_CUTOFF = 1;

    // ------------------------------------------------------------------
    // Corpora
    // ------------------------------------------------------------------

    /** File names under the {@code corpora/} folder, one per tool. */
    public static final String SENTENCE_CORPUS = "sentences.train";
    public static final String TOKENIZER_CORPUS = "token.train";
    public static final String NAME_FINDER_CORPUS = "namefinder.train";
    public static final String POS_TAGGER_CORPUS = "postagger.train";
    public static final String CHUNKER_CORPUS = "chunker.train";

    // ------------------------------------------------------------------
    // Model file names
    // ------------------------------------------------------------------

    public static final String SENTENCE_MODEL = "compat-sentence.bin";
    public static final String TOKENIZER_MODEL = "compat-token.bin";
    public static final String NAME_FINDER_MODEL = "compat-ner.bin";
    public static final String POS_TAGGER_MODEL = "compat-pos.bin";
    public static final String CHUNKER_MODEL = "compat-chunker.bin";

    /** The file each runtime writes its own model's inference output into. */
    public static final String RESULTS_FILE = "results.txt";

    // ------------------------------------------------------------------
    // Inference inputs
    // ------------------------------------------------------------------

    /**
     * Text for the sentence detector, chosen so the answer is not trivial: it
     * contains an abbreviation followed by a capital letter ("Mr. Vinken"), a
     * sentence-ending abbreviation ("Nov. 29."), and an internal one ("N.V.,").
     */
    public static final String SENTENCE_INPUT =
        "Pierre Vinken, 61 years old, will join the board as a nonexecutive director Nov. 29. "
            + "Mr. Vinken is chairman of Elsevier N.V., the Dutch publishing group. "
            + "Rudolph Agnew, 55 years old and former chairman of Consolidated Gold Fields PLC, "
            + "was named a director of this British industrial conglomerate.";

    /**
     * Text for the tokenizer. The learnable tokenizer has to decide where to
     * split inside "N.V.," and around the contraction and the currency amount.
     */
    public static final String TOKENIZER_INPUT =
        "Mr. Vinken isn't chairman of Elsevier N.V., the Dutch publishing group, "
            + "which paid $2.5 million in 1989.";

    /**
     * Tokens for the name finder, POS tagger and chunker. Pre-tokenized so the
     * three sequence models are exercised on identical input regardless of what
     * the tokenizer above happens to do.
     */
    public static final String[] TOKEN_INPUT = {
        "Pierre", "Vinken", ",", "61", "years", "old", ",", "will", "join",
        "the", "board", "as", "a", "nonexecutive", "director", "Nov.", "29", "."
    };

    /**
     * A second token sequence, drawn from the name finder training corpus's
     * subject matter rather than the newswire above, so the name finder has a
     * realistic chance of tagging something. A model that finds nothing at all
     * would make the two runtimes agree vacuously.
     */
    public static final String[] NAME_INPUT = {
        "So", "I", "called", "Julie", ",", "a", "friend", "who", "'s", "still",
        "in", "contact", "with", "Alan", "McKennedy", "in", "Edinburgh", "."
    };

    // ------------------------------------------------------------------
    // Locating the corpora
    // ------------------------------------------------------------------

    /**
     * Resolves the {@code corpora/} folder by walking up from the working
     * directory, so the harness runs the same whether Maven was invoked from the
     * project folder or the repository root.
     *
     * @throws IllegalStateException if the folder cannot be found
     */
    public static Path corporaDirectory() {
        String configured = System.getProperty("nopennlp.corpora.dir");
        if (configured != null && !configured.trim().isEmpty()) {
            return Paths.get(configured);
        }

        Path directory = Paths.get("").toAbsolutePath();
        while (directory != null) {
            Path candidate = directory.resolve("src/java/model-compat/corpora");
            if (Files.isDirectory(candidate)) {
                return candidate;
            }
            // Also accept being run from inside the project folder itself.
            Path local = directory.resolve("corpora");
            if (Files.isDirectory(local) && Files.exists(local.resolve(SENTENCE_CORPUS))) {
                return local;
            }
            directory = directory.getParent();
        }

        throw new IllegalStateException(
            "Could not locate src/java/model-compat/corpora from "
                + Paths.get("").toAbsolutePath()
                + ". Pass -Dnopennlp.corpora.dir=<path> to override.");
    }

    /** The full path of one corpus file. */
    public static File corpus(String name) {
        File file = corporaDirectory().resolve(name).toFile();
        if (!file.isFile()) {
            throw new IllegalStateException("Training corpus not found: " + file.getAbsolutePath());
        }
        return file;
    }
}
