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

import opennlp.tools.chunker.ChunkerME;
import opennlp.tools.chunker.ChunkerModel;

import org.openjdk.jmh.annotations.Benchmark;
import org.openjdk.jmh.annotations.BenchmarkMode;
import org.openjdk.jmh.annotations.Mode;
import org.openjdk.jmh.annotations.OutputTimeUnit;
import org.openjdk.jmh.annotations.Scope;
import org.openjdk.jmh.annotations.Setup;
import org.openjdk.jmh.annotations.State;

/**
 * Shallow parsing of {@link BenchmarkData#SENTENCE_TOKENS} with the tags in
 * {@link BenchmarkData#SENTENCE_TAGS}.
 *
 * <p>The JVM counterpart of {@code ChunkerBenchmarks} in the BenchmarkDotNet
 * project.
 */
@State(Scope.Benchmark)
@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.MICROSECONDS)
public class ChunkerBenchmark {

    private ChunkerME chunker;

    @Setup
    public void setup() throws IOException {
        chunker = new ChunkerME(new ChunkerModel(BenchmarkData.modelFile("en-chunker.bin")));
    }

    @Benchmark
    public String[] chunk() {
        return chunker.chunk(BenchmarkData.SENTENCE_TOKENS, BenchmarkData.SENTENCE_TAGS);
    }
}
