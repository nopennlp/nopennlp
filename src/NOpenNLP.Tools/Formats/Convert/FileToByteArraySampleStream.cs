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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Convert;

// NOpenNLP: upstream streams over java.io.File; FileInfo is the .NET counterpart.
public class FileToByteArraySampleStream(IObjectStream<FileInfo?> samples)
    : FilterObjectStream<FileInfo?, byte[]?>(samples)
{
    private static byte[] ReadFile(FileInfo file)
    {
        var bytes = new MemoryStream();

        using (Stream @in = file.OpenRead())
        {
            byte[] buffer = new byte[1024];
            int length;
            while ((length = @in.Read(buffer, 0, buffer.Length)) > 0)
            {
                bytes.Write(buffer, 0, length);
            }
        }

        return bytes.ToArray();
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override byte[]? Read()
    {
        FileInfo? sampleFile = samples.Read();

        if (sampleFile != null)
        {
            return ReadFile(sampleFile);
        }
        else
        {
            return null;
        }
    }
}
