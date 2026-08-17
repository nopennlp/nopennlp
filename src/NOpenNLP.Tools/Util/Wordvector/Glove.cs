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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;
#pragma warning disable NONLPEXP0001

namespace NOpenNLP.Tools.Util.Wordvector;

/// <summary>
/// Parses Glove word vector files.
/// <para/>
/// Warning: Experimental new feature, see OPENNLP-1144 for details, the API might be changed anytime.
/// </summary>
[Experimental("NONLPEXP0001")]
public static class Glove
{
    private const int BufferSize = 1024 * 1024;

    /// <summary>
    /// Parses a glove vector plain text file.
    /// <para/>
    /// Warning: Experimental new feature, see OPENNLP-1144 for details, the API might be changed anytime.
    /// </summary>
    /// <param name="in">The input stream for Glove vectors.</param>
    /// <returns>A Glove based wv table.</returns>
    /// <exception cref="IOException">Thrown if any error occurs during parsing.</exception>
    public static IWordVectorTable Parse(Stream @in)
    {
        // NOpenNLP: leaveOpen matches Java, where the caller owns the stream passed to the reader.
        using var reader = new StreamReader(@in, new UTF8Encoding(false), false, BufferSize, leaveOpen: true);

        var vectors = new JCG.Dictionary<string, IWordVector>();

        int dimension = -1;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            // NOpenNLP: Java's String.split(" ") drops trailing empty fields, so a line ending in
            // a space yields the same part count as one that does not. C#'s Split keeps them,
            // which would infer the wrong dimension and fail on files GloVe ships with.
            string[] parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);

            if (dimension == -1)
            {
                dimension = parts.Length - 1;
            }
            else if (dimension != parts.Length - 1)
            {
                throw new IOException("Vector dimension must be constant!");
            }

            string token = parts[0];

            float[] vector = new float[dimension];

            for (int i = 0; i < vector.Length; i++)
            {
                // NOpenNLP: Java's Float.parseFloat is culture-invariant; parse invariantly so a
                // locale with a comma decimal separator does not misread the file.
                vector[i] = float.Parse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            vectors[token] = new FloatArrayVector(vector);
        }

        return new MapWordVectorTable(vectors);
    }
}
