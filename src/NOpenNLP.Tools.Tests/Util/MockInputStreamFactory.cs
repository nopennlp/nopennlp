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

namespace NOpenNLP.Tools.Util;

public class MockInputStreamFactory : IInputStreamFactory
{
    private readonly FileInfo? inputSourceFile;

    private readonly string? inputSourceStr;

    private readonly Encoding? charset;

    public MockInputStreamFactory(FileInfo file)
    {
        inputSourceFile = file;
        inputSourceStr = null;
        charset = null;
    }

    public MockInputStreamFactory(string str)
        : this(str, Encoding.UTF8)
    {
    }

    public MockInputStreamFactory(string str, Encoding charset)
    {
        inputSourceFile = null;
        inputSourceStr = str;
        this.charset = charset;
    }

    public Stream CreateInputStream()
    {
        if (inputSourceFile != null)
        {
            // NOpenNLP: upstream resolves the path against the classpath; the
            // ported tests pass a real file path instead.
            return new FileStream(inputSourceFile.FullName, FileMode.Open, FileAccess.Read);
        }

        return new MemoryStream(charset!.GetBytes(inputSourceStr!));
    }
}
