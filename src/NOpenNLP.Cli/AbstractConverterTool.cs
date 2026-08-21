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
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Base class for tools which convert a foreign corpus format into the native OpenNLP
/// format, writing the converted samples to standard output.
/// </summary>
public abstract class AbstractConverterTool<T> : TypedCmdLineTool
{
    /// <inheritdoc/>
    protected override IEnumerable<string> GetFormatNames() =>
        StreamFactoryRegistry.GetFactories<T>().Keys;

    /// <inheritdoc/>
    protected override IEnumerable<IFormatParameter>? GetFormatParameters(string format) =>
        StreamFactoryRegistry.GetFactory<T>(format)?.Parameters;

    /// <inheritdoc/>
    // NOpenNLP: upstream builds this from a HashMap's key order, so the format list is in
    // an unspecified order that can differ between JVM runs. The ported registry is
    // insertion-ordered, so this is stable and follows registration order.
    public override string ShortDescription
    {
        get
        {
            IReadOnlyDictionary<string, IObjectStreamFactory<T>> factories =
                StreamFactoryRegistry.GetFactories<T>();

            List<string> foreign = factories.Keys
                .Where(f => !StreamFactoryRegistry.DefaultFormat.Equals(f, StringComparison.Ordinal))
                .ToList();

            if (2 == factories.Count)
            {
                // opennlp + foreign
                return "converts " + string.Concat(foreign) + " data format to native OpenNLP format";
            }
            else if (2 < factories.Count)
            {
                return "converts foreign data formats (" + string.Join(",", foreign) +
                    ") to native OpenNLP format";
            }
            else
            {
                throw new InvalidOperationException(
                    "There should be more than 1 factory registered for converter tool");
            }
        }
    }

    private string CreateHelpString(string format, string usage) =>
        "Usage: " + CLI.Cmd + " " + Name + " " + format + " " + usage;

    /// <inheritdoc/>
    public override string GetHelp()
    {
        var help = new StringBuilder("help|");

        foreach (string formatName in GetFormatNames())
        {
            if (!StreamFactoryRegistry.DefaultFormat.Equals(formatName, StringComparison.Ordinal))
            {
                help.Append(formatName).Append('|');
            }
        }

        return CreateHelpString(help.ToString(0, help.Length - 1), "[help|options...]");
    }

    /// <inheritdoc/>
    public override string GetHelp(string format) => GetHelp();

    /// <inheritdoc/>
    // NOpenNLP: a converter takes the format as its first POSITIONAL argument --
    // `nopennlp TokenizerConverter conllu -data x` -- rather than as the .format suffix
    // the other typed tools use, so the command carries a format argument and the
    // selected format's options are added once it is known.
    public override Command CreateCommand(string commandName)
    {
        var formatArgument = new Argument<string?>("format")
        {
            Description = "the format to convert from",
            Arity = ArgumentArity.ZeroOrOne,
        };

        var remaining = new Argument<string[]>("args")
        {
            Description = "the format's options",
            Arity = ArgumentArity.ZeroOrMore,
        };

        var command = new Command(commandName, ShortDescription);
        command.Arguments.Add(formatArgument);
        command.Arguments.Add(remaining);

        command.SetAction(parseResult =>
        {
            string? format = parseResult.GetValue(formatArgument);

            if (format is null)
            {
                Console.WriteLine(GetHelp());
                return 0;
            }

            IObjectStreamFactory<T>? streamFactory = StreamFactoryRegistry.GetFactory<T>(format);

            if (streamFactory is null)
            {
                throw new TerminateToolException(1, "Format " + format + " is not found.\n" + GetHelp());
            }

            string[] formatArgs = parseResult.GetValue(remaining) ?? [];

            string helpString = CreateHelpString(format, CreateUsage(streamFactory.Parameters));

            if (0 == formatArgs.Length
                || (1 == formatArgs.Length && "help".Equals(formatArgs[0], StringComparison.Ordinal)))
            {
                Console.WriteLine(helpString);
                return 0;
            }

            // Parse the format's own options out of the remaining arguments.
            var formatCommand = new Command(format);
            foreach (IFormatParameter parameter in streamFactory.Parameters)
            {
                formatCommand.Options.Add(FormatOptions.ToOption(parameter));
            }

            ParseResult formatResult = formatCommand.Parse(formatArgs);

            if (formatResult.Errors.Count > 0)
            {
                string errorMessage = string.Join("\n", formatResult.Errors.Select(e => e.Message));
                throw new TerminateToolException(1, errorMessage + "\n" + helpString);
            }

            try
            {
                using IObjectStream<T> sampleStream =
                    streamFactory.Create(new ParseResultParameterValues(formatResult));

                object? sample;
                while ((sample = sampleStream.Read()) != null)
                {
                    Console.WriteLine(sample);
                }
            }
            catch (IOException e)
            {
                throw new TerminateToolException(-1, "IO error while converting data : " + e.Message, e);
            }

            return 0;
        });

        return command;
    }

    /// <summary>
    /// Renders the usage string for a format's parameters, the way upstream's
    /// <c>ArgumentParser.createUsage</c> does.
    /// </summary>
    internal static string CreateUsage(IEnumerable<IFormatParameter> parameters)
    {
        var usage = new StringBuilder();
        var details = new StringBuilder();

        foreach (IFormatParameter parameter in parameters)
        {
            if (parameter.IsOptional)
            {
                usage.Append('[');
            }

            usage.Append(parameter.Name).Append(' ').Append(parameter.ValueName);

            if (parameter.IsOptional)
            {
                usage.Append(']');
            }

            usage.Append(' ');

            // NOpenNLP: upstream appends the name/valueName line unconditionally and
            // only the description line when there is a description, so a parameter
            // without one still gets an entry in the arguments description.
            details.Append('\t').Append(parameter.Name).Append(' ')
                .Append(parameter.ValueName).Append('\n');

            if (parameter.Description.Length > 0)
            {
                details.Append("\t\t").Append(parameter.Description).Append('\n');
            }
        }

        if (usage.Length > 0)
        {
            usage.Length -= 1;
        }

        if (details.Length > 0)
        {
            details.Length -= 1;
            usage.Append("\n\nArguments description:\n").Append(details);
        }

        return usage.ToString();
    }
}
