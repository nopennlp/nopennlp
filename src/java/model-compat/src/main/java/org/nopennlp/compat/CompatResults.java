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

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.Writer;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.LinkedHashMap;
import java.util.Map;

/**
 * The inference output a runtime got from its own models, recorded so the other
 * runtime can compare against it.
 *
 * <p>A plain {@code key=value} text file, one entry per line, UTF-8, with
 * {@code \n} line endings written explicitly so a Windows run and a Linux run
 * produce the same bytes. It is deliberately not a real serialization format:
 * both sides have to parse it with no dependencies beyond their standard
 * library, and a human reading a CI log should be able to see what differed.
 *
 * <p>Values are joined with {@code |} and are always strings. Probabilities are
 * formatted by {@link #probability(double)} to a fixed number of digits, in the
 * invariant culture, because the last bits of a double are not the contract
 * here: two implementations of the same arithmetic can differ in the final ulp
 * without either being wrong, and a test that failed on that would be noise.
 * Six digits is far tighter than any real behavioural difference and far looser
 * than floating-point jitter.
 *
 * <p>The .NET counterpart is {@code CompatResults.cs}. Keep the two in sync.
 */
public final class CompatResults {

    private final Map<String, String> entries = new LinkedHashMap<>();

    /** Separator between the elements of a multi-valued entry. */
    public static final String SEPARATOR = "|";

    /** Records one entry, failing loudly on a duplicate key. */
    public void put(String key, String value) {
        if (entries.put(key, value) != null) {
            throw new IllegalStateException("Duplicate result key: " + key);
        }
    }

    /** Records a multi-valued entry, joined with {@link #SEPARATOR}. */
    public void put(String key, String[] values) {
        put(key, String.join(SEPARATOR, values));
    }

    /** Records a sequence of probabilities. */
    public void put(String key, double[] values) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < values.length; i++) {
            if (i > 0) {
                sb.append(SEPARATOR);
            }
            sb.append(probability(values[i]));
        }
        put(key, sb.toString());
    }

    /** The recorded entries, in insertion order. */
    public Map<String, String> entries() {
        return entries;
    }

    /**
     * Formats a probability to six decimal places in the invariant culture. See
     * the class comment for why the full precision of the double is not the
     * contract.
     */
    public static String probability(double value) {
        return String.format(java.util.Locale.ROOT, "%.6f", value);
    }

    /** Writes the entries to {@code path}, overwriting whatever was there. */
    public void write(Path path) throws IOException {
        Files.createDirectories(path.toAbsolutePath().getParent());
        try (Writer writer = new OutputStreamWriter(
                Files.newOutputStream(path), StandardCharsets.UTF_8)) {
            for (Map.Entry<String, String> entry : entries.entrySet()) {
                // '\n' rather than the platform separator: the file crosses
                // between runtimes and operating systems, so its bytes must not
                // depend on which one wrote it.
                writer.write(entry.getKey() + "=" + entry.getValue() + "\n");
            }
        }
    }

    /** Reads entries previously written by {@link #write(Path)}. */
    public static Map<String, String> read(Path path) throws IOException {
        Map<String, String> result = new LinkedHashMap<>();
        try (BufferedReader reader = new BufferedReader(
                new InputStreamReader(Files.newInputStream(path), StandardCharsets.UTF_8))) {
            String line;
            while ((line = reader.readLine()) != null) {
                if (line.isEmpty()) {
                    continue;
                }
                int split = line.indexOf('=');
                if (split < 0) {
                    throw new IOException("Malformed line in " + path + ": " + line);
                }
                result.put(line.substring(0, split), line.substring(split + 1));
            }
        }
        return result;
    }
}
