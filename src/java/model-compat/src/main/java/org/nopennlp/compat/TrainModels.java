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

import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Trains the harness models with Apache OpenNLP and records what they produce,
 * for NOpenNLP to read back. This is the Java half of the "Java writes, .NET
 * reads" direction of issue #46.
 *
 * <p>Usage, from the {@code src/java/model-compat} directory:
 * <pre>
 *   ./mvnw -q compile exec:java
 *   ./mvnw -q compile exec:java -Dexec.args="/path/to/output"
 * </pre>
 *
 * <p>The output goes to {@code work/java} by default: the five model files, plus
 * a {@code results.txt} holding the inference output Java got from them. The
 * .NET side asserts against that file, so a disagreement reads as a diff of two
 * concrete outputs rather than a failure against a constant nobody can check.
 */
public final class TrainModels {

    private TrainModels() {
    }

    public static void main(String[] args) throws Exception {
        Path outputDirectory = (args.length > 0 && args[0] != null && !args[0].isEmpty())
            ? Paths.get(args[0])
            : Paths.get("work", "java");
        Files.createDirectories(outputDirectory);

        System.out.println("Training Apache OpenNLP models into " + outputDirectory.toAbsolutePath());
        CompatModels.trainAll(outputDirectory);

        System.out.println("Recording what Apache OpenNLP got from them");
        CompatResults results = CompatModels.analyze(outputDirectory);
        Path resultsFile = outputDirectory.resolve(CompatCorpus.RESULTS_FILE);
        results.write(resultsFile);

        System.out.println("  " + CompatCorpus.RESULTS_FILE + " (" + results.entries().size() + " entries)");
        System.out.println("Done: " + outputDirectory.toAbsolutePath());
    }
}
