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

using System.CommandLine;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Base class for tools which take positional arguments rather than named options.
/// </summary>
public abstract class BasicCmdLineTool : CmdLineTool
{
    /// <summary>
    /// Executes the tool with the given positional arguments.
    /// </summary>
    public abstract void Run(string[] args);

    /// <inheritdoc/>
    // NOpenNLP: these tools take bare positional arguments -- `nopennlp TokenizerME
    // model < sentences` -- and parse them by index, so the command carries one variadic
    // argument and hands the values straight to Run. Declaring options here would change
    // the command lines users type.
    public override Command CreateCommand(string commandName)
    {
        var arguments = new Argument<string[]>("args")
        {
            Description = "the tool's positional arguments",
            Arity = ArgumentArity.ZeroOrMore,
        };

        var command = new Command(commandName, ShortDescription);
        command.Arguments.Add(arguments);

        command.SetAction(parseResult =>
        {
            Run(parseResult.GetValue(arguments) ?? []);
            return 0;
        });

        return command;
    }
}
