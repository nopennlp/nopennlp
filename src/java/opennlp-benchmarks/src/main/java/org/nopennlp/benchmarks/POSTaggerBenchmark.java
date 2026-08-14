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

import opennlp.tools.postag.POSModel;
import opennlp.tools.postag.POSTaggerME;

import org.openjdk.jmh.annotations.Benchmark;
import org.openjdk.jmh.annotations.BenchmarkMode;
import org.openjdk.jmh.annotations.Mode;
import org.openjdk.jmh.annotations.OutputTimeUnit;
import org.openjdk.jmh.annotations.Scope;
import org.openjdk.jmh.annotations.Setup;
import org.openjdk.jmh.annotations.State;

/**
 * POS tagging of {@link BenchmarkData#SENTENCE_TOKENS}.
 *
 * <p>Both the maxent and the perceptron model are covered: they were produced
 * by different trainers and evaluate through different code, so a change that
 * helps one need not help the other.
 *
 * <p>The JVM counterpart of {@code POSTaggerBenchmarks} in the BenchmarkDotNet
 * project.
 */
@State(Scope.Benchmark)
@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.MICROSECONDS)
public class POSTaggerBenchmark {

    private POSTaggerME maxent;
    private POSTaggerME perceptron;

    @Setup
    public void setup() throws IOException {
        maxent = new POSTaggerME(new POSModel(BenchmarkData.modelFile("en-pos-maxent.bin")));
        perceptron =
            new POSTaggerME(new POSModel(BenchmarkData.modelFile("en-pos-perceptron.bin")));
    }

    @Benchmark
    public String[] tagMaxent() {
        return maxent.tag(BenchmarkData.SENTENCE_TOKENS);
    }

    @Benchmark
    public String[] tagPerceptron() {
        return perceptron.tag(BenchmarkData.SENTENCE_TOKENS);
    }
}
