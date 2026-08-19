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
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Model;

public class HashSumEventStream(IObjectStream<Event?> eventStream) : AbstractObjectStream<Event?>(eventStream)
{
    // NOpenNLP: upstream catches NoSuchAlgorithmException here, which .NET
    // does not have; MD5.Create is always available.
    private readonly MD5 digest = MD5.Create(); // NOpenNLP: made readonly

    public override Event? Read()
    {
        Event? @event = base.Read();

        if (@event != null)
        {
            // NOpenNLP: Java's MessageDigest.update accumulates across calls.
            // .NET's incremental equivalent on netstandard2.0 is
            // TransformBlock/TransformFinalBlock, so the bytes are fed through it.
            byte[] bytes = Encoding.UTF8.GetBytes(@event.ToString());
            digest.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        return @event;
    }

    /// <summary>
    /// Calculates the hash sum of the stream. The method must be
    /// called after the stream is completely consumed.
    /// </summary>
    /// <returns>the hash sum</returns>
    /// <exception cref="InvalidOperationException">
    /// if the stream is not consumed completely.
    /// </exception>
    public virtual BigInteger CalculateHashSum()
    {
        digest.TransformFinalBlock([], 0, 0);
        byte[] hash = digest.Hash!;

        // NOpenNLP: upstream is `new BigInteger(1, digest.digest())`, which reads
        // the bytes as a big-endian magnitude with an explicit positive sign.
        // .NET's BigInteger(byte[]) is little-endian two's complement, so the
        // bytes are reversed and a zero byte is appended to keep the value
        // positive when the most significant byte has its high bit set.
        byte[] magnitude = new byte[hash.Length + 1];
        for (int i = 0; i < hash.Length; i++)
        {
            magnitude[i] = hash[hash.Length - 1 - i];
        }

        return new BigInteger(magnitude);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            digest.Dispose();
        }

        base.Dispose(disposing);
    }
}
