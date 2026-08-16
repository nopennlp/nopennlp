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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Tests for the <see cref="DataInput"/> extensions, which decode the
/// big-endian layout Java's <c>DataOutputStream</c> writes and OpenNLP's
/// binary model files use.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. The expected
/// byte sequences below were captured from a Java-compatible
/// <c>DataOutputStream</c>, so they pin the wire format itself rather than any
/// one implementation of it.
/// </remarks>
[NOpenNLPSpecific]
public class DataInputTest
{
    private static MemoryStream Bytes(params byte[] bytes) => new(bytes);

    [Test]
    public void TestReadJavaInt32()
    {
        ClassicAssert.AreEqual(0, Bytes(0x00, 0x00, 0x00, 0x00).ReadJavaInt32());
        ClassicAssert.AreEqual(1, Bytes(0x00, 0x00, 0x00, 0x01).ReadJavaInt32());
        ClassicAssert.AreEqual(-1, Bytes(0xFF, 0xFF, 0xFF, 0xFF).ReadJavaInt32());
        ClassicAssert.AreEqual(int.MaxValue, Bytes(0x7F, 0xFF, 0xFF, 0xFF).ReadJavaInt32());
        ClassicAssert.AreEqual(int.MinValue, Bytes(0x80, 0x00, 0x00, 0x00).ReadJavaInt32());
    }

    /// <summary>
    /// The bytes must be consumed most-significant first; a little-endian read
    /// would return 0x04030201 here.
    /// </summary>
    [Test]
    public void TestReadJavaInt32IsBigEndian()
    {
        ClassicAssert.AreEqual(0x01020304, Bytes(0x01, 0x02, 0x03, 0x04).ReadJavaInt32());
    }

    [Test]
    public void TestReadJavaInt32ReadsSuccessiveValues()
    {
        Stream stream = Bytes(0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02);

        ClassicAssert.AreEqual(1, stream.ReadJavaInt32());
        ClassicAssert.AreEqual(2, stream.ReadJavaInt32());
    }

    [Test]
    public void TestReadJavaInt32ThrowsOnTruncatedInput()
    {
        Assert.Throws<EndOfStreamException>((Action)(() => _ = Bytes(0x00, 0x01).ReadJavaInt32()));
        Assert.Throws<EndOfStreamException>((Action)(() => _ = Bytes().ReadJavaInt32()));
    }

    [Test]
    public void TestReadJavaDouble()
    {
        ClassicAssert.AreEqual(0.0,
            Bytes(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00).ReadJavaDouble());
        ClassicAssert.AreEqual(0.5,
            Bytes(0x3F, 0xE0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00).ReadJavaDouble());
        ClassicAssert.AreEqual(1.0,
            Bytes(0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00).ReadJavaDouble());
        ClassicAssert.AreEqual(-2.0,
            Bytes(0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00).ReadJavaDouble());
    }

    /// <summary>
    /// Negative zero must survive as negative zero, not collapse to 0.0, so the
    /// sign bit is checked rather than the value.
    /// </summary>
    [Test]
    public void TestReadJavaDoubleReadsNegativeZero()
    {
        double value = Bytes(0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00).ReadJavaDouble();

        ClassicAssert.AreEqual(0.0, value);
        ClassicAssert.IsTrue(double.IsNegative(value));
    }

    [Test]
    public void TestReadJavaDoubleReadsNonFiniteValues()
    {
        ClassicAssert.IsTrue(double.IsNaN(
            Bytes(0x7F, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00).ReadJavaDouble()));
        ClassicAssert.IsTrue(double.IsPositiveInfinity(
            Bytes(0x7F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00).ReadJavaDouble()));
        ClassicAssert.IsTrue(double.IsNegativeInfinity(
            Bytes(0xFF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00).ReadJavaDouble()));
    }

    [Test]
    public void TestReadJavaDoubleReadsExtremes()
    {
        ClassicAssert.AreEqual(double.MaxValue,
            Bytes(0x7F, 0xEF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF).ReadJavaDouble());
        ClassicAssert.AreEqual(double.MinValue,
            Bytes(0xFF, 0xEF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF).ReadJavaDouble());
        ClassicAssert.AreEqual(double.Epsilon,
            Bytes(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01).ReadJavaDouble());
    }

    [Test]
    public void TestReadJavaDoubleThrowsOnTruncatedInput()
    {
        Assert.Throws<EndOfStreamException>((Action)(() => _ = Bytes(0x3F, 0xE0, 0x00, 0x00).ReadJavaDouble()));
    }

