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
import org.openjdk.jmh.annotations.Level;
import org.openjdk.jmh.annotations.Mode;
import org.openjdk.jmh.annotations.OutputTimeUnit;
import org.openjdk.jmh.annotations.Param;
import org.openjdk.jmh.annotations.Scope;
import org.openjdk.jmh.annotations.Setup;
import org.openjdk.jmh.annotations.State;
import org.openjdk.jmh.annotations.TearDown;

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

    /**
     * Resets the adaptive feature state between iterations.
     *
     * <p>{@code NameFinderME} carries state across calls that upstream expects
     * a caller to clear at each document boundary — chiefly the previous-decision
     * map, which makes a feature fire on the second and later sightings of a
     * token that did not fire on the first.
     *
     * <p>Because every invocation here re-runs the same sentence, that map fills
     * on the first call and then stays fixed, so what is measured is the
     * warm-cache case, not a cold document. That is deliberate: it is the steady
     * state, and it is stable.
     *
     * <p>Clearing per iteration matches the {@code [IterationCleanup]} in the
     * BenchmarkDotNet counterpart, so both harnesses measure the same thing. If
     * these numbers are ever used to reason about first-document latency, that
     * is the wrong benchmark to read.
     */
    @TearDown(Level.Iteration)
    public void clearAdaptiveData() {
        nameFinder.clearAdaptiveData();
    }

    @Benchmark
    public Span[] find() {
        return nameFinder.find(BenchmarkData.ENTITY_TOKENS);
    }
}
