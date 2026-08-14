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
using NOpenNLP.Tools.Chunker;

namespace NOpenNLP.Benchmarks;

/// <summary>
/// Shallow parsing of <see cref="BenchmarkData.SentenceTokens"/> with the tags
/// in <see cref="BenchmarkData.SentenceTags"/>.
/// </summary>
[MemoryDiagnoser]
public class ChunkerBenchmarks
{
    private opennlp.tools.chunker.ChunkerME javaChunker = null!;
    private ChunkerME chunker = null!;

    [GlobalSetup]
    public void Setup()
    {
        string path = BenchmarkData.ModelPath("en-chunker.bin");

        javaChunker = new opennlp.tools.chunker.ChunkerME(
            new opennlp.tools.chunker.ChunkerModel(new java.io.File(path)));

        chunker = new ChunkerME(new ChunkerModel(new FileInfo(path)));
    }

    [Benchmark(Baseline = true, Description = "Java (IKVM)")]
    public string[] JavaChunk()
        => javaChunker.chunk(BenchmarkData.SentenceTokens, BenchmarkData.SentenceTags);

    [Benchmark(Description = "NOpenNLP")]
    public string[] Chunk()
        => chunker.Chunk(BenchmarkData.SentenceTokens, BenchmarkData.SentenceTags);
}