    [Test]
    public void TestReadJavaUTF()
    {
        // Two-byte length prefix, then that many bytes.
        ClassicAssert.AreEqual("abc", Bytes(0x00, 0x03, 0x61, 0x62, 0x63).ReadJavaUTF());
    }

    [Test]
    public void TestReadJavaUTFReadsEmptyString()
    {
        ClassicAssert.AreEqual("", Bytes(0x00, 0x00).ReadJavaUTF());
    }

    [Test]
    public void TestReadJavaUTFReadsTwoAndThreeByteSequences()
    {
        // "café" - the e-acute is two bytes.
        ClassicAssert.AreEqual("café",
            Bytes(0x00, 0x05, 0x63, 0x61, 0x66, 0xC3, 0xA9).ReadJavaUTF());

        // The euro sign is three bytes.
        ClassicAssert.AreEqual("€", Bytes(0x00, 0x03, 0xE2, 0x82, 0xAC).ReadJavaUTF());
    }

    /// <summary>
    /// In modified UTF-8 a NUL is written as the two bytes C0 80, never as a
    /// NUL byte. Decoding with standard UTF-8 would yield U+FFFD here.
    /// </summary>
    [Test]
    public void TestReadJavaUTFReadsEmbeddedNul()
    {
        string value = Bytes(0x00, 0x04, 0x61, 0xC0, 0x80, 0x62).ReadJavaUTF();

        ClassicAssert.AreEqual(3, value.Length);
        ClassicAssert.AreEqual('a', value[0]);
        ClassicAssert.AreEqual('\0', value[1]);
        ClassicAssert.AreEqual('b', value[2]);
    }

    /// <summary>
    /// Characters outside the basic multilingual plane are written as a
    /// surrogate pair with each half encoded separately in three bytes, so a
    /// non-BMP character occupies six bytes rather than the four that standard
    /// UTF-8 would use.
    /// </summary>
    [Test]
    public void TestReadJavaUTFReadsSurrogatePair()
    {
        string value = Bytes(0x00, 0x06, 0xED, 0xA0, 0xBD, 0xED, 0xB8, 0x80).ReadJavaUTF();

        ClassicAssert.AreEqual(2, value.Length);
        ClassicAssert.AreEqual('\ud83d', value[0]);
        ClassicAssert.AreEqual('\ude00', value[1]);
        ClassicAssert.AreEqual(0x1F600, char.ConvertToUtf32(value[0], value[1]));
    }

    [Test]
    public void TestReadJavaUTFReadsSuccessiveValues()
    {
        Stream stream = Bytes(0x00, 0x02, 0x51, 0x4E, 0x00, 0x03, 0x47, 0x49, 0x53);

        ClassicAssert.AreEqual("QN", stream.ReadJavaUTF());
        ClassicAssert.AreEqual("GIS", stream.ReadJavaUTF());
    }

    [Test]
    public void TestReadJavaUTFThrowsOnTruncatedInput()
    {
        // Length prefix itself is incomplete.
        Assert.Throws<EndOfStreamException>((Action)(() => _ = Bytes(0x00).ReadJavaUTF()));

        // Length prefix promises five bytes but only one follows.
        Assert.Throws<EndOfStreamException>((Action)(() => _ = Bytes(0x00, 0x05, 0x61).ReadJavaUTF()));
    }

    [Test]
    public void TestReadJavaUTFThrowsOnMalformedInput()
    {
        // A continuation byte where a leading byte belongs.
        Assert.Throws<FormatException>((Action)(() => _ = Bytes(0x00, 0x01, 0x80).ReadJavaUTF()));

        // A two-byte sequence whose second byte is not a continuation byte.
        Assert.Throws<FormatException>((Action)(() => _ = Bytes(0x00, 0x02, 0xC3, 0x28).ReadJavaUTF()));

        // A two-byte sequence cut short by the declared length.
        Assert.Throws<FormatException>((Action)(() => _ = Bytes(0x00, 0x01, 0xC3).ReadJavaUTF()));

        // A three-byte sequence cut short by the declared length.
        Assert.Throws<FormatException>((Action)(() => _ = Bytes(0x00, 0x02, 0xE2, 0x82).ReadJavaUTF()));
    }

    /// <summary>
    /// A string long enough that the payload arrives over several reads, and
    /// whose length prefix exceeds <see cref="short.MaxValue"/>. The prefix is
    /// unsigned, so a signed read would go negative here and the allocation
    /// would throw rather than the string decoding.
    /// </summary>
    /// <remarks>
    /// NOpenNLP: adapted from J2N's TestDataInputStream, which exercises
    /// lengths near 65535 for the same reason.
    /// </remarks>
    [Test]
    public void TestReadJavaUTFReadsLongString()
    {
        const int Length = 40000;

        byte[] bytes = new byte[Length + 2];
        bytes[0] = (Length >> 8) & 0xFF;
        bytes[1] = Length & 0xFF;
        for (int i = 0; i < Length; i++)
        {
            bytes[i + 2] = (byte)'x';
        }

        ClassicAssert.IsTrue(Length > short.MaxValue);

        string value = Bytes(bytes).ReadJavaUTF();

        ClassicAssert.AreEqual(Length, value.Length);
        ClassicAssert.AreEqual(new string('x', Length), value);
    }

