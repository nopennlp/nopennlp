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

namespace NOpenNLP.Tools.Util;

public class ObjectStreamUtils
{
    /// <summary>
    /// Creates an <see cref="IObjectStream{T}"/> from an array.
    /// </summary>
    /// <param name="array">the array to stream over</param>
    /// <returns>the object stream over the array elements</returns>
    public static IObjectStream<T?> CreateObjectStream<T>(params T[] array)
        where T : class =>
        new ArrayObjectStream<T>(array);

    // NOpenNLP: upstream also overloads createObjectStream and
    // concatenateObjectStream for a Collection. C# binds an ICollection<T>
    // argument to the varargs overload with T bound to the collection type --
    // yielding a one-element stream of the collection itself rather than of its
    // elements -- so those overloads are omitted rather than renamed. Use
    // `new CollectionObjectStream<T>(collection)` and
    // `ConcatenateObjectStream(collection.ToArray())` instead.

    /// <summary>
    /// Creates a single concatenated <see cref="IObjectStream{T}"/> from multiple individual
    /// <see cref="IObjectStream{T}"/>s with the same type.
    /// </summary>
    public static IObjectStream<T?> ConcatenateObjectStream<T>(params IObjectStream<T?>[] streams)
        where T : class
    {
        foreach (IObjectStream<T?> stream in streams)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(streams), "stream cannot be null");
            }
        }

        return new ConcatenatedObjectStream<T>(streams);
    }

    // NOpenNLP: upstream returns anonymous inner classes from each factory
    // method; C# has no equivalent, so each becomes a private nested type.
    private sealed class ArrayObjectStream<T> : ObjectStreamBase<T?>
        where T : class
    {
        private readonly T[] array;

        private int index;

        internal ArrayObjectStream(T[] array)
        {
            this.array = array;
        }

        public override T? Read() => index < array.Length ? array[index++] : null;

        public override void Reset() => index = 0;
    }

    // Backs both concatenate overloads: the varargs form differs from the
    // collection form upstream only in indexing an array instead of an
    // iterator, which an IEnumerable covers for both.
    private sealed class ConcatenatedObjectStream<T> : ObjectStreamBase<T?>
        where T : class
    {
        private readonly ICollection<IObjectStream<T?>> streams;

        private IEnumerator<IObjectStream<T?>> iterator;

        private IObjectStream<T?>? currentStream;

        internal ConcatenatedObjectStream(ICollection<IObjectStream<T?>> streams)
        {
            this.streams = streams;
            iterator = streams.GetEnumerator();
            currentStream = iterator.MoveNext() ? iterator.Current : null;
        }

        public override T? Read()
        {
            T? @object = null;

            while (currentStream != null && @object == null)
            {
                @object = currentStream.Read();
                if (@object == null)
                {
                    currentStream = iterator.MoveNext() ? iterator.Current : null;
                }
            }

            return @object;
        }

        public override void Reset()
        {
            foreach (IObjectStream<T?> stream in streams)
            {
                stream.Reset();
            }

            iterator.Dispose();
            iterator = streams.GetEnumerator();
            currentStream = iterator.MoveNext() ? iterator.Current : null;
        }

        public override void Close()
        {
            foreach (IObjectStream<T?> stream in streams)
            {
                stream.Close();
            }

            iterator.Dispose();
        }
    }
}
