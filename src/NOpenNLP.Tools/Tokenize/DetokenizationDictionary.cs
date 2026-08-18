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
using System.IO;
using NOpenNLP.Tools.Dictionary.Serializer;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// Specifies in which direction a token should be moved when detokenizing.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream declares this enum nested inside
/// <see cref="DetokenizationDictionary"/>, as <c>DetokenizationDictionary.Operation</c>.
/// A nested enum would have to be referred to by the same qualified name in C#,
/// but the upstream name <c>Operation</c> is too general for a namespace-level
/// type, so it is hoisted and renamed <see cref="DetokenizationOperationType"/>.
/// </remarks>
public enum DetokenizationOperationType
{
    /// <summary>
    /// Attaches the token to the token on the right side.
    /// </summary>
    MoveRight,

    /// <summary>
    /// Attaches the token to the token on the left side.
    /// </summary>
    MoveLeft,

    /// <summary>
    /// Attaches the token to the token on the left and right sides.
    /// </summary>
    MoveBoth,

    /// <summary>
    /// Attaches the token token to the right token on first occurrence, and
    /// to the token on the left side on the second occurrence.
    /// </summary>
    RightLeftMatching
}

/// <summary>
/// Extension methods for <see cref="DetokenizationOperationType"/>.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream puts <c>parse</c> on the nested enum itself and relies on
/// the Java enum's <c>toString</c> for the serialized form. C# enums cannot
/// declare members, and the C#-cased names would not round-trip through the
/// dictionary XML, so both directions are implemented here against the upstream
/// Java constant names, which are the persisted format.
/// </remarks>
public static class DetokenizationOperationTypeExtensions
{
    private const string MOVE_RIGHT = "MOVE_RIGHT";
    private const string MOVE_LEFT = "MOVE_LEFT";
    private const string MOVE_BOTH = "MOVE_BOTH";
    private const string RIGHT_LEFT_MATCHING = "RIGHT_LEFT_MATCHING";

    /// <summary>
    /// Parses the serialized form of an operation, as written in a
    /// detokenization dictionary.
    /// </summary>
    /// <param name="operation">the serialized operation name, or <c>null</c></param>
    /// <returns>
    /// the parsed operation, or <c>null</c> if the name is not recognized.
    /// </returns>
    public static DetokenizationOperationType? Parse(string? operation)
    {
        if (MOVE_RIGHT.Equals(operation, StringComparison.Ordinal))
        {
            return DetokenizationOperationType.MoveRight;
        }
        else if (MOVE_LEFT.Equals(operation, StringComparison.Ordinal))
        {
            return DetokenizationOperationType.MoveLeft;
        }
        else if (MOVE_BOTH.Equals(operation, StringComparison.Ordinal))
        {
            return DetokenizationOperationType.MoveBoth;
        }
        else if (RIGHT_LEFT_MATCHING.Equals(operation, StringComparison.Ordinal))
        {
            return DetokenizationOperationType.RightLeftMatching;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieves the serialized form of an operation, as written in a
    /// detokenization dictionary. This is the upstream Java constant name, which
    /// is the persisted format, rather than the C# member name.
    /// </summary>
    /// <param name="operation">the operation</param>
    /// <returns>the serialized operation name</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The operation is not a defined <see cref="DetokenizationOperationType"/>.
    /// </exception>
    public static string ToOperationString(this DetokenizationOperationType operation) => operation switch
    {
        DetokenizationOperationType.MoveRight => MOVE_RIGHT,
        DetokenizationOperationType.MoveLeft => MOVE_LEFT,
        DetokenizationOperationType.MoveBoth => MOVE_BOTH,
        DetokenizationOperationType.RightLeftMatching => RIGHT_LEFT_MATCHING,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation!"),
    };
}

public class DetokenizationDictionary
{
    private readonly JCG.Dictionary<string, DetokenizationOperationType> operationTable = new(); // NOpenNLP: made readonly

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="tokens">an array of tokens that should be detokenized according to an operation</param>
    /// <param name="operations">an array of operations which specifies which operation
    ///        should be used for the provided tokens</param>
    public DetokenizationDictionary(string[] tokens, DetokenizationOperationType[] operations)
    {
        if (tokens.Length != operations.Length)
        {
            throw new ArgumentException("tokens and ops must have the same length: tokens=" +
                tokens.Length + ", operations=" + operations.Length + "!");
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            DetokenizationOperationType operation = operations[i];

            // NOpenNLP: the upstream null check on the operation is dropped, as
            // DetokenizationOperationType is a value type and cannot be null.
            if (token is null)
            {
                throw new ArgumentException("token at index " + i + " must not be null!", nameof(tokens));
            }

            operationTable[token] = operation;
        }
    }

    public DetokenizationDictionary(Stream @in)
    {
        Init(@in);
    }

    // NOpenNLP: the upstream File constructor is omitted; a caller opens the
    // file and passes the stream, which is what the Stream constructor above is
    // for. The ported Dictionary does the same.

    /// <exception cref="IOException"/>
    private void Init(Stream @in)
    {
        DictionaryEntryPersistor.Create(@in, entry =>
        {
            // NOpenNLP: upstream reads the attribute off a non-null Attributes;
            // the ported Entry allows a null Attributes, so the lookup is guarded.
            string? operationString = entry.Attributes?.GetValue("operation");

            StringList word = entry.Tokens;

            if (word.Count != 1)
            {
                throw new InvalidFormatException("Each entry must have exactly one token! " + word);
            }

            // parse operation
            DetokenizationOperationType? operation = DetokenizationOperationTypeExtensions.Parse(operationString);

            if (operation is null)
            {
                throw new InvalidFormatException("Unknown operation type: " + operationString);
            }

            operationTable[word.GetToken(0)] = operation.Value;
        });
    }

    /// <summary>
    /// Retrieves the operation for the given token, or <c>null</c> if the token
    /// is not in this dictionary.
    /// </summary>
    /// <remarks>
    /// NOpenNLP: upstream returns <c>null</c> from a <c>Map.get</c> miss. The
    /// enum is a value type in C#, so the miss is expressed as a nullable
    /// return rather than a <see cref="System.Collections.Generic.KeyNotFoundException"/>.
    /// </remarks>
    internal DetokenizationOperationType? GetOperation(string token)
    {
        return operationTable.TryGetValue(token, out DetokenizationOperationType operation) ? operation : null;
    }

    /// <summary>
    /// Writes the current instance to the given <see cref="Stream"/>.
    /// </summary>
    /// <param name="out">the <see cref="Stream"/> to write the dictionary into.</param>
    /// <exception cref="IOException"/>
    public virtual void Serialize(Stream @out)
    {
        // NOpenNLP: upstream builds an anonymous Iterator over the operation
        // table's keys; a C# iterator method expresses the same thing directly.
        using IEnumerator<Entry> entries = CreateEntries();
        DictionaryEntryPersistor.Serialize(@out, entries, false);
    }

    private IEnumerator<Entry> CreateEntries()
    {
        foreach (string token in operationTable.Keys)
        {
            Attributes attributes = new Attributes();
            // NOpenNLP: upstream writes the operation's enum name via toString();
            // the ported enum uses C# member casing, so ToOperationString emits the
            // upstream constant name, which is what a dictionary actually stores.
            attributes.SetValue("operation", operationTable[token].ToOperationString());

            yield return new Entry(new StringList(token), attributes);
        }
    }
}
