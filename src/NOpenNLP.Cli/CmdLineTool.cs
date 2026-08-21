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
using System.CommandLine;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Base class for all command line tools.
/// </summary>
public abstract class CmdLineTool
{
    protected CmdLineTool()
    {
    }

    /// <summary>
    /// The name of the tool, used as the command. Must not contain white spaces.
    /// </summary>
    // NOpenNLP: upstream derives this from the runtime class name, stripping a trailing
    // "Tool". The same derivation works here because the ported classes keep upstream's
    // names, and the two tools that override it (ChunkerTrainerTool ->
    // "ChunkerTrainerME", LemmatizerTrainerTool -> "LemmatizerTrainerME") override it
    // here too.
    public virtual string Name
    {
        get
        {
            string simpleName = GetType().Name;

            return simpleName.EndsWith("Tool", StringComparison.Ordinal)
                ? simpleName.Substring(0, simpleName.Length - 4)
                : simpleName;
        }
    }

    /// <summary>
    /// Whether the tool has any command line parameters.
    /// </summary>
    public virtual bool HasParams => true;

    /// <summary>
    /// A short description of what the tool does, shown in the usage listing.
    /// </summary>
    public virtual string ShortDescription => "";

    /// <summary>
    /// A description of how to use the tool.
    /// </summary>
    public abstract string GetHelp();

    /// <summary>
    /// Builds the command that parses this tool's arguments and runs it.
    /// </summary>
    /// <param name="commandName">
    /// the name the user typed, which for a typed tool includes the <c>.format</c>
    /// suffix and so is not always <see cref="Name"/>
    /// </param>
    // NOpenNLP: upstream declares run(String[]) (or run(String, String[]) for a typed
    // tool) and does its own validation through ArgumentParser. Here each tool
    // contributes a System.CommandLine Command instead, which parses and validates the
    // arguments and reports errors, so the ArgumentParser layer has no counterpart.
    public abstract Command CreateCommand(string commandName);
}
