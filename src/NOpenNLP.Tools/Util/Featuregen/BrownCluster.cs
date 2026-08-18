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
using NOpenNLP.Tools.Util.Model;
using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using NOpenNLP.Tools.Support;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
///
/// Class to load a Brown cluster document: word\tword_class\tprob
/// http://metaoptimize.com/projects/wordreprs/
///
/// The file containing the clustering lexicon has to be passed as the
/// value of the dict attribute of each BrownCluster feature generator.
/// </summary>
public class BrownCluster : ISerializableArtifact
{
    private static readonly Regex tabPattern = new Regex("\t");

    public class BrownClusterSerializer : IArtifactSerializer<BrownCluster>
    {
        public virtual BrownCluster Create(Stream @in)
        {
            return new BrownCluster(@in);
        }

        public virtual void Serialize(BrownCluster artifact, Stream @out)
        {
            artifact.Serialize(@out);
        }
        // NOpenNLP: upstream relies on a default interface implementation to
        // bridge the non-generic IArtifactSerializer; DIMs are unavailable on
        // netstandard2.0/net462, so the bridge is explicit here.
        object IArtifactSerializer.Create(Stream @in) => Create(@in);

        void IArtifactSerializer.Serialize(object artifact, Stream @out) =>
            Serialize((BrownCluster)artifact, @out);
    }

    private readonly JCG.Dictionary<string, string> tokenToClusterMap = new(); // NOpenNLP: made readonly

    /// <summary>
    /// Generates the token to cluster map from Brown cluster input file.
    /// NOTE: we only add those tokens with frequency bigger than 5.
    /// </summary>
    /// <param name="in">the inputstream</param>
    /// <exception cref="IOException">the io exception</exception>
    public BrownCluster(Stream @in)
    {
        var breader = new StreamReader(@in, Encoding.UTF8);

        while (breader.ReadLine() is { } line)
        {
            string[] lineArray = tabPattern.Split(line);
            if (lineArray.Length == 3)
            {
                int freq = int.Parse(lineArray[2]);
                if (freq > 5)
                {
                    tokenToClusterMap.Put(lineArray[1], lineArray[0]);
                }
            }
            else if (lineArray.Length == 2)
            {
                tokenToClusterMap.Put(lineArray[0], lineArray[1]);
            }
        }
    }

    /// <summary>
    /// Check if a token is in the Brown:paths, token map.
    /// </summary>
    /// <param name="string">the token to look-up</param>
    /// <returns>the brown class if such token is in the brown cluster map</returns>
    public virtual string? LookupToken(string @string)
    {
        return tokenToClusterMap.TryGetValue(@string, out var value) ? value : null;
    }

    /// <summary>
    /// Writes the cluster dictionary to the given <see cref="Stream"/>.
    /// </summary>
    /// <param name="out">the <see cref="Stream"/> to write the dictionary into.</param>
    /// <exception cref="IOException"/>
    public virtual void Serialize(Stream @out)
    {
        // NOpenNLP: upstream wraps the stream in an OutputStreamWriter with no charset,
        // which takes the platform default; the reader side of this port already fixes
        // UTF-8, so the writer does too, without a BOM. leaveOpen keeps the stream open
        // for the caller, matching Java, where the writer is only flushed and never closed.
        using var writer = new StreamWriter(@out, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 1024, leaveOpen: true);

        foreach (var entry in tokenToClusterMap)
        {
            // NOpenNLP: upstream writes "\n" literally rather than a platform newline,
            // so the file is byte-identical across platforms; StreamWriter.WriteLine
            // would emit Environment.NewLine instead.
            writer.Write(entry.Key + "\t" + entry.Value + "\n");
        }

        writer.Flush();
    }

    public virtual Type ArtifactSerializerClass => typeof(BrownClusterSerializer);
}
