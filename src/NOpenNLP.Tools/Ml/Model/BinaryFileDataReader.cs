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
using System.IO;
using System.IO.Compression;
using NOpenNLP.Tools.Support;

namespace NOpenNLP.Tools.Ml.Model;

public class BinaryFileDataReader : IDataReader
{
    // NOpenNLP-specific: matches BufferedStream's own default. The reads below are
    // one to eight bytes each, so any buffer amortizes the per-call cost; this size
    // is a compromise, small enough not to matter against a multi-megabyte model.
    private const int BufferSize = 4096;

    private readonly Stream input; // NOpenNLP: made readonly

    public BinaryFileDataReader(FileInfo f)
    {
        if (f.Name.EndsWith(".gz", StringComparison.Ordinal))
        {
            input = new GZipStream(f.OpenRead(), CompressionMode.Decompress, leaveOpen: true);
        }
        else
        {
            input = f.OpenRead();
        }

        // NOpenNLP-specific: see the stream constructor. Upstream gets this for
        // free, because Java's BinaryFileDataReader wraps both of these in a
        // BufferedInputStream before handing them to a DataInputStream.
        input = new BufferedStream(input, BufferSize);
    }

    public BinaryFileDataReader(Stream @in)
    {
        // NOpenNLP-specific: buffer the caller's stream. ReadDouble/ReadInt32 pull
        // a byte at a time (see DataInput), and the stream this is handed is
        // typically the non-buffered DeflateStream from ZipArchiveEntry.Open(),
        // where every one of those calls crosses into the decompressor. Reading a
        // 7.7 MB pos.model that way measured 232 ms against 30 ms buffered, and on
        // the CI net8.0 Windows leg it inflated the model-loading tests from under
        // a second to 38-52 s each. Upstream is unaffected: Java's
        // BinaryFileDataReader wraps its input in a BufferedInputStream.
        input = new BufferedStream(@in, BufferSize);
    }

    public virtual double ReadDouble() => input.ReadJavaDouble();

    public virtual int ReadInt32() => input.ReadJavaInt32();

    public virtual string ReadUTF() => input.ReadJavaUTF();
}
