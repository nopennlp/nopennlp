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
    // NOpenNLP: one Option instance per parameter per invocation, cached so
    // ToOption and the value lookup agree on the same instance -- System.CommandLine
    // binds a parsed value to the instance, so a second Option with the same name would
    // read back nothing.
    private static readonly Dictionary<IFormatParameter, Option> options = new Dictionary<IFormatParameter, Option>();

    /// <summary>
    /// Returns the option for <paramref name="parameter"/>, creating it on first use.
    /// </summary>
    internal static Option ToOption(IFormatParameter parameter)
    {
        if (options.TryGetValue(parameter, out Option? existing))
        {
            return existing;
        }

        Option created = Create(parameter);
        options[parameter] = created;
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
            return Build<bool>(parameter);
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
    internal static T? GetValue<T>(ParseResult parseResult, IFormatParameter parameter) =>
        parseResult.GetValue((Option<T>)ToOption(parameter));
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
