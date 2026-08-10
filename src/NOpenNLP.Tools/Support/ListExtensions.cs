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
using System.Collections.Generic;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// <see cref="IList{T}"/> operations that Java exposes on <c>java.util.List</c>
/// but .NET defines only on the concrete <see cref="List{T}"/>.
/// </summary>
/// <remarks>
/// The ported code holds its collections as <see cref="IList{T}"/>, mirroring
/// Java's use of the <c>List</c> interface, so these keep those call sites
/// compiling without widening the declared types.
/// <para/>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// </remarks>
internal static class ListExtensions
{
    /// <summary>
    /// Appends every element of <paramref name="collection"/> to
    /// <paramref name="list"/>, as Java's <c>List.addAll</c> does.
    /// </summary>
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection)
    {
        if (list is null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (collection is null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (list is List<T> concrete)
        {
            concrete.AddRange(collection);
            return;
        }

        foreach (var item in collection)
        {
            list.Add(item);
        }
    }

    /// <summary>
    /// Sorts <paramref name="list"/> in place using the default comparer, as
    /// Java's <c>Collections.sort</c> does.
    /// </summary>
    public static void Sort<T>(this IList<T> list)
    {
        if (list is null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (list is List<T> concrete)
        {
            concrete.Sort();
            return;
        }

        var buffer = new List<T>(list);
        buffer.Sort();
        for (int i = 0; i < buffer.Count; i++)
        {
            list[i] = buffer[i];
        }
    }
}
