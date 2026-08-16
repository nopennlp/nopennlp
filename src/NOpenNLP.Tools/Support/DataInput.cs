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
using System.Text;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Reads the primitives of Java's <c>java.io.DataInput</c> from a
/// <see cref="Stream"/>, in the big-endian layout <c>DataOutputStream</c>
/// writes. Model files produced by Apache OpenNLP use that layout, so the
/// binary readers must decode it exactly.
/// </summary>
/// <remarks>
/// <para/>Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// <para/>These extension methods exist so the binary model readers can accept a
/// plain <see cref="Stream"/> without a Java-shim or J2N type appearing in the
/// public API.
/// </remarks>
internal static class DataInput
{
    /// <summary>
    /// Reads a big-endian 4-byte signed integer, as <c>DataInput.readInt()</c> does.
    /// </summary>
    /// <exception cref="EndOfStreamException">the stream ended mid-value.</exception>
    public static int ReadJavaInt32(this Stream stream)
    {
        int b1 = ReadByteOrThrow(stream);
        int b2 = ReadByteOrThrow(stream);
        int b3 = ReadByteOrThrow(stream);
        int b4 = ReadByteOrThrow(stream);
        return (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
    }

    /// <summary>
    /// Reads a big-endian IEEE 754 double, as <c>DataInput.readDouble()</c> does.
    /// </summary>
    /// <exception cref="EndOfStreamException">the stream ended mid-value.</exception>
    public static double ReadJavaDouble(this Stream stream)
    {
        long bits = 0;
        for (int i = 0; i < 8; i++)
        {
            bits = (bits << 8) | (uint)ReadByteOrThrow(stream);
        }

        return BitConverter.Int64BitsToDouble(bits);
    }

    /// <summary>
    /// Reads a string in Java's <i>modified UTF-8</i> encoding, as
    /// <c>DataInput.readUTF()</c> does: a big-endian 2-byte unsigned byte count
    /// followed by that many bytes.
    /// </summary>
    /// <remarks>
    /// Modified UTF-8 is not standard UTF-8, so <see cref="Encoding.UTF8"/> cannot
    /// be used. It differs in two ways: U+0000 is encoded as the two bytes
    /// <c>C0 80</c> rather than a NUL byte, and characters outside the basic
    /// multilingual plane are written as a surrogate pair with each half encoded
    /// separately in three bytes, rather than as one four-byte sequence.
    /// </remarks>
    /// <exception cref="EndOfStreamException">the stream ended mid-value.</exception>
    /// <exception cref="FormatException">the bytes are not valid modified UTF-8.</exception>
    public static string ReadJavaUTF(this Stream stream)
    {
        int length = (ReadByteOrThrow(stream) << 8) | ReadByteOrThrow(stream);

        byte[] bytes = new byte[length];
        int read = 0;
        while (read < length)
        {
            int n = stream.Read(bytes, read, length - read);
            if (n <= 0)
            {
                throw new EndOfStreamException();
            }

            read += n;
        }

        StringBuilder sb = new StringBuilder(length);
        int i = 0;
        while (i < length)
        {
            int b1 = bytes[i++];
            if (b1 < 0x80)
            {
                // 0xxxxxxx: one byte. Note U+0000 never takes this form.
                sb.Append((char)b1);
            }
            else if ((b1 & 0xE0) == 0xC0)
            {
                // 110xxxxx 10xxxxxx: two bytes.
                if (i >= length)
                {
                    throw new FormatException("Truncated modified UTF-8 sequence.");
                }

                int b2 = bytes[i++];
                if ((b2 & 0xC0) != 0x80)
                {
                    throw new FormatException("Malformed modified UTF-8 sequence.");
                }

                sb.Append((char)(((b1 & 0x1F) << 6) | (b2 & 0x3F)));
            }
            else if ((b1 & 0xF0) == 0xE0)
            {
                // 1110xxxx 10xxxxxx 10xxxxxx: three bytes. A non-BMP character
                // arrives as two of these, one per surrogate half, and each half
                // is appended as-is to reconstitute the pair.
                if (i + 1 >= length)
                {
                    throw new FormatException("Truncated modified UTF-8 sequence.");
                }

                int b2 = bytes[i++];
                int b3 = bytes[i++];
                if ((b2 & 0xC0) != 0x80 || (b3 & 0xC0) != 0x80)
                {
                    throw new FormatException("Malformed modified UTF-8 sequence.");
                }

                sb.Append((char)(((b1 & 0x0F) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F)));
            }
            else
            {
                throw new FormatException("Malformed modified UTF-8 sequence.");
            }
        }

        return sb.ToString();
    }

    private static int ReadByteOrThrow(Stream stream)
    {
        int b = stream.ReadByte();
        if (b < 0)
        {
            throw new EndOfStreamException();
        }

        return b;
    }
}
