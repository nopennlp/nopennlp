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
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats;

public class DirectorySampleStreamTest
{
    // NOpenNLP: upstream uses JUnit's @Rule TemporaryFolder, which NUnit has no direct
    // equivalent for; TempDirectory stands in for it, created per test and removed again.
    private TempDirectory tempDirectory = null!;

    [SetUp]
    public void Setup() => tempDirectory = new TempDirectory();

    [TearDown]
    public void TearDown() => tempDirectory.Dispose();

    // NOpenNLP: upstream declares this as a java.io.FileFilter inner class named
    // TempFileNameFilter. The ported DirectorySampleStream takes a
    // Func<FileSystemInfo, bool> instead, so the same predicate is a local method.
    private static bool TempFileNameFilter(FileSystemInfo f) =>
        f is DirectoryInfo || f.Name.EndsWith(".tmp", StringComparison.Ordinal);

    // NOpenNLP: upstream asserts files.contains(file), which works because java.io.File
    // has value equality on the path. FileInfo does not override Equals, so a
    // List<FileInfo>.Contains would compare by reference and always fail; the paths are
    // compared instead, which is the comparison upstream is actually making.
    private static bool Contains(List<FileInfo> files, FileInfo? file)
    {
        if (file is null)
        {
            return false;
        }

        foreach (FileInfo f in files)
        {
            if (string.Equals(f.FullName, file.FullName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    [Test]
    public void DirectoryTest()
    {
        Func<FileSystemInfo, bool> filter = TempFileNameFilter;

        List<FileInfo> files = [];

        // NOpenNLP: upstream's tempDirectory.newFile() creates a file with a ".tmp"
        // suffix, which is what TempFileNameFilter matches on.
        FileInfo temp1 = tempDirectory.CreateFile("temp1.tmp", string.Empty);
        files.Add(temp1);

        FileInfo temp2 = tempDirectory.CreateFile("temp2.tmp", string.Empty);
        files.Add(temp2);

        DirectorySampleStream stream = new(tempDirectory.DirectoryInfo, filter, false);

        FileInfo? file = stream.Read();
        ClassicAssert.IsTrue(Contains(files, file));

        file = stream.Read();
        ClassicAssert.IsTrue(Contains(files, file));

        file = stream.Read();
        ClassicAssert.Null(file);

        stream.Dispose();
    }

    [Test]
    public void DirectoryNullFilterTest()
    {
        List<FileInfo> files = [];

        FileInfo temp1 = tempDirectory.CreateFile("temp1.tmp", string.Empty);
        files.Add(temp1);

        FileInfo temp2 = tempDirectory.CreateFile("temp2.tmp", string.Empty);
        files.Add(temp2);

        DirectorySampleStream stream = new(tempDirectory.DirectoryInfo, null, false);

        FileInfo? file = stream.Read();
        ClassicAssert.IsTrue(Contains(files, file));

        file = stream.Read();
        ClassicAssert.IsTrue(Contains(files, file));

        file = stream.Read();
        ClassicAssert.Null(file);

        stream.Dispose();
    }

    [Test]
    public void RecursiveDirectoryTest()
    {
        Func<FileSystemInfo, bool> filter = TempFileNameFilter;

        List<FileInfo> files = [];

        FileInfo temp1 = tempDirectory.CreateFile("temp1.tmp", string.Empty);
        files.Add(temp1);

        DirectoryInfo tempSubDirectory = Directory.CreateDirectory(
            Path.Combine(tempDirectory.Path, "sub1"));
        FileInfo temp2 = new(Path.Combine(tempSubDirectory.FullName, "sub1.tmp"));
        File.WriteAllText(temp2.FullName, string.Empty);
        files.Add(temp2);

        DirectorySampleStream stream = new(tempDirectory.DirectoryInfo, filter, true);

        FileInfo? file = stream.Read();
        ClassicAssert.IsTrue(Contains(files, file));

        file = stream.Read();
        ClassicAssert.IsTrue(Contains(files, file));

        file = stream.Read();
        ClassicAssert.Null(file);

        stream.Dispose();
    }

    [Test]
    public void ResetDirectoryTest()
    {
        Func<FileSystemInfo, bool> filter = TempFileNameFilter;

        List<FileInfo> files = [];

        FileInfo temp1 = tempDirectory.CreateFile("temp1.tmp", string.Empty);
        files.Add(temp1);

        FileInfo temp2 = tempDirectory.CreateFile("temp2.tmp", string.Empty);
        files.Add(temp2);

        DirectorySampleStream stream = new(tempDirectory.DirectoryInfo, filter, false);

        FileInfo? file = stream.Read();
        ClassicAssert.IsTrue(Contains(files, file));

        stream.Reset();

        file = stream.Read();
        ClassicAssert.IsTrue(Contains(files, file));

        file = stream.Read();
        ClassicAssert.IsTrue(Contains(files, file));

        file = stream.Read();
        ClassicAssert.Null(file);

        stream.Dispose();
    }

    [Test]
    public void EmptyDirectoryTest()
    {
        Func<FileSystemInfo, bool> filter = TempFileNameFilter;

        DirectorySampleStream stream = new(tempDirectory.DirectoryInfo, filter, false);

        ClassicAssert.Null(stream.Read());

        stream.Dispose();
    }

    [Test]
    public void InvalidDirectoryTest()
    {
        Func<FileSystemInfo, bool> filter = TempFileNameFilter;

        FileInfo notADirectory = tempDirectory.CreateFile("temp1.tmp", string.Empty);

        // NOpenNLP: upstream declares @Test(expected = IllegalArgumentException.class),
        // which the DirectorySampleStream constructor throws; ArgumentException is the
        // .NET counterpart.
        Assert.Throws<ArgumentException>((Action)(() =>
        {
            DirectorySampleStream stream = new(
                new DirectoryInfo(notADirectory.FullName), filter, false);

            ClassicAssert.Null(stream.Read());

            stream.Dispose();
        }));
    }
}
