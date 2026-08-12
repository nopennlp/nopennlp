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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Benchmarks;

/// <summary>
/// Named entity recognition over <see cref="BenchmarkData.EntityTokens"/>.
/// </summary>
/// <remarks>
/// Parameterised over the entity type so every one of the seven models is
/// measured. They differ in size and in how many features fire on this
/// sentence, so a single model would not be representative.
/// </remarks>
[MemoryDiagnoser]
public class NameFinderBenchmarks
{
    private opennlp.tools.namefind.NameFinderME javaNameFinder = null!;
    private NameFinderME nameFinder = null!;

    /// <summary>
    /// The entity type, which selects the <c>en-ner-{Type}.bin</c> model.
    /// </summary>
    [Params("person", "organization", "location", "date", "money", "percentage", "time")]
    public string Type { get; set; } = "person";

    [GlobalSetup]
    public void Setup()
    {
        string path = BenchmarkData.ModelPath($"en-ner-{Type}.bin");

        javaNameFinder = new opennlp.tools.namefind.NameFinderME(
            new opennlp.tools.namefind.TokenNameFinderModel(new java.io.File(path)));

        nameFinder = new NameFinderME(new TokenNameFinderModel(new FileInfo(path)));
    }

    /// <summary>
    /// Resets the adaptive feature state between iterations.
    /// </summary>
    /// <remarks>
    /// <c>NameFinderME</c> carries state across calls that upstream expects a
    /// caller to clear at each document boundary — chiefly the previous-decision
    /// map, which makes a feature fire on the second and later sightings of a
    /// token that did not fire on the first.
    /// <para/>
    /// Because every invocation here re-runs the same sentence, that map fills
    /// on the first call and then stays fixed, so what is measured is the
    /// warm-cache case, not a cold document. That is a deliberate choice: it is
    /// the steady state, and it is stable, whereas measuring the cold case would
    /// need a clear before every single invocation, which BenchmarkDotNet cannot
    /// do without also timing the clear.
    /// <para/>
    /// Clearing per iteration rather than never keeps the two harnesses aligned:
    /// the JMH counterpart does the same at <c>Level.Iteration</c>, so both
    /// measure the same thing. If these numbers are ever used to reason about
    /// first-document latency, that is the wrong benchmark to read.
    /// </remarks>
    [IterationCleanup]
    public void ClearAdaptiveData()
    {
        javaNameFinder.clearAdaptiveData();
        nameFinder.ClearAdaptiveData();
    }

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    public opennlp.tools.util.Span[] JavaFind()
        => javaNameFinder.find(BenchmarkData.EntityTokens);

    [Benchmark(Description = "NOpenNLP")]
    public Span[] Find() => nameFinder.Find(BenchmarkData.EntityTokens);
}
