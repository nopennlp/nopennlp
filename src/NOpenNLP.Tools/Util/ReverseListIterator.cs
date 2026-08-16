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

using System.Collections;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// An iterator for a list which returns values in the opposite order as the typical list iterator.
/// </summary>
// NOpenNLP: upstream implements java.util.Iterator, where next() both advances
// and returns. IEnumerator splits that into MoveNext() and Current, so the index
// starts one past the first element and MoveNext() steps onto it, which yields
// the same sequence.
public class ReverseListIterator<T> : IEnumerator<T>
{
    private readonly IList<T> list; // NOpenNLP: made readonly

    private int index;

    public ReverseListIterator(IList<T> list)
    {
        this.list = list;
        index = list.Count;
    }

    public T Current => list[index];

    object? IEnumerator.Current => Current;

    // Clamps at -1 so calling MoveNext() past the end stays exhausted rather
    // than walking the index further negative.
    public bool MoveNext() => index >= 0 && --index >= 0;

    public void Reset() => index = list.Count;

    public void Dispose()
    {
    }
}
