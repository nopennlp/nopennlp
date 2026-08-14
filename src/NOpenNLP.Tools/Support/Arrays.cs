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
using J2N.Collections;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Structural array helpers matching the semantics of Java's
/// <c>java.util.Arrays</c>.
/// </summary>
/// <remarks>
/// <see cref="System.Array"/> compares by reference, so Java code relying on
/// <c>Arrays.equals</c> / <c>Arrays.hashCode</c> would silently misbehave if
/// translated directly. These delegate to J2N's structural comparer, which
/// reproduces the Java semantics the ported models depend on.
/// <para/>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// </remarks>
internal static class Arrays
{
    /// <summary>
    /// Returns whether the two arrays are structurally equal, element by
    /// element, as Java's <c>Arrays.equals</c> does.
    /// </summary>
    public static bool Equals<T>(T[]? a, T[]? b) =>
        ArrayEqualityComparer<T>.OneDimensional.Equals(a, b);

    /// <summary>
    /// Returns a hash code derived from the array's contents, as Java's
    /// <c>Arrays.hashCode</c> does.
    /// </summary>
    public static int GetHashCode<T>(T[]? array) =>
        array is null ? 0 : ArrayEqualityComparer<T>.OneDimensional.GetHashCode(array);

    /// <summary>
    /// Returns a copy of <paramref name="original"/> truncated or zero-padded to
    /// <paramref name="newLength"/>, as Java's <c>Arrays.copyOf</c> does.
    /// </summary>
    public static T[] CopyOf<T>(T[] original, int newLength)
    {
        if (original is null)
        {
            throw new ArgumentNullException(nameof(original));
        }

        if (newLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newLength));
        }

        var copy = new T[newLength];
        Array.Copy(original, 0, copy, 0, Math.Min(original.Length, newLength));
        return copy;
    }
}
