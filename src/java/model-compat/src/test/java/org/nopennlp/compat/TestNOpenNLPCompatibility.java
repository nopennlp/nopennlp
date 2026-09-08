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

import org.junit.Test;

import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertTrue;
import static org.junit.Assert.fail;

/**
 * Verifies that Apache OpenNLP 1.9.5 can load models trained and serialized by
 * NOpenNLP, and that running inference on them produces the same output NOpenNLP
 * got. This is the ".NET writes, Java reads" direction of issue #46.
 *
 * <p>The model directory is supplied with {@code -Dnopennlp.model.dir=...}. Per
 * the harness contract this test <b>fails</b> rather than skipping when the
 * directory or its models are missing: when Java is asked to read NOpenNLP's
 * models, their absence means the pipeline that was supposed to produce them is
 * broken. (The .NET side takes the opposite stance for the other direction,
 * because a JDK may legitimately not be installed.)
 */
public class TestNOpenNLPCompatibility {

    /** Every model file the harness expects to find. */
    private static final String[] MODELS = {
        CompatCorpus.SENTENCE_MODEL,
        CompatCorpus.TOKENIZER_MODEL,
        CompatCorpus.NAME_FINDER_MODEL,
        CompatCorpus.POS_TAGGER_MODEL,
        CompatCorpus.CHUNKER_MODEL,
    };

    private static Path modelDirectory() {
        String path = System.getProperty("nopennlp.model.dir");
        if (path == null || path.trim().isEmpty()) {
            fail("System property 'nopennlp.model.dir' is not set. Have NOpenNLP write "
                + "its models first and pass -Dnopennlp.model.dir=<path>. "
                + "See src/java/model-compat/README.md.");
        }

        Path directory = Paths.get(path);
        if (!Files.isDirectory(directory)) {
            fail("No NOpenNLP model directory at '" + directory.toAbsolutePath() + "'. "
                + "Have NOpenNLP write its models before running this test.");
        }

        for (String model : MODELS) {
            if (!Files.isRegularFile(directory.resolve(model))) {
                fail("NOpenNLP model '" + model + "' is missing from "
                    + directory.toAbsolutePath() + ". The .NET writer did not complete.");
            }
        }

        return directory;
    }

    /**
     * Apache OpenNLP opens each NOpenNLP-written model, runs the shared inputs
     * through it, and gets the same answers NOpenNLP recorded.
     *
     * <p>Loading is not asserted separately: {@link CompatModels#analyze} opens
     * every model before it can run anything, so a model Java cannot read fails
     * here with the reader's own exception, which says more than an assertion
     * would.
     */
    @Test
    public void nopennlpModelsBehaveIdenticallyInJava() throws Exception {
        Path directory = modelDirectory();

        Path recorded = directory.resolve(CompatCorpus.RESULTS_FILE);
        if (!Files.isRegularFile(recorded)) {
            fail("NOpenNLP did not record its own inference output at "
                + recorded.toAbsolutePath() + ", so there is nothing to compare against.");
        }

        Map<String, String> expected = CompatResults.read(recorded);
        Map<String, String> actual = CompatModels.analyze(directory).entries();

        assertSameResults(expected, actual);
    }

    /**
     * Compares the two result sets key by key, reporting every difference at
     * once. A single {@code assertEquals} on the whole map would print two
     * hundred-line strings and leave the reader to spot the difference.
     */
    static void assertSameResults(Map<String, String> expected, Map<String, String> actual) {
        Set<String> keys = new LinkedHashSet<>(expected.keySet());
        keys.addAll(actual.keySet());

        List<String> differences = new ArrayList<>();
        for (String key : keys) {
            String want = expected.get(key);
            String got = actual.get(key);
            if (want == null) {
                differences.add(key + ": only Java produced a value (" + got + ")");
            } else if (got == null) {
                differences.add(key + ": only NOpenNLP produced a value (" + want + ")");
            } else if (!want.equals(got)) {
                differences.add(key + ":\n    NOpenNLP: " + want + "\n    Java:     " + got);
            }
        }

        if (!differences.isEmpty()) {
            fail("Apache OpenNLP and NOpenNLP disagree on "
                + differences.size() + " of " + keys.size() + " results:\n  "
                + String.join("\n  ", differences));
        }

        // A harness that compared nothing would pass just as quietly.
        assertTrue("No results were compared", keys.size() > 0);
        assertEquals("Result key sets should match exactly",
            expected.keySet().size(), keys.size());
    }
}
