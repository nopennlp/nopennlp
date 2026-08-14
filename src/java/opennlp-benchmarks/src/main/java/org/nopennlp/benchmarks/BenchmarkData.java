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
package org.nopennlp.benchmarks;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.UncheckedIOException;
import java.io.IOException;

/**
 * Locates the pre-trained models and supplies the text every benchmark runs on.
 *
 * <p>Deliberately a line-for-line counterpart of {@code BenchmarkData.cs} in
 * {@code src/NOpenNLP.Benchmarks}: the same models, the same sample text, the
 * same tokens and tags. If the two drift apart the JMH and BenchmarkDotNet
 * numbers stop being comparable, which is the entire point of this project.
 *
 * <p>The models come from {@code build/download-test-models.ps1} at the
 * repository root, which puts them in a gitignored {@code testdata/}.
 * {@code NOPENNLP_TEST_DATA_DIR} overrides the location, matching the variable
 * the .NET tests and benchmarks read.
 */
final class BenchmarkData {

    private static final String DATA_DIR_VARIABLE = "NOPENNLP_TEST_DATA_DIR";

    /**
     * The text the sentence detector runs on, and the source of the sentences
     * every other tool consumes.
     *
     * <p>The opening of the Penn Treebank Wall Street Journal sample, with the
     * abbreviations and the mid-sentence "N.V.," that make sentence detection
     * and tokenization non-trivial.
     */
    static final String SAMPLE_TEXT =
        "Pierre Vinken, 61 years old, will join the board as a nonexecutive director Nov. 29. "
        + "Mr. Vinken is chairman of Elsevier N.V., the Dutch publishing group. "
        + "Rudolph Agnew, 55 years old and former chairman of Consolidated Gold Fields PLC, "
        + "was named a director of this British industrial conglomerate.";

    /**
     * The tokens of the first sentence of {@link #SAMPLE_TEXT}.
     *
     * <p>Stated as a constant rather than produced by the tokenizer so the POS,
     * chunker and name finder benchmarks measure only themselves.
     */
    static final String[] SENTENCE_TOKENS = {
        "Pierre", "Vinken", ",", "61", "years", "old", ",", "will", "join", "the",
        "board", "as", "a", "nonexecutive", "director", "Nov.", "29", "."
    };

    /** The POS tags of {@link #SENTENCE_TOKENS}, which the chunker needs. */
    static final String[] SENTENCE_TAGS = {
        "NNP", "NNP", ",", "CD", "NNS", "JJ", ",", "MD", "VB", "DT", "NN", "IN",
        "DT", "JJ", "NN", "NNP", "CD", "."
    };

    /**
     * A sentence carrying an entity of every type the name finder models
     * recognize, so one input exercises all seven.
     */
    static final String[] ENTITY_TOKENS = {
        "John", "Smith", "paid", "$", "25.5", "million", "to", "Acme", "Corp.",
        "in", "Chicago", "yesterday", "afternoon", ",", "a", "15", "%", "increase", "."
    };

    private BenchmarkData() {
    }

    /**
     * Returns the named model under {@code testdata/models-sf/}.
     *
     * <p>Throws rather than skipping when a model is absent: a benchmark that
     * quietly skipped would report an empty result that reads like a successful
     * run.
     */
    static File modelFile(String name) {
        File directory = resolveDataDirectory();

        if (directory == null) {
            throw new UncheckedIOException(new FileNotFoundException(
                "Benchmark models not found. Run build/download-test-models.ps1, or set "
                + DATA_DIR_VARIABLE + " to the directory holding models-sf/."));
        }

        File model = new File(new File(directory, "models-sf"), name);

        if (!model.isFile()) {
            throw new UncheckedIOException(new FileNotFoundException(
                "Model '" + name + "' not found under " + directory
                + ". Run build/download-test-models.ps1 to fetch the models."));
        }

        return model;
    }

    private static File resolveDataDirectory() {
        String configured = System.getenv(DATA_DIR_VARIABLE);
        if (configured != null && !configured.isEmpty()) {
            File candidate = new File(configured);
            return candidate.isDirectory() ? candidate : null;
        }

        // Walk up from the working directory looking for testdata/, so the jar
        // runs correctly from the repository root or from its own target/
        // directory. JMH forks a JVM per trial but inherits the working
        // directory, so this resolves the same way in the forked process.
        File directory;
        try {
            directory = new File(".").getCanonicalFile();
        } catch (IOException e) {
            directory = new File(".").getAbsoluteFile();
        }

        while (directory != null) {
            File candidate = new File(directory, "testdata");
            if (candidate.isDirectory()) {
                return candidate;
            }

            directory = directory.getParentFile();
        }

        return null;
    }
}
