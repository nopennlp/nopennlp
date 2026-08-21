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

using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Brat;

/// <summary>
/// Generates Name Sample objects for a Brat Document object.
/// </summary>
public class BratNameSampleStream : SegmenterObjectStream<BratDocument, NameSample>
{
    private readonly BratDocumentParser parser;

    /// <summary>
    /// Creates a new <see cref="BratNameSampleStream"/>.
    /// </summary>
    /// <param name="sentDetector">a <see cref="ISentenceDetector"/> instance</param>
    /// <param name="tokenizer">a <see cref="ITokenizer"/> instance</param>
    /// <param name="samples">a <see cref="BratDocument"/> <see cref="IObjectStream{T}"/></param>
    public BratNameSampleStream(ISentenceDetector sentDetector,
        ITokenizer tokenizer, IObjectStream<BratDocument?> samples)
        : base(samples)
    {
        parser = new BratDocumentParser(sentDetector, tokenizer);
    }

    /// <summary>
    /// Creates a new <see cref="BratNameSampleStream"/>.
    /// </summary>
    /// <param name="sentModel">a <see cref="SentenceModel"/> model</param>
    /// <param name="tokenModel">a <see cref="TokenizerModel"/> model</param>
    /// <param name="samples">a <see cref="BratDocument"/> <see cref="IObjectStream{T}"/></param>
    public BratNameSampleStream(SentenceModel sentModel, TokenizerModel tokenModel,
        IObjectStream<BratDocument?> samples)
        : base(samples)
    {
        // TODO: We can pass in custom validators here ...
        parser = new BratDocumentParser(new SentenceDetectorME(sentModel), new TokenizerME(tokenModel));
    }

    /// <summary>
    /// Creates a new <see cref="BratNameSampleStream"/>.
    /// </summary>
    /// <param name="sentDetector">a <see cref="ISentenceDetector"/> instance</param>
    /// <param name="tokenizer">a <see cref="ITokenizer"/> instance</param>
    /// <param name="samples">a <see cref="BratDocument"/> <see cref="IObjectStream{T}"/></param>
    /// <param name="nameTypes">the name types to use or <c>null</c> if all name types</param>
    public BratNameSampleStream(ISentenceDetector sentDetector,
        ITokenizer tokenizer, IObjectStream<BratDocument?> samples, ISet<string>? nameTypes)
        : base(samples)
    {
        parser = new BratDocumentParser(sentDetector, tokenizer, nameTypes);
    }

    /// <summary>
    /// Creates a new <see cref="BratNameSampleStream"/>.
    /// </summary>
    /// <param name="sentModel">a <see cref="SentenceModel"/> model</param>
    /// <param name="tokenModel">a <see cref="TokenizerModel"/> model</param>
    /// <param name="samples">a <see cref="BratDocument"/> <see cref="IObjectStream{T}"/></param>
    /// <param name="nameTypes">the name types to use or <c>null</c> if all name types</param>
    public BratNameSampleStream(SentenceModel sentModel, TokenizerModel tokenModel,
        IObjectStream<BratDocument?> samples, ISet<string>? nameTypes)
        : base(samples)
    {
        // TODO: We can pass in custom validators here ...
        parser = new BratDocumentParser(new SentenceDetectorME(sentModel), new TokenizerME(tokenModel), nameTypes);
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    protected override IList<NameSample>? Read(BratDocument sample) => parser.Parse(sample);
}
