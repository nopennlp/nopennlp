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
using System.Text;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Reads a plain text file and return each line as a <see cref="string"/> object.
/// </summary>
public class PlainTextByLineStream : ObjectStreamBase<string?>
{
    /// <summary>
    /// UTF-8 without a preamble, so a byte order mark is decoded as U+FEFF rather than
    /// consumed -- the way Java's <c>InputStreamReader</c> reads one.
    /// </summary>
    /// <remarks>
    /// NOpenNLP: <see cref="Encoding.UTF8"/> carries a preamble, and <c>StreamReader</c>
    /// strips a leading BOM that matches it even when
    /// <c>detectEncodingFromByteOrderMarks</c> is false. Readers that compute offsets over
    /// the text they decode need this instead. See <see cref="WithoutPreamble"/>.
    /// </remarks>
    internal static readonly Encoding Utf8NoPreamble =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly Encoding encoding;

    private readonly IInputStreamFactory inputStreamFactory; // NOpenNLP: made readonly

    private TextReader? @in;

    public PlainTextByLineStream(IInputStreamFactory inputStreamFactory, string charsetName)
        : this(inputStreamFactory, Encoding.GetEncoding(charsetName))
    {
    }

    public PlainTextByLineStream(IInputStreamFactory inputStreamFactory, Encoding charset)
    {
        this.inputStreamFactory = inputStreamFactory
            ?? throw new ArgumentNullException(nameof(inputStreamFactory), "inputStreamFactory must not be null!");
        encoding = WithoutPreamble(charset);

        Reset();
    }

    /// <summary>
    /// Returns an equivalent of <paramref name="charset"/> that carries no preamble, so a
    /// byte order mark at the start of the input is decoded rather than consumed.
    /// </summary>
    /// <remarks>
    /// NOpenNLP: Java's <c>InputStreamReader</c> has no notion of a preamble -- it decodes
    /// a BOM as U+FEFF, an ordinary character of the first line. <c>StreamReader</c> strips
    /// a leading BOM whenever it matches the encoding's <c>GetPreamble()</c>, and it does so
    /// even with <c>detectEncodingFromByteOrderMarks: false</c>, which only governs
    /// <b>switching</b> encodings. Since <see cref="Encoding.UTF8"/> is preamble-carrying,
    /// the flag alone was not enough; the encoding has to be preamble-free as well. The
    /// dropped character mattered wherever offsets are computed over the text, most sharply
    /// in brat, whose .ann files index into the .txt this stream reads.
    /// </remarks>
    private static Encoding WithoutPreamble(Encoding charset)
    {
        if (charset is null || charset.GetPreamble().Length == 0)
        {
            return charset!;
        }

        if (charset is UTF8Encoding)
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        // Any other preamble-carrying encoding (the UTF-16 and UTF-32 families) keeps its
        // byte order, since that is what tells the decoder how to read the rest of the file.
        return charset;
    }

    public override string? Read() => @in!.ReadLine();

    public override void Reset()
    {
        @in?.Dispose();

        // NOpenNLP: detectEncodingFromByteOrderMarks is off to match Java's
        // InputStreamReader, which decodes a BOM as U+FEFF -- an ordinary character of the
        // first line -- rather than consuming it. Letting .NET strip it would drop a
        // character upstream keeps, and would also let a BOM override the caller's encoding.
        @in = new StreamReader(inputStreamFactory.CreateInputStream(), encoding,
            detectEncodingFromByteOrderMarks: false);
    }

    protected override void Dispose(bool disposing)
    {
        @in?.Dispose();
    }
}
