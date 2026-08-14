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
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util.Model;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Util.Featuregen;

public class WordClusterDictionary : ISerializableArtifact
{
    public class WordClusterDictionarySerializer : IArtifactSerializer<WordClusterDictionary>
    {
        public virtual WordClusterDictionary Create(Stream @in)
        {
            return new WordClusterDictionary(@in);
        }

        // NOpenNLP: serialization is not supported; inference only.
        // public virtual void Serialize(WordClusterDictionary artifact, Stream @out)
        // {
        //     artifact.Serialize(@out);
        // }
        // NOpenNLP: upstream relies on a default interface implementation to
        // bridge the non-generic IArtifactSerializer; DIMs are unavailable on
        // netstandard2.0/net462, so the bridge is explicit here.
        object IArtifactSerializer.Create(Stream @in) => Create(@in);
    }

    private readonly JCG.Dictionary<string, string> tokenToClusterMap = new(); // NOpenNLP: made readonly

    /// <summary>
    /// Read word2vec and clark clustering style lexicons.
    /// </summary>
    /// <param name="in">the inputstream</param>
    /// <exception cref="IOException">the io exception</exception>
    public WordClusterDictionary(Stream @in)
    {
        var reader = new StreamReader(@in, Encoding.UTF8);
        while (reader.ReadLine() is { } line)
        {
            string[] parts = line.Split(' ');
            if (parts.Length == 3)
            {
                tokenToClusterMap.Put(parts[0], string.Intern(parts[1]));
            }
            else if (parts.Length == 2)
            {
                tokenToClusterMap.Put(parts[0], string.Intern(parts[1]));
            }
        }
    }

    public virtual string? LookupToken(string @string)
    {
        return tokenToClusterMap[@string];
    }

    // NOpenNLP: serialization is not supported; inference only.
    // public virtual void Serialize(Stream @out)
    // {
    //     Writer writer = new BufferedWriter(new OutputStreamWriter(@out));
    //     foreach (Map.Entry<String, String> entry in tokenToClusterMap.EntrySet())
    //     {
    //         writer.Write(entry.GetKey() + " " + entry.GetValue() + "\n");
    //     }
    //
    //     writer.Flush();
    // }

    public virtual Type ArtifactSerializerClass => typeof(WordClusterDictionarySerializer);
}
