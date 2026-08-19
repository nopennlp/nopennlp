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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Round-trip tests for <see cref="DataOutput"/> against <see cref="DataInput"/>.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. The binary model
/// writers depend on these reproducing Java's <c>DataOutputStream</c> layout
/// exactly, and <see cref="Ml.Model.TwoPassDataIndexer"/> writes its temporary
/// event file through them and reads it back in the same pass.
/// </remarks>
[NOpenNLPSpecific]
public class DataOutputTest
{
    [Test]
    public void TestInt32RoundTrip()
    {
        foreach (int value in new[] { 0, 1, -1, 42, -42, 123456, int.MinValue, int.MaxValue })
        {
            using var stream = new MemoryStream();
            stream.WriteJavaInt32(value);
            stream.Position = 0;
            ClassicAssert.AreEqual(value, stream.ReadJavaInt32());
        }
    }

    [Test]
    public void TestInt32IsBigEndian()
    {
        // Java's DataOutputStream.writeInt is big-endian; BinaryWriter would emit
        // these four bytes in the opposite order.
        using var stream = new MemoryStream();
        stream.WriteJavaInt32(0x01020304);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, stream.ToArray());
    }

    [Test]
    public void TestDoubleRoundTripPreservesBits()
    {
        var random = new Random(7);
        for (int i = 0; i < 20000; i++)
        {
            long bits = ((long)random.Next(int.MinValue, int.MaxValue) << 32)
                | (uint)random.Next(int.MinValue, int.MaxValue);
            double value = BitConverter.Int64BitsToDouble(bits);

            using var stream = new MemoryStream();
            stream.WriteJavaDouble(value);
            stream.Position = 0;

            ClassicAssert.AreEqual(bits, BitConverter.DoubleToInt64Bits(stream.ReadJavaDouble()));
        }
    }

    [Test]
    public void TestSingleRoundTripPreservesBits()
    {
        var random = new Random(11);
        for (int i = 0; i < 20000; i++)
        {
            byte[] raw = BitConverter.GetBytes(random.Next(int.MinValue, int.MaxValue));
            float value = BitConverter.ToSingle(raw, 0);

            using var stream = new MemoryStream();
            stream.WriteJavaSingle(value);
            stream.Position = 0;

            CollectionAssert.AreEqual(raw, BitConverter.GetBytes(stream.ReadJavaSingle()));
        }
    }

    [Test]
    public void TestUtfRoundTrip()
    {
        string[] values =
        [
            "",
            "plain",
            "w=he ic",
            "café",              // two-byte sequence
            "\uD83D\uDE00 emoji",     // surrogate pair, encoded one half at a time
            "nul\u0000inside",        // U+0000 becomes two bytes, never a NUL byte
            new string('x', 20000),
        ];

        foreach (string value in values)
        {
            using var stream = new MemoryStream();
            stream.WriteJavaUTF(value);
            stream.Position = 0;
            ClassicAssert.AreEqual(value, stream.ReadJavaUTF());
        }
    }

    [Test]
    public void TestUtfEncodesNulAsTwoBytes()
    {
        // Modified UTF-8 writes U+0000 as C0 80 so that no NUL byte appears inside
        // the encoded form. The two length bytes come first.
        using var stream = new MemoryStream();
        stream.WriteJavaUTF("\u0000");
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x02, 0xC0, 0x80 }, stream.ToArray());
    }

    [Test]
    public void TestUtfRejectsStringLongerThan65535Bytes()
    {
        using var stream = new MemoryStream();

        // The length prefix is an unsigned 16-bit byte count, so anything longer
        // cannot be represented. Java throws UTFDataFormatException here.
        Assert.Throws<FormatException>((Action)(() => stream.WriteJavaUTF(new string('x', 70000))));

        // Nothing may be written when the length check fails, or the caller would
        // be left with a truncated record.
        ClassicAssert.AreEqual(0, stream.Length);
    }
}
