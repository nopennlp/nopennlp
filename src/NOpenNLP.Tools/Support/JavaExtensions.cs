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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Small shims for Java library behavior the ported OpenNLP source depends on.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// </remarks>
internal static class JavaExtensions
{
    /// <summary>
    /// Associates <paramref name="value"/> with <paramref name="key"/>, replacing
    /// any existing mapping, and returns the previous value as Java's
    /// <c>Map.put</c> does.
    /// </summary>
    /// <remarks>
    /// When the previous value is not needed, assigning through the indexer is
    /// more efficient.
    /// </remarks>
    public static TValue Put<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary is null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        if (!dictionary.TryGetValue(key, out TValue oldValue))
        {
            oldValue = default;
        }

        dictionary[key] = value;
        return oldValue;
    }

    /// <summary>
    /// Copies every entry of <paramref name="collection"/> into
    /// <paramref name="dictionary"/>, as Java's <c>Map.putAll</c> does.
    /// </summary>
    public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> collection)
    {
        if (dictionary is null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        if (collection is null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        foreach (var kvp in collection)
        {
            dictionary[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Writes the exception to standard error, as Java's
    /// <c>Throwable.printStackTrace()</c> does.
    /// </summary>
    public static void PrintStackTrace(this Exception e)
    {
        Console.Error.WriteLine(e.ToString());
    }

    /// <summary>
    /// Returns whether <paramref name="e"/> corresponds to Java's
    /// <c>NoSuchMethodException</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNoSuchMethodException(this Exception e)
    {
        return e is MissingMethodException;
    }

    /// <summary>
    /// Returns whether <paramref name="e"/> corresponds to Java's
    /// <c>InvocationTargetException</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInvocationTargetException(this Exception e)
    {
        return e is TargetInvocationException;
    }

    /// <summary>
    /// Returns whether <paramref name="e"/> corresponds to Java's
    /// <c>InstantiationException</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInstantiationException(this Exception e)
    {
        return e is MissingMethodException
            || e is TypeLoadException
            || e is ReflectionTypeLoadException
            || e is TypeInitializationException;
    }

    /// <summary>
    /// Returns whether <paramref name="e"/> corresponds to Java's
    /// <c>IllegalAccessException</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIllegalAccessException(this Exception e)
    {
        return e is MemberAccessException
            || e is TypeAccessException;
    }
}
