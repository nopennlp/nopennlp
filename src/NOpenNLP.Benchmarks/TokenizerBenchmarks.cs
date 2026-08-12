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
using NOpenNLP.Tools.Tokenize;

namespace NOpenNLP.Benchmarks;

/// <summary>
/// The three tokenizers, over <see cref="BenchmarkData.SampleText"/>.
/// </summary>
/// <remarks>
/// <see cref="TokenizerME"/> is the statistical one and the interesting
/// comparison. The whitespace and simple tokenizers are rule-based and cheap,
/// but they are what most callers reach for first, and they exercise the
/// character classification helpers rather than the maxent stack.
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class TokenizerBenchmarks
{
    private opennlp.tools.tokenize.TokenizerME javaTokenizer = null!;
    private TokenizerME tokenizer = null!;

    [GlobalSetup]
    public void Setup()
    {
        string path = BenchmarkData.ModelPath("en-token.bin");

        javaTokenizer = new opennlp.tools.tokenize.TokenizerME(
            new opennlp.tools.tokenize.TokenizerModel(new java.io.File(path)));

        tokenizer = new TokenizerME(new TokenizerModel(new FileInfo(path)));
    }

    //
    // Each tokenizer is its own category with its own baseline. Without the
    // categories BenchmarkDotNet takes the single baseline in the class as the
    // denominator for every row, so the rule-based tokenizers would be reported
    // as a ratio against the statistical one — a comparison between two
    // different algorithms that answers nothing.
    //

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("TokenizerME")]
    public string[] JavaTokenizerME() => javaTokenizer.tokenize(BenchmarkData.SampleText);

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("TokenizerME")]
    public string[] TokenizerME() => tokenizer.Tokenize(BenchmarkData.SampleText);

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("SimpleTokenizer")]
    public string[] JavaSimpleTokenizer()
        => opennlp.tools.tokenize.SimpleTokenizer.INSTANCE.tokenize(BenchmarkData.SampleText);

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("SimpleTokenizer")]
    public string[] SimpleTokenizer()
        => Tools.Tokenize.SimpleTokenizer.INSTANCE.Tokenize(BenchmarkData.SampleText);

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    [BenchmarkCategory("WhitespaceTokenizer")]
    public string[] JavaWhitespaceTokenizer()
        => opennlp.tools.tokenize.WhitespaceTokenizer.INSTANCE.tokenize(BenchmarkData.SampleText);

    [Benchmark(Description = "NOpenNLP")]
    [BenchmarkCategory("WhitespaceTokenizer")]
    public string[] WhitespaceTokenizer()
        => Tools.Tokenize.WhitespaceTokenizer.INSTANCE.Tokenize(BenchmarkData.SampleText);
}
