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
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Tokenize;

namespace NOpenNLP.Benchmarks;

/// <summary>
/// Constructing each model from its <c>.bin</c> file.
/// </summary>
/// <remarks>
/// A different code path from inference, and a cost every process pays at least
/// once: unzipping the model container, parsing the manifest, and reading the
/// parameters through the model readers. For a short-lived process or a
/// serverless invocation it dominates, so it is worth tracking on its own rather
/// than hiding it in a <c>[GlobalSetup]</c>.
/// <para/>
/// These read from the filesystem, so the numbers include I/O. In practice the
/// files are in the OS page cache after the first iteration, and both sides pay
/// the same cost, so the comparison stays fair even though the absolute figures
/// are not pure parsing.
/// </remarks>
// One category per model, so each is compared against the same model loaded by
// Java rather than against whichever single benchmark carried Baseline = true.
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ModelLoadingBenchmarks
{
    private string sentencePath = null!;
    private string tokenizerPath = null!;
    private string posMaxentPath = null!;
    private string posPerceptronPath = null!;
    private string chunkerPath = null!;
    private string nameFinderPath = null!;

    [GlobalSetup]
    public void Setup()
    {
        sentencePath = BenchmarkData.ModelPath("en-sent.bin");
        tokenizerPath = BenchmarkData.ModelPath("en-token.bin");
        posMaxentPath = BenchmarkData.ModelPath("en-pos-maxent.bin");
        posPerceptronPath = BenchmarkData.ModelPath("en-pos-perceptron.bin");
        chunkerPath = BenchmarkData.ModelPath("en-chunker.bin");
        nameFinderPath = BenchmarkData.ModelPath("en-ner-person.bin");
    }

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("SentenceModel")]
    public object JavaLoadSentenceModel()
        => new opennlp.tools.sentdetect.SentenceModel(new java.io.File(sentencePath));

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("SentenceModel")]
    public object LoadSentenceModel() => new SentenceModel(new FileInfo(sentencePath));

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("TokenizerModel")]
    public object JavaLoadTokenizerModel()
        => new opennlp.tools.tokenize.TokenizerModel(new java.io.File(tokenizerPath));

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("TokenizerModel")]
    public object LoadTokenizerModel() => new TokenizerModel(new FileInfo(tokenizerPath));

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("POSModel maxent")]
    public object JavaLoadPosMaxentModel()
        => new opennlp.tools.postag.POSModel(new java.io.File(posMaxentPath));

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("POSModel maxent")]
    public object LoadPosMaxentModel() => new POSModel(new FileInfo(posMaxentPath));

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("POSModel perceptron")]
    public object JavaLoadPosPerceptronModel()
        => new opennlp.tools.postag.POSModel(new java.io.File(posPerceptronPath));

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("POSModel perceptron")]
    public object LoadPosPerceptronModel() => new POSModel(new FileInfo(posPerceptronPath));

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("ChunkerModel")]
    public object JavaLoadChunkerModel()
        => new opennlp.tools.chunker.ChunkerModel(new java.io.File(chunkerPath));

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("ChunkerModel")]
    public object LoadChunkerModel() => new ChunkerModel(new FileInfo(chunkerPath));

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("TokenNameFinderModel")]
    public object JavaLoadNameFinderModel()
        => new opennlp.tools.namefind.TokenNameFinderModel(new java.io.File(nameFinderPath));

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("TokenNameFinderModel")]
    public object LoadNameFinderModel() => new TokenNameFinderModel(new FileInfo(nameFinderPath));
}
