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

    //
    // There is deliberately no [IterationCleanup] clearing the adaptive state,
    // even though NameFinderME carries per-document state that upstream expects
    // a caller to clear at a document boundary.
    //
    // Adding one costs an order of magnitude in apparent time, and none of it is
    // real: an iteration-level hook forces BenchmarkDotNet to InvocationCount=1,
    // UnrollFactor=1, so a single call is timed per iteration and the result is
    // swamped by timer and setup overhead. The first version of this file did
    // that and reported ~390 us/op against a true cost near 35 us/op.
    //
    // Clearing also changes nothing here. Every invocation re-runs the same
    // sentence, so the previous-decision map fills on the first call and is
    // identical from then on; measured directly, clearing per call and never
    // clearing agree to within noise (35.4 vs 35.6 us/op), and the clear itself
    // costs 0.03 us. What is measured either way is the warm case. Reading these
    // numbers as first-document latency would be wrong, but that is true with or
    // without the hook.
    //
    // The JMH counterpart drops its @TearDown for the same reason, so the two
    // harnesses still measure the same thing.
    //

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    public opennlp.tools.util.Span[] JavaFind()
        => javaNameFinder.find(BenchmarkData.EntityTokens);

    [Benchmark(Description = "NOpenNLP")]
    public Span[] Find() => nameFinder.Find(BenchmarkData.EntityTokens);
}
