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

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// Class which models a sequence.
/// </summary>
/// <typeparam name="T">The type of the object which is the source of this sequence.</typeparam>
public class Sequence<T>
{
    private readonly Event[] events; // NOpenNLP: made readonly
    private readonly T source; // NOpenNLP: made readonly

    /// <summary>
    /// Creates a new sequence made up of the specified events and derived from the
    /// specified source.
    /// </summary>
    /// <param name="events">The events of the sequence.</param>
    /// <param name="source">The source object for this sequence.</param>
    public Sequence(Event[] events, T source)
    {
        this.events = events;
        this.source = source;
    }

    /// <summary>
    /// Gets the events which make up this sequence.
    /// </summary>
    public virtual Event[] Events => events;

    /// <summary>
    /// Gets an object from which this sequence can be derived. This object is
    /// used when the events for this sequence need to be re-derived such as in a
    /// call to <see cref="ISequenceStream{T}.UpdateContext"/>.
    /// </summary>
    public virtual T Source => source;
}
