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
using System.Globalization;
using System.IO;
using System.Text;
using J2N.Globalization;
using NOpenNLP.Tools.Support;
using JSingle = J2N.Numerics.Single;

namespace NOpenNLP.Tools.Ml.Model;

public class RealValueFileEventStream : FileEventStream
{
    public RealValueFileEventStream(string fileName)
        : base(fileName)
    {
    }

    public RealValueFileEventStream(string fileName, Encoding? encoding)
        : base(fileName, encoding)
    {
    }

    public RealValueFileEventStream(FileInfo file)
        : base(file)
    {
    }

    /// <summary>
    /// Parses the specified contexts and re-populates context array with features
    /// and returns the values for these features. If all values are unspecified,
    /// then <c>null</c> is returned.
    /// </summary>
    /// <param name="contexts">The contexts with real values specified.</param>
    /// <returns>The value for each context or <c>null</c> if all values are unspecified.</returns>
    public static float[]? ParseContexts(string[] contexts)
    {
        bool hasRealValue = false;
        float[]? values = new float[contexts.Length];
        for (int ci = 0; ci < contexts.Length; ci++)
        {
            int ei = contexts[ci].LastIndexOf('=');
            if (ei > 0 && ei + 1 < contexts[ci].Length)
            {
                bool gotReal = true;
                try
                {
                    // NOpenNLP: upstream uses Float.parseFloat, which is
                    // culture-invariant and accepts Java's float syntax. J2N
                    // reproduces it; float.Parse would use the current culture.
                    values[ci] = JSingle.Parse(contexts[ci].Substring(ei + 1),
                        NumberStyle.Float, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    gotReal = false;
                    Console.Error.WriteLine("Unable to determine value in context:" + contexts[ci]);
                    values[ci] = 1;
                }

                if (gotReal)
                {
                    if (values[ci] < 0)
                    {
                        throw new RuntimeException("Negative values are not allowed: " + contexts[ci]);
                    }

                    contexts[ci] = contexts[ci].Substring(0, ei);
                    hasRealValue = true;
                }
            }
            else
            {
                values[ci] = 1;
            }
        }

        if (!hasRealValue)
        {
            values = null;
        }

        return values;
    }

    public override Event? Read()
    {
        string? line;
        if ((line = reader.ReadLine()) != null)
        {
            int si = line.IndexOf(' ');
            string outcome = line[..si];
            string[] contexts = line[(si + 1)..].Split(' ');
            float[]? values = ParseContexts(contexts);
            return new Event(outcome, contexts, values);
        }

        return null;
    }
}
