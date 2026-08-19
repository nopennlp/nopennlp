/*
 * Copyright 2026 NOpenNLP Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.IO;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Writes the primitives of Java's <c>java.io.DataOutput</c> to a
/// <see cref="Stream"/>, in the big-endian layout <c>DataInputStream</c> reads.
/// This is the exact inverse of <see cref="DataInput"/>, so a model written here
/// is byte-identical to one Apache OpenNLP would have written.
/// </summary>
/// <remarks>
/// <para/>Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// <para/>These extension methods exist so the binary model writers can accept a
/// plain <see cref="Stream"/> without a Java-shim or J2N type appearing in the
/// public API.
/// </remarks>
internal static class DataOutput
{
    /// <summary>
    /// Writes a big-endian 4-byte signed integer, as <c>DataOutput.writeInt(int)</c> does.
    /// </summary>
    public static void WriteJavaInt32(this Stream stream, int v)
    {
        stream.WriteByte((byte)((v >> 24) & 0xFF));
        stream.WriteByte((byte)((v >> 16) & 0xFF));
        stream.WriteByte((byte)((v >> 8) & 0xFF));
        stream.WriteByte((byte)(v & 0xFF));
    }

    /// <summary>
    /// Writes a big-endian IEEE 754 double, as <c>DataOutput.writeDouble(double)</c> does.
    /// </summary>
    public static void WriteJavaDouble(this Stream stream, double v)
    {
        long bits = BitConverter.DoubleToInt64Bits(v);
        for (int shift = 56; shift >= 0; shift -= 8)
        {
            stream.WriteByte((byte)((bits >> shift) & 0xFF));
        }
    }

    /// <summary>
    /// Writes a big-endian IEEE 754 float, as <c>DataOutput.writeFloat(float)</c> does.
    /// </summary>
    public static void WriteJavaSingle(this Stream stream, float v)
    {
        // NOpenNLP: netstandard2.0 has no BitConverter.SingleToInt32Bits, and
        // BitConverter.GetBytes is little-endian on the platforms we target, so
        // the bytes are reversed to reach Java's big-endian order.
        byte[] bytes = BitConverter.GetBytes(v);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        stream.Write(bytes, 0, 4);
    }

    /// <summary>
    /// Writes a string in Java's <i>modified UTF-8</i> encoding, as
    /// <c>DataOutput.writeUTF(String)</c> does: a big-endian 2-byte unsigned byte
    /// count followed by that many bytes.
    /// </summary>
    /// <remarks>
    /// See <see cref="DataInput.ReadJavaUTF"/> for how modified UTF-8 differs from
    /// standard UTF-8. Because the length is a 16-bit count of <i>bytes</i>, a
    /// string whose encoded form exceeds 65535 bytes cannot be represented, and
    /// Java throws <c>UTFDataFormatException</c> in that case.
    /// </remarks>
    /// <exception cref="FormatException">
    /// the encoded string is longer than 65535 bytes.
    /// </exception>
    public static void WriteJavaUTF(this Stream stream, string s)
    {
        // Java counts the encoded bytes before writing any of them, so an
        // over-long string fails without leaving a partial record behind.
        long byteCount = 0;
        foreach (char c in s)
        {
            if (c >= 0x0001 && c <= 0x007F)
            {
                byteCount++;
            }
            else if (c <= 0x07FF)
            {
                // Includes U+0000, which modified UTF-8 encodes as two bytes so
                // that no NUL byte appears inside the encoded form.
                byteCount += 2;
            }
            else
            {
                // Surrogates are encoded one half at a time, three bytes each,
                // rather than combined into a single four-byte sequence.
                byteCount += 3;
            }
        }

        if (byteCount > 65535)
        {
            throw new FormatException(
                $"Encoded string too long: {byteCount} bytes, maximum is 65535.");
        }

        byte[] bytes = new byte[byteCount];
        int i = 0;
        foreach (char c in s)
        {
            if (c >= 0x0001 && c <= 0x007F)
            {
                bytes[i++] = (byte)c;
            }
            else if (c <= 0x07FF)
            {
                bytes[i++] = (byte)(0xC0 | ((c >> 6) & 0x1F));
                bytes[i++] = (byte)(0x80 | (c & 0x3F));
            }
            else
            {
                bytes[i++] = (byte)(0xE0 | ((c >> 12) & 0x0F));
                bytes[i++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                bytes[i++] = (byte)(0x80 | (c & 0x3F));
            }
        }

        stream.WriteByte((byte)((byteCount >> 8) & 0xFF));
        stream.WriteByte((byte)(byteCount & 0xFF));
        stream.Write(bytes, 0, bytes.Length);
    }
}
