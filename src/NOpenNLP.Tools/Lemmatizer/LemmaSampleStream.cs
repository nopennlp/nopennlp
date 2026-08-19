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
/// Reads data for training and testing the lemmatizer. The format consists of:
/// word\tpostag\tlemma.
/// </summary>
public class LemmaSampleStream(IObjectStream<string?> samples)
    : FilterObjectStream<string?, LemmaSample?>(samples)
{
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override LemmaSample? Read()
    {
        IList<string> toks = new JCG.List<string>();
        IList<string> tags = new JCG.List<string>();
        IList<string> preds = new JCG.List<string>();

        for (string? line = samples.Read(); line != null && !line.Equals(""); line = samples.Read())
        {
            string[] parts = line.Split('\t');
            if (parts.Length != 3)
            {
                Console.Error.WriteLine("Skipping corrupt line: " + line);
            }
            else
            {
                toks.Add(parts[0]);
                tags.Add(parts[1]);
                preds.Add(parts[2]);
            }
        }

        if (toks.Count > 0)
        {
            return new LemmaSample([.. toks], [.. tags], [.. preds]);
        }
        else
        {
            return null;
        }
    }
}
