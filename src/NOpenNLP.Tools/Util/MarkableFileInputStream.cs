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

namespace NOpenNLP.Tools.Util;

/// <summary>
/// A markable File Input Stream.
/// </summary>
// NOpenNLP: upstream wraps a FileInputStream, which does not support mark/reset,
// and adds them by seeking the underlying channel. A .NET FileStream is already
// seekable, so this type only tracks the marked position and seeks back to it.
// Mark() and ResetToMark() keep the upstream names rather than mapping onto
// Stream.Position, so ported callers read the same.
internal class MarkableFileInputStream(FileInfo file) : Stream
{
    private readonly FileStream @in = new(file.FullName, FileMode.Open, FileAccess.Read);

    private long markedPosition = -1;

    public void Mark()
    {
        try
        {
            markedPosition = @in.Position;
        }
        catch (IOException)
        {
            markedPosition = -1;
        }
    }

    public void ResetToMark()
    {
        if (markedPosition >= 0)
        {
            @in.Position = markedPosition;
        }
        else
        {
            throw new IOException("Stream has to be marked before it can be reset!");
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => @in.Read(buffer, offset, count);

    public override int ReadByte() => @in.ReadByte();

    public override bool CanRead => @in.CanRead;

    public override bool CanSeek => @in.CanSeek;

    public override bool CanWrite => false;

    public override long Length => @in.Length;

    public override long Position
    {
        get => @in.Position;
        set => @in.Position = value;
    }

    public override long Seek(long offset, SeekOrigin origin) => @in.Seek(offset, origin);

    public override void Flush() => @in.Flush();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            @in.Dispose();
        }

        base.Dispose(disposing);
    }
}
