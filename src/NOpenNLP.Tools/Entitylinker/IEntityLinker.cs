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
using System.IO;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Entitylinker;

/// <summary>
/// EntityLinkers establish connections to external data to enrich extracted
/// entities. For instance, for Location entities a linker can be developed to
/// lookup each found location in a geonames gazateer. Another example may be to
/// find peoples' names and look them up in a database or active directory.
/// Intended to return n best matches for any give search, but can also be
/// implemented as deterministic.
/// </summary>
/// <typeparam name="T">
/// A type that extends <see cref="Span"/>. <see cref="LinkedSpan{T}"/> and <see cref="BaseLink"/>
/// are provided to provide this signature: <c>IEntityLinker&lt;LinkedSpan&lt;BaseLink&gt;&gt;</c>
/// as a default.
/// </typeparam>
public interface IEntityLinker<T> : IEntityLinker
    where T : Span
{
    /// <summary>
    /// Links an entire document of named entities to an external source.
    /// </summary>
    /// <param name="doctext">The full text of the document.</param>
    /// <param name="sentences">The sentence spans of the document.</param>
    /// <param name="tokensBySentence">
    /// A list of tokens spans that correspond to each sentence. The outer array refers to the
    /// sentence, the inner array is the tokens for the outer sentence.
    /// </param>
    /// <param name="namesBySentence">
    /// A list of name spans that correspond to each sentence. The outer array refers to the
    /// sentence, the inner array refers to the tokens that for the same sentence.
    /// </param>
    IList<T> Find(string doctext, Span[] sentences, Span[][] tokensBySentence, Span[][] namesBySentence);

    /// <summary>
    /// Links the names that correspond to the <c>tokens[]</c> spans. The sentence index
    /// can be used to get the sentence text and tokens from the text based on the
    /// sentence and token spans. The text is available for additional context.
    /// </summary>
    /// <param name="doctext">The full text of the document.</param>
    /// <param name="sentences">The sentence spans of the document.</param>
    /// <param name="tokensBySentence">
    /// A list of tokens spans that correspond to each sentence. The outer array refers to the
    /// sentence, the inner array is the tokens for the outer sentence.
    /// </param>
    /// <param name="namesBySentence">
    /// A list of name spans that correspond to each sentence. The outer array refers to the
    /// sentence, the inner array refers to the tokens that for the same sentence.
    /// </param>
    /// <param name="sentenceIndex">
    /// The index to the sentence span that the <c>tokens[]</c> <see cref="Span"/>[] corresponds to.
    /// </param>
    IList<T> Find(string doctext, Span[] sentences, Span[][] tokensBySentence,
        Span[][] namesBySentence, int sentenceIndex);
}

// NOpenNLP-specific: Java's EntityLinkerFactory loads implementations against the raw
// EntityLinker.class token and returns EntityLinker<?>. C# has no raw generic type, so the
// members that do not mention T live on this non-generic base, mirroring how
// IArtifactSerializer is split.
/// <summary>
/// The non-generic base of <see cref="IEntityLinker{T}"/>, used to load and initialize an
/// implementation whose span type is not known statically.
/// </summary>
public interface IEntityLinker
{
    /// <summary>
    /// Allows for passing properties through the <see cref="EntityLinkerFactory"/> into all
    /// impls dynamically. <see cref="IEntityLinker{T}"/> impls should initialize reusable objects
    /// used by the impl in this method. If this is done, any errors will be
    /// captured and thrown by the <see cref="EntityLinkerFactory"/>.
    /// </summary>
    /// <param name="initializationData">
    /// The <see cref="EntityLinkerProperties"/> object that contains properties needed by the
    /// impl, as well as any other objects required for the impl.
    /// </param>
    /// <exception cref="IOException">Thrown if initialization fails.</exception>
    void Init(EntityLinkerProperties initializationData);
}
