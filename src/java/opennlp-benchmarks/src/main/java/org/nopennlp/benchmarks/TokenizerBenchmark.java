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

import opennlp.tools.tokenize.SimpleTokenizer;
import opennlp.tools.tokenize.TokenizerME;
import opennlp.tools.tokenize.TokenizerModel;
import opennlp.tools.tokenize.WhitespaceTokenizer;

import org.openjdk.jmh.annotations.Benchmark;
import org.openjdk.jmh.annotations.BenchmarkMode;
import org.openjdk.jmh.annotations.Mode;
import org.openjdk.jmh.annotations.OutputTimeUnit;
import org.openjdk.jmh.annotations.Scope;
import org.openjdk.jmh.annotations.Setup;
import org.openjdk.jmh.annotations.State;

/**
 * The three tokenizers, over {@link BenchmarkData#SAMPLE_TEXT}.
 *
 * <p>The JVM counterpart of {@code TokenizerBenchmarks} in the BenchmarkDotNet
 * project.
 */
@State(Scope.Benchmark)
@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.MICROSECONDS)
public class TokenizerBenchmark {

    private TokenizerME tokenizer;

    @Setup
    public void setup() throws IOException {
        tokenizer = new TokenizerME(
            new TokenizerModel(BenchmarkData.modelFile("en-token.bin")));
    }

    @Benchmark
    public String[] tokenizerME() {
        return tokenizer.tokenize(BenchmarkData.SAMPLE_TEXT);
    }

    @Benchmark
    public String[] simpleTokenizer() {
        return SimpleTokenizer.INSTANCE.tokenize(BenchmarkData.SAMPLE_TEXT);
    }

    @Benchmark
    public String[] whitespaceTokenizer() {
        return WhitespaceTokenizer.INSTANCE.tokenize(BenchmarkData.SAMPLE_TEXT);
    }
}
