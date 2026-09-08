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
using System.Globalization;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Compat;

/// <summary>
/// The inference output a runtime got from its own models, recorded so the other
/// runtime can compare against it.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; the mirror of <c>CompatResults.java</c>. Keep the two
/// in sync.
/// <para/>
/// A plain <c>key=value</c> text file, one entry per line, UTF-8, with <c>\n</c>
/// line endings written explicitly so a Windows run and a Linux run produce the
/// same bytes. It is deliberately not a real serialization format: both sides
/// have to parse it with no dependencies beyond their standard library, and a
/// human reading a CI log should be able to see what differed.
/// <para/>
/// Values are joined with <c>|</c> and are always strings. Probabilities are
/// formatted by <see cref="Probability"/> to a fixed number of digits, in the
/// invariant culture, because the last bits of a double are not the contract
/// here: two implementations of the same arithmetic can differ in the final ulp
/// without either being wrong, and a test that failed on that would be noise.
/// Six digits is far tighter than any real behavioural difference and far looser
/// than floating-point jitter.
/// </remarks>
internal sealed class CompatResults
{
    /// <summary>Separator between the elements of a multi-valued entry.</summary>
    public const string Separator = "|";

    // Insertion-ordered: the file is meant to be read by a person, and a diff of
    // two runs is only legible if both list their entries in the same order.
    private readonly JCG.OrderedDictionary<string, string> entries = [];

    /// <summary>The recorded entries, in insertion order.</summary>
    public IDictionary<string, string> Entries => entries;

    /// <summary>Records one entry, failing loudly on a duplicate key.</summary>
    public void Put(string key, string value)
    {
        if (entries.ContainsKey(key))
        {
            throw new InvalidOperationException($"Duplicate result key: {key}");
        }

        entries[key] = value;
    }

    /// <summary>Records a multi-valued entry, joined with <see cref="Separator"/>.</summary>
    public void Put(string key, string[] values) => Put(key, string.Join(Separator, values));

    /// <summary>Records a sequence of probabilities.</summary>
    public void Put(string key, double[] values)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(Separator);
            }

            sb.Append(Probability(values[i]));
        }

        Put(key, sb.ToString());
    }

    /// <summary>
    /// Formats a probability to six decimal places in the invariant culture. See
    /// the class remarks for why the full precision of the double is not the
    /// contract.
    /// </summary>
    public static string Probability(double value) =>
        value.ToString("F6", CultureInfo.InvariantCulture);

    /// <summary>Writes the entries to <paramref name="path"/>, overwriting whatever was there.</summary>
    public void Write(string path)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        var sb = new StringBuilder();
        foreach (var entry in entries)
        {
            // '\n' rather than Environment.NewLine: the file crosses between
            // runtimes and operating systems, so its bytes must not depend on
            // which one wrote it.
            sb.Append(entry.Key).Append('=').Append(entry.Value).Append('\n');
        }

        // No BOM: the Java side reads the file as plain UTF-8 and would take a
        // BOM as part of the first key.
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>Reads entries previously written by <see cref="Write"/>.</summary>
    public static IDictionary<string, string> Read(string path)
    {
        JCG.OrderedDictionary<string, string> result = [];

        foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
        {
            if (line.Length == 0)
            {
                continue;
            }

            int split = line.IndexOf('=');
            if (split < 0)
            {
                throw new IOException($"Malformed line in {path}: {line}");
            }

            result[line[..split]] = line[(split + 1)..];
        }

        return result;
    }
}
