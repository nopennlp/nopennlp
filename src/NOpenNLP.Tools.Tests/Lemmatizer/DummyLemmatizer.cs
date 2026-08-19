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

namespace NOpenNLP.Tools.Lemmatizer;

/// <summary>
/// This dummy lemmatizer implementation simulates a LemmatizerME. The file has
/// samples of sentences, with target and predicted values.
/// </summary>
public class DummyLemmatizer(DummyLemmaSampleStream aSampleStream) : ILemmatizer
{
    private readonly DummyLemmaSampleStream mSampleStream = aSampleStream; // NOpenNLP: made readonly

    public string[] Lemmatize(string[] toks, string[] tags)
    {
        LemmaSample predsSample = mSampleStream.Read()!;

        // checks if the streams are sync
        for (int i = 0; i < toks.Length; i++)
        {
            if (!toks[i].Equals(predsSample.Tokens[i])
                || !tags[i].Equals(predsSample.Tags[i]))
            {
                throw new InvalidOperationException("The streams are not sync!"
                    + "\n expected sentence: " + string.Join(", ", toks)
                    + "\n expected tags: " + string.Join(", ", tags)
                    + "\n predicted sentence: "
                    + string.Join(", ", predsSample.Tokens) + "\n predicted tags: "
                    + string.Join(", ", predsSample.Tags));
            }
        }

        return predsSample.Lemmas;
    }

    // NOpenNLP: upstream returns null here; the ported interface declares a
    // non-nullable return type, and no test calls this method.
    public IList<IList<string>> Lemmatize(IList<string> toks, IList<string> tags) => [];
}
