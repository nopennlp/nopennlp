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
using System.Linq;
using NOpenNLP.Tools.Ml.Model;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Base class for <see cref="IObjectStream{T}"/>s which turn samples into
/// training <see cref="Event"/>s.
/// </summary>
/// <typeparam name="T">the type of the samples the events are created from</typeparam>
public abstract class AbstractEventStream<T> : ObjectStreamBase<Event?>
    where T : class
{
    private readonly IObjectStream<T?> samples;

    private IEnumerator<Event> events = Enumerable.Empty<Event>().GetEnumerator();

    // NOpenNLP: upstream tracks the iterator with hasNext(); IEnumerator has no
    // such lookahead, so the pending element from the last MoveNext() is held
    // here instead. It is null exactly when upstream's hasNext() is false.
    private Event? pending;

    /// <summary>
    /// Initializes the current instance with a sample stream.
    /// </summary>
    /// <param name="samples">the sample stream.</param>
    protected AbstractEventStream(IObjectStream<T?> samples) => this.samples = samples;

    /// <summary>
    /// Creates events for the provided sample.
    /// </summary>
    /// <param name="sample">the sample for which training <see cref="Event"/>s
    /// are be created.</param>
    /// <returns>a sequence of training events, or an empty sequence.</returns>
    /// <remarks>
    /// NOpenNLP: upstream returns an <c>Iterator&lt;Event&gt;</c>. This returns
    /// <see cref="IEnumerable{T}"/> instead, which is the more usual .NET shape and
    /// spares implementers from handing back an enumerator the caller must dispose.
    /// <see cref="Read"/> enumerates it manually, since it yields one event per call.
    /// </remarks>
    protected abstract IEnumerable<Event> CreateEvents(T sample);

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public sealed override Event? Read()
    {
        // NOpenNLP specific: tail-recursive call replaced with loop
        while (true)
        {
            if (pending != null)
            {
                Event next = pending;
                pending = events.MoveNext() ? events.Current : null;
                return next;
            }

            while (pending == null && samples.Read() is { } sample)
            {
                events = CreateEvents(sample).GetEnumerator();
                pending = events.MoveNext() ? events.Current : null;
            }

            if (pending != null)
            {
                continue;
            }

            return null;
        }
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        events = Enumerable.Empty<Event>().GetEnumerator();
        pending = null;
        samples.Reset();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing) => samples.Dispose();
}
