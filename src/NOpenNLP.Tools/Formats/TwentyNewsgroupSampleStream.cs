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
using NOpenNLP.Tools.Doccat;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats;

// NOpenNLP: upstream keys the map on java.nio.file.Path; FileInfo is the .NET
// counterpart for a filesystem path that is read as a file.
public class TwentyNewsgroupSampleStream : ObjectStreamBase<DocumentSample?>
{
    private readonly ITokenizer tokenizer; // NOpenNLP: made readonly

    // NOpenNLP: upstream uses a HashMap, whose iteration order is unspecified.
    // OrderedDictionary iterates in insertion order so repeated passes -- and runs --
    // yield the samples in the same, directory-listing order.
    // FileInfo hashes by reference where java.nio.file.Path hashes by path, but the
    // enumeration below visits each file exactly once, so no key is ever looked up or
    // overwritten and the two behave identically here.
    private readonly JCG.OrderedDictionary<FileInfo, string> catFileMap = []; // NOpenNLP: made readonly

    // NOpenNLP: Read() advances one entry per call and Reset() restarts the traversal,
    // which is the manual hasNext()/next() control an IEnumerator expresses directly.
    private IEnumerator<KeyValuePair<FileInfo, string>> catFileTupleIterator;

    /// <exception cref="IOException">if there is an error while listing the data directory</exception>
    internal TwentyNewsgroupSampleStream(ITokenizer tokenizer, DirectoryInfo dataDir)
    {
        this.tokenizer = tokenizer;

        foreach (var dir in dataDir.EnumerateDirectories())
        {
            foreach (var file in dir.EnumerateFiles())
            {
                catFileMap[file] = dir.Name;
            }
        }

        // NOpenNLP: upstream calls reset() here. Reset() is virtual, and calling a
        // virtual member from a constructor would run a derived override before that
        // derived constructor had initialized its own state, so the one statement
        // reset() performs is inlined instead.
        catFileTupleIterator = catFileMap.GetEnumerator();
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override DocumentSample? Read()
    {
        if (catFileTupleIterator.MoveNext())
        {
            var catFileTuple = catFileTupleIterator.Current;

            // NOpenNLP: upstream builds the String from the raw bytes with the JVM's
            // default charset. .NET has no equivalent notion of a platform charset --
            // File.ReadAllText decodes as UTF-8 unless a BOM says otherwise -- and the
            // 20 Newsgroups corpus is ASCII, so the two agree on this data.
            string text = File.ReadAllText(catFileTuple.Key.FullName);
            return new DocumentSample(catFileTuple.Value, tokenizer.Tokenize(text));
        }

        return null;
    }

    /// <inheritdoc/>
    public override void Reset() => catFileTupleIterator = catFileMap.GetEnumerator();

    protected override void Dispose(bool disposing)
    {
    }
}
