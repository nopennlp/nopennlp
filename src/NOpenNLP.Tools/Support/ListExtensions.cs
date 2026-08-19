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
    /// <remarks>
    /// The sort is <i>stable</i>: elements that compare equal keep their relative
    /// order. Java guarantees this and callers depend on it, but
    /// <see cref="List{T}.Sort()"/> does not provide it, so it cannot be used here.
    /// See <see cref="Sort{T}(IList{T}, IComparer{T})"/>.
    /// </remarks>
    public static void Sort<T>(this IList<T> list)
        => Sort(list, Comparer<T>.Default);

    /// <summary>
    /// Sorts <paramref name="list"/> in place using <paramref name="comparer"/>, as
    /// Java's <c>Collections.sort(List, Comparator)</c> does.
    /// </summary>
    /// <remarks>
    /// <para/>The sort is <i>stable</i>: elements that compare equal keep their
    /// relative order.
    /// <para/>NOpenNLP: Java's <c>Collections.sort</c> and <c>Arrays.sort(Object[])</c>
    /// are specified to be stable, and ported code relies on it. The model writers
    /// group runs of equal-comparing predicates and write one entry per run, so an
    /// unstable sort would reorder the predicate names within a run and change the
    /// bytes of the model file. <see cref="List{T}.Sort()"/> is an introsort and is
    /// explicitly documented as unstable, so a merge sort is used instead.
    /// </remarks>
    public static void Sort<T>(this IList<T> list, IComparer<T> comparer)
    {
        if (list is null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (comparer is null)
        {
            throw new ArgumentNullException(nameof(comparer));
        }

        int count = list.Count;
        if (count < 2)
        {
            return;
        }

        var source = new T[count];
        list.CopyTo(source, 0);
        var buffer = new T[count];
        MergeSort(source, buffer, 0, count, comparer);

        for (int i = 0; i < count; i++)
        {
            list[i] = source[i];
        }
    }

    private static void MergeSort<T>(T[] source, T[] buffer, int start, int end, IComparer<T> comparer)
    {
        if (end - start < 2)
        {
            return;
        }

        int middle = start + ((end - start) / 2);
        MergeSort(source, buffer, start, middle, comparer);
        MergeSort(source, buffer, middle, end, comparer);

        // Already ordered: the largest of the left run is at most the smallest of
        // the right run, so the merge would copy the two runs back unchanged.
        if (comparer.Compare(source[middle - 1], source[middle]) <= 0)
        {
            return;
        }

        int i = start, j = middle, k = start;
        while (i < middle && j < end)
        {
            // <= 0 takes from the left run first, which is what makes this stable.
            buffer[k++] = comparer.Compare(source[i], source[j]) <= 0 ? source[i++] : source[j++];
        }

        while (i < middle)
        {
            buffer[k++] = source[i++];
        }

        while (j < end)
        {
            buffer[k++] = source[j++];
        }

        Array.Copy(buffer, start, source, start, end - start);
    }
}
