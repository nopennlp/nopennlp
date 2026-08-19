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
using System.IO;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Lemmatizer;

/// <summary>
/// This dummy lemma sample stream reads a file containing forms, postags, gold
/// lemmas, and predicted lemmas. It can be used together with DummyLemmatizer
/// simulate a lemmatizer.
/// </summary>
/// <remarks>
/// the predicted flag sets if the stream will contain the expected or the
/// predicted tags.
/// </remarks>
public class DummyLemmaSampleStream(IObjectStream<string?> samples, bool isPredicted)
    : FilterObjectStream<string?, LemmaSample?>(samples)
{
    private readonly bool mIsPredicted = isPredicted; // NOpenNLP: made readonly
    private int count = 0;

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override LemmaSample? Read()
    {
        IList<string> toks = new JCG.List<string>();
        IList<string> posTags = new JCG.List<string>();
        IList<string> goldLemmas = new JCG.List<string>();
        IList<string> predictedLemmas = new JCG.List<string>();

        for (string? line = samples.Read(); line != null && !line.Equals(""); line = samples.Read())
        {
            string[] parts = line.Split('\t');
            if (parts.Length != 4)
            {
                Console.Error.WriteLine("Skipping corrupt line " + count + ": " + line);
            }
            else
            {
                toks.Add(parts[0]);
                posTags.Add(parts[1]);
                goldLemmas.Add(parts[2]);
                predictedLemmas.Add(parts[3]);
            }

            count++;
        }

        if (toks.Count > 0)
        {
            if (mIsPredicted)
            {
                return new LemmaSample([.. toks], [.. posTags], [.. predictedLemmas]);
            }
            else
            {
                return new LemmaSample([.. toks], [.. posTags], [.. goldLemmas]);
            }
        }
        else
        {
            return null;
        }
    }
}
