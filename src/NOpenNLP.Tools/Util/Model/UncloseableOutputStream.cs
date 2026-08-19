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

namespace NOpenNLP.Tools.Util.Model;

/// <summary>
/// A <see cref="Stream"/> which cannot be closed.
/// <para/>
/// NOpenNLP: upstream writes this inline in <see cref="ModelUtil.WriteModel"/> as an
/// anonymous java.io.OutputStream that forwards write(int) and inherits the no-op
/// close(). Naming it here mirrors <see cref="UncloseableInputStream"/> and keeps
/// WriteModel readable.
/// </summary>
public class UncloseableOutputStream(Stream @out) : Stream
{
    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => @out.CanWrite;

    public override long Length => @out.Length;

    public override long Position
    {
        get => @out.Position;
        set => @out.Position = value;
    }

    public override void Flush() => @out.Flush();

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => @out.Write(buffer, offset, count);

    public override void WriteByte(byte value) => @out.WriteByte(value);

    /// <summary>
    /// This method does not have any effect; the <see cref="Stream"/>
    /// cannot be closed.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        // Deliberately does not dispose the wrapped stream.
    }
}
