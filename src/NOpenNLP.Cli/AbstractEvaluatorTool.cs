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
using System.CommandLine;
using System.Linq;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Base class for tools which read samples of type <typeparamref name="T"/> from a stream
/// of a named format.
/// </summary>
// NOpenNLP: this collapses upstream's AbstractTypedParamTool / AbstractEvaluatorTool /
// AbstractTrainerTool / AbstractCrossValidatorTool chain, whose distinct job was to carry
// the annotated params Class and re-parse a filtered String[] at each level. With
// System.CommandLine the options are declared once and read from the parse result, so the
// chain has nothing left to do; the trainer-specific helper lives on
// AbstractTrainerTool below it.
public abstract class AbstractEvaluatorTool<T> : TypedCmdLineTool
{
    /// <summary>
    /// The factory for the selected format, available once the action runs.
    /// </summary>
    protected IObjectStreamFactory<T>? factory;

    /// <summary>
    /// The samples read from the selected format, available once the action runs.
    /// </summary>
    protected IObjectStream<T>? sampleStream;

    /// <inheritdoc/>
    protected override IEnumerable<string> GetFormatNames() =>
        StreamFactoryRegistry.GetFactories<T>().Keys;

    /// <inheritdoc/>
    protected override IEnumerable<IFormatParameter>? GetFormatParameters(string format) =>
        StreamFactoryRegistry.GetFactory<T>(format)?.Parameters;

    /// <summary>
    /// The options this tool accepts, beyond the selected format's own.
    /// </summary>
    protected abstract IEnumerable<Option> GetToolOptions();

    /// <summary>
    /// Runs the tool once its options and the format's have been parsed.
    /// </summary>
    protected abstract void Run(ParseResult parseResult);

    /// <summary>
    /// Resolves the factory for the selected format, or fails the way upstream does.
    /// </summary>
    // NOpenNLP: upstream appends getHelp() to this message. Here a typed tool's help is
    // built from the selected format's parameters, so calling it from the not-found path
    // would ask for the factory that was just missing and recurse until the stack ran
    // out. The message names the formats that do exist instead, which is the part of the
    // help a user with a bad format name needs.
    protected IObjectStreamFactory<T> GetStreamFactory(string format)
    {
        IObjectStreamFactory<T>? streamFactory = StreamFactoryRegistry.GetFactory<T>(format);

        if (null != streamFactory)
        {
            return streamFactory;
        }

        throw new TerminateToolException(1, "Format " + format + " is not found.\n"
            + "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp()
            + "[options...]");
    }

    /// <inheritdoc/>
    public override Command CreateCommand(string commandName)
    {
        var command = new Command(commandName, ShortDescription);

        foreach (Option option in GetToolOptions())
        {
            command.Options.Add(option);
        }

        // The selected format contributes its own options, so `Tool.format -data x` gets
        // one merged option list -- which is what upstream assembles by validating the
        // tool's params and the factory's params against the same argument array.
        IObjectStreamFactory<T> streamFactory = GetStreamFactory(Format);

        foreach (IFormatParameter parameter in streamFactory.Parameters)
        {
            command.Options.Add(FormatOptions.ToOption(parameter));
        }

        command.SetAction(parseResult =>
        {
            factory = streamFactory;
            sampleStream = streamFactory.Create(new ParseResultParameterValues(parseResult));

            Run(parseResult);

            return 0;
        });

        return command;
    }
}
