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
using BenchmarkDotNet.Configs;
using NOpenNLP.Tools.Postag;

namespace NOpenNLP.Benchmarks;

/// <summary>
/// POS tagging of <see cref="BenchmarkData.SentenceTokens"/>.
/// </summary>
/// <remarks>
/// Both the maxent and the perceptron model are covered: they were produced by
/// different trainers and evaluate through different code, so a change that
/// helps one need not help the other. Both run through the same beam search.
/// </remarks>
// Each model is its own category with its own baseline, so maxent is compared
// to Java maxent and perceptron to Java perceptron, rather than every row being
// divided by whichever single benchmark carried Baseline = true.
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class POSTaggerBenchmarks
{
    private opennlp.tools.postag.POSTaggerME javaMaxent = null!;
    private POSTaggerME maxent = null!;
    private opennlp.tools.postag.POSTaggerME javaPerceptron = null!;
    private POSTaggerME perceptron = null!;

    [GlobalSetup]
    public void Setup()
    {
        string maxentPath = BenchmarkData.ModelPath("en-pos-maxent.bin");
        string perceptronPath = BenchmarkData.ModelPath("en-pos-perceptron.bin");

        javaMaxent = new opennlp.tools.postag.POSTaggerME(
            new opennlp.tools.postag.POSModel(new java.io.File(maxentPath)));
        maxent = new POSTaggerME(new POSModel(new FileInfo(maxentPath)));

        javaPerceptron = new opennlp.tools.postag.POSTaggerME(
            new opennlp.tools.postag.POSModel(new java.io.File(perceptronPath)));
        perceptron = new POSTaggerME(new POSModel(new FileInfo(perceptronPath)));
    }

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("Maxent")]
    public string[] JavaTagMaxent() => javaMaxent.tag(BenchmarkData.SentenceTokens);

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("Maxent")]
    public string[] TagMaxent() => maxent.Tag(BenchmarkData.SentenceTokens);

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("Perceptron")]
    public string[] JavaTagPerceptron() => javaPerceptron.tag(BenchmarkData.SentenceTokens);

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("Perceptron")]
    public string[] TagPerceptron() => perceptron.Tag(BenchmarkData.SentenceTokens);
}
