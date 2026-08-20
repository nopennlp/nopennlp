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

using System;
using System.Collections.Generic;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// Iterator-like class for modeling document classification events.
/// </summary>
public class DocumentCategorizerEventStream : AbstractEventStream<DocumentSample>
{
    // NOpenNLP: made readonly
    private readonly DocumentCategorizerContextGenerator mContextGenerator;

    /// <summary>
    /// Initializes the current instance via samples and feature generators.
    /// </summary>
    /// <param name="data"><see cref="IObjectStream{T}"/> of <see cref="DocumentSample"/>s</param>
    /// <param name="featureGenerators">the feature generators</param>
    public DocumentCategorizerEventStream(IObjectStream<DocumentSample?> data,
        params IFeatureGenerator[] featureGenerators)
        : base(data)
        => mContextGenerator = new DocumentCategorizerContextGenerator(featureGenerators);

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="samples"><see cref="IObjectStream{T}"/> of <see cref="DocumentSample"/>s</param>
    public DocumentCategorizerEventStream(IObjectStream<DocumentSample?> samples)
        : base(samples)
        => mContextGenerator = new DocumentCategorizerContextGenerator(new BagOfWordsFeatureGenerator());

    /// <inheritdoc/>
    protected override IEnumerable<Event> CreateEvents(DocumentSample sample)
    {
        yield return new Event(sample.Category,
            mContextGenerator.GetContext(sample.Text, sample.ExtraInformation));
    }
}
