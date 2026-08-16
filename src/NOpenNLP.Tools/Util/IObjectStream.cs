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
using System.IO;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Reads <c>Object</c>s from a stream.
/// <para/>
/// Design Decision:<br/>
/// This interface provides a means for iterating over the
/// objects in a stream, it does not implement <see cref="System.Collections.Generic.IEnumerator{T}"/> or
/// <see cref="System.Collections.Generic.IEnumerable{T}"/> because:
/// <list type="bullet">
/// <item><description><c>Iterator.next()</c> and <c>Iterator.hasNext()</c> are declared as throwing no
/// checked exceptions. Thus the <see cref="IOException"/>s thrown by <see cref="Read"/> would
/// have to be wrapped in runtime exceptions, and the compiler would be
/// unable to force users of this code to catch such exceptions.</description></item>
/// <item><description>Implementing an enumerable would mean either silently calling
/// <see cref="Reset"/> to guarantee that all items were always seen on each
/// iteration, or documenting that it only iterates over the remaining
/// elements of the <see cref="IObjectStream{T}"/>. In either case, users not reading the
/// documentation carefully might run into unexpected behavior.</description></item>
/// </list>
/// </summary>
// NOpenNLP: upstream extends AutoCloseable, whose close() maps onto IDisposable
// so C# `using` works.
public interface IObjectStream<out T> : IDisposable
{
    /// <summary>
    /// Returns the next object. Calling this method repeatedly until it returns
    /// <c>null</c> will return each object from the underlying source exactly once.
    /// </summary>
    /// <returns>the next object or <c>null</c> to signal that the stream is exhausted</returns>
    /// <exception cref="IOException">if there is an error during reading</exception>
    T Read();

    /// <summary>
    /// Repositions the stream at the beginning and the previously seen object sequence
    /// will be repeated exactly. This method can be used to re-read
    /// the stream if multiple passes over the objects are required.
    ///
    /// The implementation of this method is optional.
    /// </summary>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    /// <exception cref="NotSupportedException">if reset is not supported on this stream</exception>
    // NOpenNLP: upstream declares Reset() and Close() as Java 8 default methods;
    // C# default interface implementations are unavailable on netstandard2.0, so
    // implementors supply the bodies. ObjectStreamBase provides the upstream
    // defaults for implementors that do not need to override them.
    void Reset();

    // NOpenNLP: omitted Close in favor of IDisposable.Dispose
}
