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
using System.IO;
using BenchmarkDotNet.Attributes;
using NOpenNLP.Tools.Sentdetect;

namespace NOpenNLP.Benchmarks;

/// <summary>
/// Sentence detection over <see cref="BenchmarkData.SampleText"/>, comparing
/// <see cref="SentenceDetectorME"/> against the Java implementation it was
/// ported from.
/// </summary>
[MemoryDiagnoser]
public class SentenceDetectorBenchmarks
{
    private opennlp.tools.sentdetect.SentenceDetectorME javaDetector = null!;
    private SentenceDetectorME detector = null!;

    /// <summary>
    /// Loads both models once. Model loading is measured separately by
    /// <see cref="ModelLoadingBenchmarks"/>; folding it in here would swamp the
    /// inference cost these benchmarks exist to measure.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        string path = BenchmarkData.ModelPath("en-sent.bin");

        javaDetector = new opennlp.tools.sentdetect.SentenceDetectorME(
            new opennlp.tools.sentdetect.SentenceModel(new java.io.File(path)));

        detector = new SentenceDetectorME(new SentenceModel(new FileInfo(path)));
    }

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    public string[] JavaSentDetect() => javaDetector.sentDetect(BenchmarkData.SampleText);

    [Benchmark(Description = "NOpenNLP")]
    public string[] SentDetect() => detector.SentDetect(BenchmarkData.SampleText);
}
