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
using System.Collections.Generic;
using System.IO;
using J2N;
using J2N.Collections.Generic.Extensions;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Leipzig;

internal class SampleShuffleStream<T> : ObjectStreamBase<T?>
    where T : class
{
    private readonly IList<T> bufferedSamples = new JCG.List<T>(); // NOpenNLP: made readonly

    // NOpenNLP: upstream holds a java.util.Iterator that reset() replaces with a fresh
    // one. That is the manual hasNext()/next() advance an IEnumerator<T> expresses;
    // an IEnumerable<T> cannot carry the position across Read calls.
    private IEnumerator<T> sampleIt;

    /// <exception cref="IOException">if there is an error during reading</exception>
    internal SampleShuffleStream(IObjectStream<T?> samples)
    {
        T? sample;
        while ((sample = samples.Read()) != null)
        {
            bufferedSamples.Add(sample);
        }

        // NOpenNLP: upstream shuffles with a fixed seed so its output order is
        // deterministic. J2N's Randomizer reproduces java.util.Random and its Shuffle
        // reproduces Collections.shuffle, so the order matches upstream exactly.
        // System.Random uses a different algorithm and would silently diverge.
        bufferedSamples.Shuffle(new Randomizer(23));

        sampleIt = bufferedSamples.GetEnumerator();
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override T? Read()
    {
        if (sampleIt.MoveNext())
        {
            return sampleIt.Current;
        }

        return null;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    /// <exception cref="NotSupportedException">if reset is not supported on this stream</exception>
    public override void Reset()
    {
        sampleIt.Dispose();
        sampleIt = bufferedSamples.GetEnumerator();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            sampleIt.Dispose();
        }
    }
}
