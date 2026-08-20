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

import java.util.concurrent.TimeUnit;

import opennlp.tools.stemmer.PorterStemmer;
import opennlp.tools.stemmer.snowball.SnowballStemmer;

import org.openjdk.jmh.annotations.Benchmark;
import org.openjdk.jmh.annotations.BenchmarkMode;
import org.openjdk.jmh.annotations.Mode;
import org.openjdk.jmh.annotations.OutputTimeUnit;
import org.openjdk.jmh.annotations.Scope;
import org.openjdk.jmh.annotations.Setup;
import org.openjdk.jmh.annotations.State;

/**
 * Stemming every token of {@link BenchmarkData#SENTENCE_TOKENS}, with the
 * hand-written Porter stemmer and with the generated English Snowball stemmer.
 *
 * <p>The JVM counterpart of {@code StemmerBenchmarks} in the BenchmarkDotNet
 * project. Neither stemmer loads a model, so unlike the other benchmarks here
 * these run without {@code testdata/models-sf/}.
 */
@State(Scope.Benchmark)
@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.MICROSECONDS)
public class StemmerBenchmark {

    private PorterStemmer porterStemmer;
    private SnowballStemmer snowballStemmer;

    @Setup
    public void setup() {
        porterStemmer = new PorterStemmer();
        snowballStemmer = new SnowballStemmer(SnowballStemmer.ALGORITHM.ENGLISH);
    }

    @Benchmark
    public String[] porterStem() {
        String[] stems = new String[BenchmarkData.SENTENCE_TOKENS.length];
        for (int i = 0; i < stems.length; i++) {
            stems[i] = porterStemmer.stem(BenchmarkData.SENTENCE_TOKENS[i]).toString();
        }

        return stems;
    }

    @Benchmark
    public String[] snowballStem() {
        String[] stems = new String[BenchmarkData.SENTENCE_TOKENS.length];
        for (int i = 0; i < stems.length; i++) {
            stems[i] = snowballStemmer.stem(BenchmarkData.SENTENCE_TOKENS[i]).toString();
        }

        return stems;
    }
}
