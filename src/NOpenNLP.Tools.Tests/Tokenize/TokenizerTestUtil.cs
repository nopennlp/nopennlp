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
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// Utility class for testing the <see cref="ITokenizer"/>.
/// </summary>
public class TokenizerTestUtil
{
    internal static TokenizerModel CreateSimpleMaxentTokenModel()
    {
        JCG.List<TokenSample> samples = [];

        samples.Add(new TokenSample("year", [new Span(0, 4)]));
        samples.Add(new TokenSample("year,", [
            new Span(0, 4),
            new Span(4, 5)]));
        samples.Add(new TokenSample("it,", [
            new Span(0, 2),
            new Span(2, 3)]));
        samples.Add(new TokenSample("it", [
            new Span(0, 2)]));
        samples.Add(new TokenSample("yes", [
            new Span(0, 3)]));
        samples.Add(new TokenSample("yes,", [
            new Span(0, 3),
            new Span(3, 4)]));

        var mlParams = new TrainingParameters();
        mlParams.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        mlParams.Put(TrainingParameters.CUTOFF_PARAM, 0);

        return TokenizerME.Train(new CollectionObjectStream<TokenSample>(samples),
            TokenizerFactory.Create(null, "eng", null, true, null!)!, mlParams);
    }

    internal static TokenizerModel CreateMaxentTokenModel()
    {
        IInputStreamFactory trainDataIn = new ResourceAsStreamFactory(
            "/opennlp/tools/tokenize/token.train");

        IObjectStream<TokenSample?> samples = new TokenSampleStream(
            new PlainTextByLineStream(trainDataIn, Encoding.UTF8));

        var mlParams = new TrainingParameters();
        mlParams.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        mlParams.Put(TrainingParameters.CUTOFF_PARAM, 0);

        return TokenizerME.Train(samples, TokenizerFactory.Create(null, "eng", null, true, null!)!, mlParams);
    }
}
