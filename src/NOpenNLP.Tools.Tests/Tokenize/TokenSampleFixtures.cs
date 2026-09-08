/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// This file has been modified from the original Apache OpenNLP source:
// translated from Java to C# and adapted for .NET. See NOTICE.


using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// The sample factories <c>TokenSampleTest</c> declares upstream, split out so both
/// test projects can compile them.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream declares these as static methods on TokenSampleTest itself, and
/// the tokenizer evaluator tests call them from there. Those evaluator tests need
/// types from the cmdline package, which this port ships in NOpenNLP.Cli, so they live
/// in NOpenNLP.Cli.Tests -- a project that does not reference NOpenNLP.Tools.Tests.
/// Linking TokenSampleTest.cs wholesale would re-run its three [Test] methods in the
/// CLI test assembly, so only these factories are split out and linked. TokenSampleTest
/// keeps calling them exactly as upstream does.
/// </remarks>
public static class TokenSampleFixtures
{
    public static TokenSample CreateGoldSample() =>
        new TokenSample("A test.", [new Span(0, 1), new Span(2, 6)]);

    public static TokenSample CreatePredSample() =>
        new TokenSample("A test.", [new Span(0, 3), new Span(2, 6)]);

    public static TokenSample CreatePredSilverSample() =>
        new TokenSample("A t st.", [new Span(0, 1), new Span(2, 6)]);
}
