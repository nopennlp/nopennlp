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
import java.io.IOException;
import java.util.concurrent.TimeUnit;

import opennlp.tools.chunker.ChunkerModel;
import opennlp.tools.namefind.TokenNameFinderModel;
import opennlp.tools.postag.POSModel;
import opennlp.tools.sentdetect.SentenceModel;
import opennlp.tools.tokenize.TokenizerModel;

import org.openjdk.jmh.annotations.Benchmark;
import org.openjdk.jmh.annotations.BenchmarkMode;
import org.openjdk.jmh.annotations.Mode;
import org.openjdk.jmh.annotations.OutputTimeUnit;
import org.openjdk.jmh.annotations.Scope;
import org.openjdk.jmh.annotations.Setup;
import org.openjdk.jmh.annotations.State;

/**
 * Constructing each model from its {@code .bin} file.
 *
 * <p>A different code path from inference, and a cost every process pays at
 * least once: unzipping the model container, parsing the manifest, and reading
 * the parameters through the model readers.
 *
 * <p>Reported in milliseconds rather than the microseconds the inference
 * benchmarks use, because loading is three orders of magnitude slower and
 * microseconds would print as unreadably large numbers.
 *
 * <p>The JVM counterpart of {@code ModelLoadingBenchmarks} in the
 * BenchmarkDotNet project.
 */
@State(Scope.Benchmark)
@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.MILLISECONDS)
public class ModelLoadingBenchmark {

    private File sentence;
    private File tokenizer;
    private File posMaxent;
    private File posPerceptron;
    private File chunker;
    private File nameFinder;

    @Setup
    public void setup() {
        sentence = BenchmarkData.modelFile("en-sent.bin");
        tokenizer = BenchmarkData.modelFile("en-token.bin");
        posMaxent = BenchmarkData.modelFile("en-pos-maxent.bin");
        posPerceptron = BenchmarkData.modelFile("en-pos-perceptron.bin");
        chunker = BenchmarkData.modelFile("en-chunker.bin");
        nameFinder = BenchmarkData.modelFile("en-ner-person.bin");
    }

    @Benchmark
    public Object loadSentenceModel() throws IOException {
        return new SentenceModel(sentence);
    }

    @Benchmark
    public Object loadTokenizerModel() throws IOException {
        return new TokenizerModel(tokenizer);
    }

    @Benchmark
    public Object loadPosMaxentModel() throws IOException {
        return new POSModel(posMaxent);
    }

    @Benchmark
    public Object loadPosPerceptronModel() throws IOException {
        return new POSModel(posPerceptron);
    }

    @Benchmark
    public Object loadChunkerModel() throws IOException {
        return new ChunkerModel(chunker);
    }

    @Benchmark
    public Object loadNameFinderModel() throws IOException {
        return new TokenNameFinderModel(nameFinder);
    }
}
