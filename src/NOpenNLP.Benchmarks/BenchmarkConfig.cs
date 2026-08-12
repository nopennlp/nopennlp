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
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;

namespace NOpenNLP.Benchmarks;

/// <summary>
/// The configuration every benchmark in this assembly runs under.
/// </summary>
internal sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddLogger(ConsoleLogger.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);

        // Allocation matters as much as time here. The Java code allocates
        // freely and IKVM turns that into managed allocation, so a port that is
        // no faster but allocates far less is still a real improvement, and one
        // that quietly allocates more is a regression worth seeing.
        AddDiagnoser(MemoryDiagnoser.Default);

        AddExporter(MarkdownExporter.GitHub);
        AddExporter(HtmlExporter.Default);

        // The IKVM baseline is often an order of magnitude away from the port.
        // Trend style renders that as "12x slower" rather than a bare ratio,
        // which is the thing a reader of these tables actually wants to know.
        WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));

        // Keeps each Java/NOpenNLP pair adjacent and the baseline first, rather
        // than sorting the whole table by elapsed time and scattering the two
        // halves of every comparison.
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Declared));
    }
}
