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

using System.IO;
using System.Text;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Convert;

/// <summary>
/// Provides the ability to read the contents of files
/// contained in an object stream of files.
/// </summary>
// NOpenNLP: upstream streams over java.io.File; FileInfo is the .NET counterpart.
public class FileToStringSampleStream(IObjectStream<FileInfo?> samples, Encoding encoding)
    : FilterObjectStream<FileInfo?, string?>(samples)
{
    private readonly Encoding encoding = encoding;

    /// <summary>
    /// Reads the contents of a file to a string.
    /// </summary>
    /// <param name="textFile">The file to read.</param>
    /// <param name="encoding">The encoding for the file.</param>
    /// <returns>The string contents of the file.</returns>
    /// <exception cref="IOException">Thrown if the file cannot be read.</exception>
    private static string ReadFile(FileInfo textFile, Encoding encoding)
    {
        using var @in = new StreamReader(textFile.OpenRead(), encoding);

        var text = new StringBuilder();

        char[] buffer = new char[1024];
        int length;
        while ((length = @in.Read(buffer, 0, buffer.Length)) > 0)
        {
            text.Append(buffer, 0, length);
        }

        return text.ToString();
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override string? Read()
    {
        FileInfo? sampleFile = samples.Read();

        if (sampleFile != null)
        {
            return ReadFile(sampleFile, encoding);
        }
        else
        {
            return null;
        }
    }
}
