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

namespace NOpenNLP.Tools.Formats.Brat;

public class BratDocumentStream : ObjectStreamBase<BratDocument?>
{
    private readonly AnnotationConfiguration config; // NOpenNLP: made readonly
    private IList<string>? documentIds = new JCG.List<string>();

    // NOpenNLP: upstream keeps a java.util.Iterator advanced across hasNext()/next().
    // An IEnumerator is the C# counterpart for that manual advancing.
    private IEnumerator<string>? documentIdIterator;

    /// <summary>
    /// Creates a <see cref="BratDocumentStream"/> which reads the documents from the
    /// given input directory.
    /// </summary>
    /// <param name="config">the annotation.conf from the brat project as an
    /// Annotation Configuration object</param>
    /// <param name="bratCorpusDirectory">the directory containing all the brat
    /// training data files</param>
    /// <param name="searchRecursive">specifies if the corpus directory should be
    /// traversed recursively to find training data files.</param>
    /// <param name="fileFilter">a custom file filter to filter out certain files
    /// or <c>null</c> to accept all files</param>
    /// <exception cref="IOException">if reading from the brat directory fails in anyway</exception>
    // NOpenNLP: java.io.FileFilter -- a single-method interface -- becomes a
    // Func<FileSystemInfo, bool>, matching DirectorySampleStream. It takes
    // FileSystemInfo rather than FileInfo because Java's File.listFiles(FileFilter)
    // offers the filter directories as well as files, and the recursive branch below
    // depends on directories reaching it. A null filter accepts everything, as
    // upstream's null FileFilter does.
    public BratDocumentStream(AnnotationConfiguration config, DirectoryInfo bratCorpusDirectory,
        bool searchRecursive, Func<FileSystemInfo, bool>? fileFilter)
    {
        if (!bratCorpusDirectory.Exists)
        {
            throw new IOException("Input corpus directory must be a directory " +
                "according to File.isDirectory()!");
        }

        this.config = config;

        // NOpenNLP: J2N has no Stack; the BCL Stack<T> behaves the same for the
        // push/pop traversal upstream does here, and nothing depends on Java semantics.
        var directoryStack = new Stack<DirectoryInfo>();
        directoryStack.Push(bratCorpusDirectory);

        while (directoryStack.Count > 0)
        {
            foreach (var entry in directoryStack.Pop().EnumerateFileSystemInfos())
            {
                if (fileFilter is not null && !fileFilter(entry))
                {
                    continue;
                }

                if (entry is FileInfo file)
                {
                    string annFilePath = file.FullName;
                    if (annFilePath.EndsWith(".ann", StringComparison.Ordinal))
                    {
                        // cutoff last 4 chars ...
                        string documentId = annFilePath[..^4];

                        var txtFile = new FileInfo(documentId + ".txt");

                        if (txtFile.Exists)
                        {
                            documentIds.Add(documentId);
                        }
                    }
                }
                else if (searchRecursive && entry is DirectoryInfo subDirectory)
                {
                    directoryStack.Push(subDirectory);
                }
            }
        }

        Reset();
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override BratDocument? Read()
    {
        BratDocument? doc = null;

        if (documentIdIterator!.MoveNext())
        {
            string id = documentIdIterator.Current;

            using Stream txtIn = new FileInfo(id + ".txt").OpenRead();
            using Stream annIn = new FileInfo(id + ".ann").OpenRead();

            doc = BratDocument.ParseDocument(config, id, txtIn, annIn);
        }

        return doc;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        documentIdIterator?.Dispose();
        documentIdIterator = documentIds!.GetEnumerator();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        // No longer needed, make the object unusable
        documentIdIterator?.Dispose();
        documentIds = null;
        documentIdIterator = null;
    }
}
