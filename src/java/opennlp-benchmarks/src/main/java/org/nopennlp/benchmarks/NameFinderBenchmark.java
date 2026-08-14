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

import java.io.IOException;
import java.util.concurrent.TimeUnit;

import opennlp.tools.namefind.NameFinderME;
import opennlp.tools.namefind.TokenNameFinderModel;
import opennlp.tools.util.Span;

import org.openjdk.jmh.annotations.Benchmark;
import org.openjdk.jmh.annotations.BenchmarkMode;
import org.openjdk.jmh.annotations.Mode;
import org.openjdk.jmh.annotations.OutputTimeUnit;
import org.openjdk.jmh.annotations.Param;
import org.openjdk.jmh.annotations.Scope;
import org.openjdk.jmh.annotations.Setup;
import org.openjdk.jmh.annotations.State;

/**
 * Named entity recognition over {@link BenchmarkData#ENTITY_TOKENS}.
 *
 * <p>Parameterised over the entity type so every one of the seven models is
 * measured; they differ in size and in how many features fire on this sentence.
 *
 * <p>The JVM counterpart of {@code NameFinderBenchmarks} in the
 * BenchmarkDotNet project.
 */
@State(Scope.Benchmark)
@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.MICROSECONDS)
public class NameFinderBenchmark {

    /** Selects the {@code en-ner-{type}.bin} model. */
    @Param({"person", "organization", "location", "date", "money", "percentage", "time"})
    public String type;

    private NameFinderME nameFinder;

    @Setup
    public void setup() throws IOException {
        nameFinder = new NameFinderME(
            new TokenNameFinderModel(BenchmarkData.modelFile("en-ner-" + type + ".bin")));
    }

    //
    // There is deliberately no @TearDown clearing the adaptive state, even
    // though NameFinderME carries per-document state that upstream expects a
    // caller to clear at a document boundary.
    //
    // Every invocation re-runs the same sentence, so the previous-decision map
    // fills on the first call and is identical from then on; clearing per call
    // and never clearing agree to within noise. What is measured either way is
    // the warm case, so reading these numbers as first-document latency would be
    // wrong.
    //
    // JMH would tolerate the hook — it is the BenchmarkDotNet counterpart where
    // an iteration-level hook is actively harmful, because it forces
    // InvocationCount=1 and inflates the result by an order of magnitude. The
    // hook is dropped on both sides so the two harnesses measure the same thing.
    //

    @Benchmark
    public Span[] find() {
        return nameFinder.find(BenchmarkData.ENTITY_TOKENS);
    }
}
