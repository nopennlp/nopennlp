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

using J2N.Globalization;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using JDouble = J2N.Numerics.Double;

namespace NOpenNLP.Tools.Ml.Model;

public class PlainTextFileDataReader : IDataReader
{
    private readonly StreamReader input; // NOpenNLP: made readonly

    public PlainTextFileDataReader(FileInfo f)
    {
        if (f.Name.EndsWith(".gz", StringComparison.Ordinal))
        {
            input = new StreamReader(new GZipStream(f.OpenRead(), CompressionMode.Decompress));
        }
        else
        {
            input = new StreamReader(f.OpenRead());
        }
    }

    public PlainTextFileDataReader(Stream @in)
    {
        input = new StreamReader(@in);
    }

    public PlainTextFileDataReader(StreamReader @in)
    {
        input = @in;
    }

    public virtual double ReadDouble()
    {
        // NOpenNLP: validate we're not at the end of the stream
        if (input.ReadLine() is not { } line)
        {
            throw new EndOfStreamException();
        }

        // NOpenNLP: upstream uses Double.parseDouble, which is culture-invariant.
        // double.Parse without a format provider uses the current culture, so a
        // model written with '.' as the decimal separator fails to parse under a
        // locale such as de-DE that expects ','. J2N accepts every form Java does.
        return JDouble.Parse(line, NumberStyle.Float, CultureInfo.InvariantCulture);
    }

    public virtual int ReadInt32()
    {
        // NOpenNLP: validate we're not at the end of the stream
        if (input.ReadLine() is not { } line)
        {
            throw new EndOfStreamException();
        }

        // NOpenNLP: upstream uses Integer.parseInt, which is culture-invariant.
        return int.Parse(line, CultureInfo.InvariantCulture);
    }

    public virtual string ReadUTF()
    {
        // NOpenNLP: validate we're not at the end of the stream
        if (input.ReadLine() is not { } line)
        {
            throw new EndOfStreamException();
        }

        return line;
    }
}
