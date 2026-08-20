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
using NOpenNLP.Tools.Lemmatizer;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Conllu;

public class ConlluLemmaSampleStream(IObjectStream<ConlluSentence?> samples, ConlluTagset tagset)
    : FilterObjectStream<ConlluSentence?, LemmaSample?>(samples)
{
    private readonly ConlluTagset tagset = tagset;

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override LemmaSample? Read()
    {
        ConlluSentence? sentence = samples.Read();

        if (sentence != null)
        {
            IList<string> tokens = new JCG.List<string>();
            IList<string> tags = new JCG.List<string>();
            IList<string> lemmas = new JCG.List<string>();

            foreach (ConlluWordLine line in sentence.WordLines)
            {
                tokens.Add(line.Form);
                tags.Add(line.GetPosTag(tagset));
                lemmas.Add(line.Lemma);
            }

            return new LemmaSample(tokens, tags, lemmas);
        }

        return null;
    }
}
