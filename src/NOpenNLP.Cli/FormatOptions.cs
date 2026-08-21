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
using System.CommandLine;
using System.IO;
using NOpenNLP.Tools.Formats;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Translates a format's <see cref="IFormatParameter"/> descriptors into
/// System.CommandLine options, and reads their parsed values back.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. It is the seam that
/// keeps NOpenNLP.Tools free of a System.CommandLine dependency: the library describes
/// what a format accepts, and this turns that description into options and back into
/// values. Upstream needs no equivalent because its formats package imports
/// <c>ArgumentParser</c> from the command line package directly.
/// </remarks>
internal static class FormatOptions
{
    // NOpenNLP: ToOption must return the same Option instance for a given parameter
    // within one invocation, because System.CommandLine binds a parsed value to the
    // instance -- a second Option with the same name would read back nothing.
    //
    // The cache is per invocation rather than static. Descriptors are shared: every
    // text-based format declares FormatParameters.Data, so a process-wide cache would
    // hand the same Option instance to more than one Command. That is harmless for the
    // real CLI, which builds one command and exits, but the tests drive CLI.Run many
    // times in one process, where a value parsed for one tool would still be bound to
    // the instance the next tool adds to its command. NewScope() is called once per
    // command construction; Current is what ToOption and the value lookup share.
    [ThreadStatic]
    private static Dictionary<IFormatParameter, Option>? current;

    private static Dictionary<IFormatParameter, Option> Current =>
        current ??= new Dictionary<IFormatParameter, Option>();

    /// <summary>
    /// Starts a fresh set of options, discarding any bound to a previous invocation.
    /// </summary>
    internal static void NewScope() => current = new Dictionary<IFormatParameter, Option>();

    /// <summary>
    /// Returns the option for <paramref name="parameter"/>, creating it on first use
    /// within the current scope.
    /// </summary>
    internal static Option ToOption(IFormatParameter parameter)
    {
        if (Current.TryGetValue(parameter, out Option? existing))
        {
            return existing;
        }

        Option created = Create(parameter);
        Current[parameter] = created;
        return created;
    }

    private static Option Create(IFormatParameter parameter)
    {
        if (parameter.ValueType == typeof(string))
        {
            return Build<string>(parameter);
        }

        if (parameter.ValueType == typeof(int))
        {
            return Build<int>(parameter);
        }

        if (parameter.ValueType == typeof(bool))
        {
            // NOpenNLP: declared as Option<string> so it accepts what
            // Boolean.parseBoolean does. System.CommandLine validates a bool token
            // itself, before any parser of ours runs, so an Option<bool> rejects
            // `-includeTitles 0` -- which upstream reads as false -- with exit 1.
            // GetValue<bool> below interprets the string the way Java does.
            var option = new Option<string?>(parameter.Name)
            {
                Description = parameter.Description,
                HelpName = parameter.ValueName,
                Required = !parameter.IsOptional,
            };

            bool defaultValue = parameter.DefaultValue is bool value && value;
            option.DefaultValueFactory = _ => defaultValue ? "true" : "false";

            return option;
        }

        if (parameter.ValueType == typeof(FileInfo))
        {
            return Build<FileInfo>(parameter);
        }

        throw new NotSupportedException(
            "Unsupported format parameter type: " + parameter.ValueType);
    }

    private static Option<T> Build<T>(IFormatParameter parameter)
    {
        var option = new Option<T>(parameter.Name)
        {
            Description = parameter.Description,
            HelpName = parameter.ValueName,
            Required = !parameter.IsOptional,
        };

        if (parameter.IsOptional && parameter.DefaultValue is not null)
        {
            T defaultValue = (T)parameter.DefaultValue;
            option.DefaultValueFactory = _ => defaultValue;
        }

        return option;
    }

    /// <summary>
    /// Reads the parsed value of <paramref name="parameter"/> from
    /// <paramref name="parseResult"/>.
    /// </summary>
    internal static T? GetValue<T>(ParseResult parseResult, IFormatParameter parameter)
    {
        // A boolean parameter is carried by an Option<string> (see Create), so its value
        // is interpreted here the way Java's Boolean.parseBoolean does.
        if (typeof(T) == typeof(bool))
        {
            string? raw = parseResult.GetValue((Option<string?>)ToOption(parameter));
            object parsed = ToolParams.JavaBooleanValue(raw);
            return (T)parsed;
        }

        return parseResult.GetValue((Option<T>)ToOption(parameter));
    }
}

/// <summary>
/// Supplies a factory's parameter values from a System.CommandLine parse result.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. It stands in for the
/// dynamic proxy <c>ArgumentParser.parse</c> returns upstream.
/// </remarks>
internal sealed class ParseResultParameterValues(ParseResult parseResult) : IFormatParameterValues
{
    private readonly ParseResult parseResult = parseResult;

    /// <inheritdoc/>
    public T? Get<T>(IFormatParameter parameter) => FormatOptions.GetValue<T>(parseResult, parameter);
}
