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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NOpenNLP.Tools.Util.Wordvector;

/// <summary>
/// A table that maps tokens to word vectors.
/// <para/>
/// Warning: Experimental new feature, see OPENNLP-1144 for details, the API might be changed anytime.
/// </summary>
/// <remarks>
/// NOpenNLP: <c>tokens()</c> method replaced with <see cref="IEnumerable{T}"/> implementation.
/// </remarks>
[Experimental("NONLPEXP0001")]
public interface IWordVectorTable : IEnumerable<string>
{
    // NOpenNLP: Java's Map.get returns null for an absent token; this returns null likewise
    // rather than throwing, so callers see the same behavior.
    /// <summary>
    /// Gets the vector for <paramref name="token"/>, or <c>null</c> if the token is not in the table.
    /// </summary>
    IWordVector? Get(string token);

    /// <summary>
    /// Gets the number of tokens in this table.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the dimension of the vectors in this table, or <c>-1</c> if the table is empty.
    /// </summary>
    int Dimension { get; }
}
