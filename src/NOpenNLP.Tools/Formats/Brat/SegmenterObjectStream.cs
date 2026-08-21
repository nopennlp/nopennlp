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

using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Brat;

/// <summary>
/// An <see cref="IObjectStream{T}"/> which splits each incoming sample into zero or
/// more outgoing samples, emitting them one at a time.
/// </summary>
/// <typeparam name="S">the type of the source/input stream</typeparam>
/// <typeparam name="T">the type of this stream</typeparam>
// NOpenNLP: both type parameters are constrained to reference types because Read()
// signals end of stream with null on the output side and tests the input sample
// against null, neither of which a value type could express.
public abstract class SegmenterObjectStream<S, T>(IObjectStream<S?> @in)
    : FilterObjectStream<S?, T?>(@in)
    where S : class
    where T : class
{
    // NOpenNLP: upstream holds a java.util.Iterator, advancing it across hasNext()/next().
    // An IEnumerator is the C# counterpart for that manual advancing; it is disposed and
    // replaced whenever a new batch of samples arrives.
    private IEnumerator<T> sampleIt = System.Linq.Enumerable.Empty<T>().GetEnumerator();

    /// <summary>
    /// Splits the given <paramref name="sample"/> into the samples this stream emits.
    /// </summary>
    /// <param name="sample">the incoming sample to segment</param>
    /// <returns>the outgoing samples, or <c>null</c> if the sample produced none</returns>
    /// <exception cref="IOException">if there is an error during reading</exception>
    protected abstract IList<T>? Read(S sample);

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public sealed override T? Read()
    {
        if (sampleIt.MoveNext())
        {
            return sampleIt.Current;
        }
        else
        {
            var inSample = samples.Read();

            if (inSample != null)
            {
                var outSamples = Read(inSample);

                if (outSamples != null)
                {
                    sampleIt.Dispose();
                    sampleIt = outSamples.GetEnumerator();
                }

                return Read();
            }
        }

        return null;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        sampleIt.Dispose();

        base.Dispose(disposing);
    }
}
