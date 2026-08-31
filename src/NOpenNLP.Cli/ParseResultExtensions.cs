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

using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Reads a tool's option values from a parse result, resolving by option name when the
/// tool's own <see cref="Option"/> instance is not the one registered on the command.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// <para/>
/// A tool and the corpus format it reads can declare the same option: every trainer
/// declares <c>-lang</c>, and so do the AD, CoNLL and OntoNotes formats. Upstream parses
/// one flat <c>string[]</c>, so the two declarations are just two views of the same
/// argument text and both readers see the user's value. System.CommandLine instead binds
/// a parsed value to an Option <b>instance</b>, so only one instance per name may be
/// registered; the format's instance wins, because the factory reads back through it.
/// <para/>
/// That left the tool holding an instance the command never saw, and
/// <c>ParseResult.GetRequiredValue</c> on it reported the option as missing even though
/// the user had supplied it -- <c>ChunkerTrainerME.ad -lang pt ...</c> aborted with
/// "-lang is required but was not provided". These helpers fall back to the by-name
/// lookup, which finds whichever instance was actually registered.
/// </remarks>
internal static class ParseResultExtensions
{
    /// <summary>
    /// Reads the value of <paramref name="option"/>, falling back to a lookup by name
    /// when the command carries a different instance of the same option.
    /// </summary>
    public static T? GetValueByName<T>(this ParseResult parseResult, Option<T> option) =>
        IsRegistered(parseResult, option)
            ? parseResult.GetValue(option)
            : parseResult.GetValue<T>(option.Name);

    /// <summary>
    /// Reads the value of a required <paramref name="option"/>, falling back to a lookup
    /// by name when the command carries a different instance of the same option.
    /// </summary>
    public static T GetRequiredValueByName<T>(this ParseResult parseResult, Option<T> option)
    {
        if (IsRegistered(parseResult, option))
        {
            return parseResult.GetRequiredValue(option);
        }

        T? value = parseResult.GetValue<T>(option.Name);

        // The registered instance carries the Required flag, so the parser has already
        // rejected a missing value before the action runs. This guards the case where a
        // format declares the option as optional while the tool requires it.
        if (value is null)
        {
            throw new InvalidOperationException(
                $"{option.Name} is required but was not provided.");
        }

        return value;
    }

    private static bool IsRegistered<T>(ParseResult parseResult, Option<T> option)
    {
        for (CommandResult? result = parseResult.CommandResult; result is not null;
            result = result.Parent as CommandResult)
        {
            if (result.Command.Options.Any(registered => ReferenceEquals(registered, option)))
            {
                return true;
            }
        }

        return false;
    }
}
