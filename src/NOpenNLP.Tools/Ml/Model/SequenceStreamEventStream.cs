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
using System.Linq;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// Class which turns a sequence stream into an event stream.
/// </summary>
/// <typeparam name="T">The type of the object which is the source of each sequence.</typeparam>
public class SequenceStreamEventStream<T>(ISequenceStream<T> sequenceStream) : ObjectStreamBase<Event?>
{
    // NOpenNLP: this is one of the cases where IEnumerator<T> is the right type
    // rather than IEnumerable<T>: Read() hands back one event per call and has to
    // remember its position across calls, which is exactly the manual control over
    // advancing that an enumerator provides.
    private IEnumerator<Event> eventIt = Enumerable.Empty<Event>().GetEnumerator();

    public override Event? Read()
    {
        // NOpenNLP: Java's Iterator.hasNext() peeks without advancing, while
        // IEnumerator.MoveNext() advances and reports in one call, so the loop
        // condition becomes "could not advance" and the value is read from Current.
        while (!eventIt.MoveNext())
        {
            var sequence = sequenceStream.Read();
            if (sequence == null)
            {
                return null;
            }

            eventIt.Dispose();
            eventIt = ((IEnumerable<Event>)sequence.Events).GetEnumerator();
        }

        return eventIt.Current;
    }

    public override void Reset()
    {
        eventIt.Dispose();
        eventIt = Enumerable.Empty<Event>().GetEnumerator();
        sequenceStream.Reset();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            eventIt.Dispose();
            eventIt = Enumerable.Empty<Event>().GetEnumerator();
            sequenceStream.Dispose();
        }
    }
}
