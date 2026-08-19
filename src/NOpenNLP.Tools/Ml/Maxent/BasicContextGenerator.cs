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

namespace NOpenNLP.Tools.Ml.Maxent;

/// <summary>
/// Generates contexts for maxent decisions, assuming that the input given to the
/// <see cref="GetContext"/> method is a string containing contextual predicates
/// separated by spaces, e.g.
/// <para/>
/// cp_1 cp_2 ... cp_n
/// </summary>
public class BasicContextGenerator(string separator) : IContextGenerator<string>
{
    public BasicContextGenerator()
        : this(" ")
    {
    }

    /// <summary>
    /// Builds up the list of contextual predicates given a string.
    /// </summary>
    // NOpenNLP: upstream is String.split(separator), which treats the separator as a
    // regular expression and, unlike .NET, drops trailing empty strings. The separators
    // in use are literal, so a literal split matches, but the trailing empties are
    // trimmed here to keep the returned array the same length as upstream's.
    public virtual string[] GetContext(string o)
    {
        string[] parts = o.Split([separator], StringSplitOptions.None);

        int length = parts.Length;
        while (length > 0 && parts[length - 1].Length == 0)
        {
            length--;
        }

        if (length == parts.Length)
        {
            return parts;
        }

        string[] trimmed = new string[length];
        Array.Copy(parts, trimmed, length);
        return trimmed;
    }
}