    /// <summary>
    /// Corrupting encoded bytes at random must always end in either a decoded
    /// string or a well-formed exception, never a hang, a stray exception type,
    /// or a message-less throw that tells a caller nothing.
    /// </summary>
    /// <remarks>
    /// NOpenNLP: adapted from J2N's TestDataInputStream.TestReadUTF, which
    /// applies the same corruption strategy. The seed is fixed so a failure can
    /// be reproduced, and the encoder below emits modified UTF-8 rather than
    /// relying on a writer this project does not have.
    /// </remarks>
    [Test]
    public void TestReadJavaUTFSurvivesCorruptedInput()
    {
        Random random = new Random(20260815);

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            int length = random.Next(64) + 1;
            StringBuilder testBuffer = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                testBuffer.Append((char)random.Next(char.MaxValue + 1));
            }

            byte[] testBytes = EncodeJavaUTF(testBuffer.ToString());

            // Corrupt a few bytes at random, then mangle the last two so the
            // input frequently ends in a partial character.
            int corruptions = random.Next(3);
            for (int i = 0; i < corruptions; i++)
            {
                testBytes[random.Next(testBytes.Length)] = (byte)random.Next(256);
            }

            testBytes[^1] = (byte)random.Next(256);
            testBytes[^2] = (byte)random.Next(256);

            try
            {
                Bytes(testBytes).ReadJavaUTF();
            }
            catch (FormatException e)
            {
                ClassicAssert.IsNotNull(e.Message, "vague exception thrown");
                ClassicAssert.IsNotEmpty(e.Message, "vague exception thrown");
            }
            catch (EndOfStreamException)
            {
                // The corruption truncated a sequence; beyond the scope of the test.
            }
        }
    }

    /// <summary>
    /// Encodes a string the way Java's <c>DataOutputStream.writeUTF</c> does,
    /// so the corruption test has well-formed input to start from.
    /// </summary>
    private static byte[] EncodeJavaUTF(string value)
    {
        MemoryStream payload = new MemoryStream();
        foreach (char c in value)
        {
            if (c > 0 && c <= 0x7F)
            {
                payload.WriteByte((byte)c);
            }
            else if (c <= 0x7FF)
            {
                // Includes U+0000, which is written as the two bytes C0 80.
                payload.WriteByte((byte)(0xC0 | ((c >> 6) & 0x1F)));
                payload.WriteByte((byte)(0x80 | (c & 0x3F)));
            }
            else
            {
                // Surrogate halves take this branch individually, which is what
                // makes a non-BMP character six bytes rather than four.
                payload.WriteByte((byte)(0xE0 | ((c >> 12) & 0x0F)));
                payload.WriteByte((byte)(0x80 | ((c >> 6) & 0x3F)));
                payload.WriteByte((byte)(0x80 | (c & 0x3F)));
            }
        }

        byte[] body = payload.ToArray();
        byte[] result = new byte[body.Length + 2];
        result[0] = (byte)((body.Length >> 8) & 0xFF);
        result[1] = (byte)(body.Length & 0xFF);
        Array.Copy(body, 0, result, 2, body.Length);
        return result;
    }

    /// <summary>
    /// The reads are positional, so a mixed sequence must leave the stream
    /// correctly positioned for whatever follows. This is the shape the model
    /// readers actually consume.
    /// </summary>
    [Test]
    public void TestMixedReadsAdvanceTheStreamCorrectly()
    {
        Stream stream = Bytes(
            0x00, 0x03, 0x47, 0x49, 0x53,                       // "GIS"
            0x00, 0x00, 0x00, 0x02,                             // 2
            0x3F, 0xE0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,     // 0.5
            0x00, 0x02, 0x68, 0x69);                            // "hi"

        ClassicAssert.AreEqual("GIS", stream.ReadJavaUTF());
        ClassicAssert.AreEqual(2, stream.ReadJavaInt32());
        ClassicAssert.AreEqual(0.5, stream.ReadJavaDouble());
        ClassicAssert.AreEqual("hi", stream.ReadJavaUTF());
        ClassicAssert.AreEqual(-1, stream.ReadByte());
    }
}
