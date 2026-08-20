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
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using NOpenNLP.Tools.Stemmer.Snowball;
// Both namespaces declare a PorterStemmer: the hand-written one OpenNLP ships
// under stemmer/, and the generated Snowball algorithm of the same name. Upstream
// has the same collision and resolves it by package; alias the outer one here.
using PorterStemmer = NOpenNLP.Tools.Stemmer.PorterStemmer;

namespace NOpenNLP.Benchmarks;

/// <summary>
/// Stemming every token of <see cref="BenchmarkData.SentenceTokens"/>, with the
/// hand-written Porter stemmer and with the generated English Snowball stemmer.
/// </summary>
/// <remarks>
/// The two algorithms are separate categories with their own baselines: they are
/// different code paths - Porter is ported by hand, the Snowball one is generated
/// - and a single baseline would have BenchmarkDotNet report one as a ratio of
/// the other, which means nothing.
/// <para/>
/// Neither stemmer loads a model, so unlike the other benchmarks here these run
/// without <c>testdata/models-sf/</c>.
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class StemmerBenchmarks
{
    private const string PorterCategory = "Porter";
    private const string SnowballCategory = "Snowball (English)";

    private opennlp.tools.stemmer.PorterStemmer javaPorterStemmer = null!;
    private PorterStemmer porterStemmer = null!;

    private opennlp.tools.stemmer.snowball.SnowballStemmer javaSnowballStemmer = null!;
    private SnowballStemmer snowballStemmer = null!;

    [GlobalSetup]
    public void Setup()
    {
        javaPorterStemmer = new opennlp.tools.stemmer.PorterStemmer();
        porterStemmer = new PorterStemmer();

        javaSnowballStemmer = new opennlp.tools.stemmer.snowball.SnowballStemmer(
            opennlp.tools.stemmer.snowball.SnowballStemmer.ALGORITHM.ENGLISH);
        snowballStemmer = new SnowballStemmer(ALGORITHM.ENGLISH);
    }

    [BenchmarkCategory(PorterCategory)]
    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    public string[] JavaPorterStem()
    {
        string[] stems = new string[BenchmarkData.SentenceTokens.Length];
        for (int i = 0; i < stems.Length; i++)
        {
            // IKVM surfaces this overload's CharSequence as string already, so
            // unlike the Snowball one below it needs no toString().
            stems[i] = javaPorterStemmer.stem(BenchmarkData.SentenceTokens[i]);
        }

        return stems;
    }

    [BenchmarkCategory(PorterCategory)]
    [Benchmark(Description = "NOpenNLP")]
    public string[] PorterStem()
    {
        string[] stems = new string[BenchmarkData.SentenceTokens.Length];
        for (int i = 0; i < stems.Length; i++)
        {
            stems[i] = porterStemmer.Stem(BenchmarkData.SentenceTokens[i]);
        }

        return stems;
    }

    [BenchmarkCategory(SnowballCategory)]
    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    public string[] JavaSnowballStem()
    {
        string[] stems = new string[BenchmarkData.SentenceTokens.Length];
        for (int i = 0; i < stems.Length; i++)
        {
            stems[i] = javaSnowballStemmer.stem(BenchmarkData.SentenceTokens[i]).toString();
        }

        return stems;
    }

    [BenchmarkCategory(SnowballCategory)]
    [Benchmark(Description = "NOpenNLP")]
    public string[] SnowballStem()
    {
        string[] stems = new string[BenchmarkData.SentenceTokens.Length];
        for (int i = 0; i < stems.Length; i++)
        {
            stems[i] = snowballStemmer.Stem(BenchmarkData.SentenceTokens[i]);
        }

        return stems;
    }
}
