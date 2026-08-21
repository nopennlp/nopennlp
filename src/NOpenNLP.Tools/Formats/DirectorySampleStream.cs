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

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// The directory sample stream allows for creating a stream
/// from a directory listing of files.
/// </summary>
// NOpenNLP: upstream streams java.io.File; FileInfo is the .NET counterpart.
// java.io.FileFilter -- a single-method interface -- becomes a delegate, which is how
// .NET expresses the same thing. It takes FileSystemInfo rather than FileInfo because
// a java.io.File is either a file or a directory and the filter is offered both (see
// Read). A null filter means "accept everything", as upstream's null FileFilter does.
public class DirectorySampleStream : ObjectStreamBase<FileInfo?>
{
    private readonly IList<DirectoryInfo> inputDirectories;

    private readonly bool recursive;

    private readonly Func<FileSystemInfo, bool>? fileFilter;

    // NOpenNLP: J2N has no Stack<T>; the BCL one matches java.util.Stack for the
    // push/pop/clear/isEmpty operations used here.
    private readonly Stack<DirectoryInfo> directories = new(); // NOpenNLP: made readonly

    private readonly Stack<FileInfo> textFiles = new(); // NOpenNLP: made readonly

    /// <summary>
    /// Creates a new directory sample stream.
    /// </summary>
    /// <param name="dirs">The directories to read.</param>
    /// <param name="fileFilter">The filter to apply while enumerating files.</param>
    /// <param name="recursive">Enables or disables recursive file listing.</param>
    public DirectorySampleStream(DirectoryInfo[] dirs, Func<FileSystemInfo, bool>? fileFilter, bool recursive)
    {
        this.fileFilter = fileFilter;
        this.recursive = recursive;

        var inputDirectoryList = new JCG.List<DirectoryInfo>(dirs.Length);

        foreach (var dir in dirs)
        {
            // NOpenNLP: upstream checks File.isDirectory(), which is false both for a
            // path that does not exist and for one that is a regular file.
            // DirectoryInfo.Exists reports false in exactly those same two cases.
            if (!dir.Exists)
            {
                throw new ArgumentException(
                    $"All passed in directories must be directories, but \"{dir}\" is not!", nameof(dirs));
            }

            inputDirectoryList.Add(dir);
        }

        inputDirectories = inputDirectoryList.AsReadOnly();

        foreach (var dir in inputDirectories)
        {
            directories.Push(dir);
        }
    }

    /// <summary>
    /// Creates a new directory sample stream.
    /// </summary>
    /// <param name="dir">The directory.</param>
    /// <param name="fileFilter">The filter to apply while enumerating files.</param>
    /// <param name="recursive">Enables or disables recursive file listing.</param>
    public DirectorySampleStream(DirectoryInfo dir, Func<FileSystemInfo, bool>? fileFilter, bool recursive)
        : this([dir], fileFilter, recursive)
    {
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override FileInfo? Read()
    {
        while (textFiles.Count == 0 && directories.Count > 0)
        {
            var dir = directories.Pop();

            // NOpenNLP: Java's File.listFiles(FileFilter) returns files and directories
            // in one array, applying the filter to both -- upstream's own test filter is
            // `file.isDirectory() || file.getName().endsWith(".tmp")`, which relies on
            // directories being offered to it, so the filter must not be restricted to
            // files here. EnumerateFileSystemInfos is the .NET equivalent listing, and
            // the sort standing in for Arrays.sort is ordinal so the order does not vary
            // by culture the way the default string comparison would.
            var entries = new JCG.List<FileSystemInfo>(dir.EnumerateFileSystemInfos());

            entries.Sort((x, y) => string.CompareOrdinal(x.FullName, y.FullName));

            foreach (var entry in entries)
            {
                if (fileFilter is not null && !fileFilter(entry))
                {
                    continue;
                }

                if (entry is FileInfo file)
                {
                    textFiles.Push(file);
                }
                else if (recursive && entry is DirectoryInfo subDirectory)
                {
                    directories.Push(subDirectory);
                }
            }
        }

        return textFiles.Count > 0 ? textFiles.Pop() : null;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        directories.Clear();
        textFiles.Clear();

        foreach (var dir in inputDirectories)
        {
            directories.Push(dir);
        }
    }

    /// <summary>
    /// Calling this function has no effect on the stream.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
    }
}
