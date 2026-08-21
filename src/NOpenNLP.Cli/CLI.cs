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
using System.Globalization;
using System.Linq;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// The command line interface: looks a tool up by name and runs it.
/// </summary>
public static class CLI
{
    /// <summary>
    /// The command users type, and the name this ships under as a dotnet tool.
    /// </summary>
    // NOpenNLP: upstream's CMD is "opennlp"; this port installs as `nopennlp`, so the
    // usage and help text it appears in name the command users actually have.
    public const string Cmd = "nopennlp";

    // NOpenNLP: upstream uses a LinkedHashMap so the usage listing follows registration
    // order; JCG.LinkedDictionary is the counterpart. The order below is upstream's
    // CLI.java registration order and is user-visible, so keep it.
    private static readonly JCG.LinkedDictionary<string, CmdLineTool> toolLookupMap = CreateToolLookupMap();

    private static JCG.LinkedDictionary<string, CmdLineTool> CreateToolLookupMap()
    {
        var tools = new List<CmdLineTool>();

        ToolRegistry.AddTools(tools);

        var map = new JCG.LinkedDictionary<string, CmdLineTool>();

        foreach (CmdLineTool tool in tools)
        {
            map[tool.Name] = tool;
        }

        return map;
    }

    /// <summary>
    /// The names of every registered tool, in registration order.
    /// </summary>
    public static ICollection<string> GetToolNames() => toolLookupMap.Keys;

    /// <summary>
    /// The registered tools by name, in registration order.
    /// </summary>
    public static IReadOnlyDictionary<string, CmdLineTool> GetToolLookupMap() => toolLookupMap;

    private static void Usage()
    {
        // NOpenNLP: Version is ambiguous with System.Version here, so it is qualified.
        Console.Write("NOpenNLP " + Util.Version.CurrentVersion() + ". ");
        Console.WriteLine("Usage: " + Cmd + " TOOL");
        Console.WriteLine("where TOOL is one of:");

        // distance of tool name from line start
        int numberOfSpaces = -1;
        foreach (string toolName in toolLookupMap.Keys)
        {
            if (toolName.Length > numberOfSpaces)
            {
                numberOfSpaces = toolName.Length;
            }
        }
        numberOfSpaces = numberOfSpaces + 4;

        foreach (CmdLineTool tool in toolLookupMap.Values)
        {
            Console.Write("  " + tool.Name);

            for (int i = 0; i < Math.Abs(tool.Name.Length - numberOfSpaces); i++)
            {
                Console.Write(" ");
            }

            Console.WriteLine(tool.ShortDescription);
        }

        Console.WriteLine("All tools print help when invoked with help parameter");
        Console.WriteLine("Example: " + Cmd + " SimpleTokenizer help");
    }

    /// <summary>
    /// Runs the tool named by <paramref name="args"/> and returns the process exit code.
    /// </summary>
    // NOpenNLP: upstream's main() calls System.exit directly; this returns the code so
    // the entry point can return it and tests can invoke the CLI without ending the
    // process.
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Usage();
            return 0;
        }

        long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string[] toolArguments = new string[args.Length - 1];
        Array.Copy(args, 1, toolArguments, 0, toolArguments.Length);

        string toolName = args[0];

        // check for format
        string formatName = StreamFactoryRegistry.DefaultFormat;
        int idx = toolName.IndexOf('.');
        if (-1 < idx)
        {
            formatName = toolName.Substring(idx + 1);
            toolName = toolName.Substring(0, idx);
        }

        toolLookupMap.TryGetValue(toolName, out CmdLineTool? tool);

        try
        {
            if (tool is null)
            {
                throw new TerminateToolException(1, "Tool " + toolName + " is not found.");
            }

            if ((0 == toolArguments.Length && tool.HasParams)
                || 0 < toolArguments.Length && "help".Equals(toolArguments[0], StringComparison.Ordinal))
            {
                if (tool is TypedCmdLineTool typedTool)
                {
                    Console.WriteLine(typedTool.GetHelp(formatName));
                }
                else
                {
                    Console.WriteLine(tool.GetHelp());
                }

                return 0;
            }

            if (tool is TypedCmdLineTool typed)
            {
                typed.Format = formatName;
            }
            else if (-1 != idx)
            {
                throw new TerminateToolException(1, "Tool " + toolName + " does not support formats.");
            }

            Command command = tool.CreateCommand(args[0]);
            var root = new RootCommand(Cmd);
            root.Subcommands.Add(command);

            ParseResult parseResult = root.Parse(args);

            if (parseResult.Errors.Count > 0)
            {
                // Report the parse errors and the tool's usage, then exit 1 -- which is
                // where upstream lands through TerminateToolException(1, errorMessage +
                // "\n" + getHelp()). Invoke() would print these itself, but it also
                // swallows what the action throws (see below), so the two paths are
                // separated here.
                foreach (var error in parseResult.Errors)
                {
                    Console.Error.WriteLine(error.Message);
                }

                Console.Error.WriteLine(tool.GetHelp());

                return 1;
            }

            // NOpenNLP: the action is invoked directly rather than through
            // ParseResult.Invoke(). Invoke() wraps the action in its own exception
            // handling, which catches a TerminateToolException, prints its stack trace
            // and returns 0 -- so every failure inside a tool would report success and
            // show the user a stack trace instead of the message. Calling the action
            // lets the exception reach the handler below, which is what gives upstream's
            // message and exit code.
            if (parseResult.Action is System.CommandLine.Invocation.SynchronousCommandLineAction action)
            {
                int result = action.Invoke(parseResult);

                if (result != 0)
                {
                    return result;
                }
            }
            else
            {
                // The help and version actions are the only others System.CommandLine
                // installs, and both are safe to run through the pipeline.
                return parseResult.Invoke();
            }
        }
        catch (TerminateToolException e)
        {
            if (e.Message != null)
            {
                Console.Error.WriteLine(e.Message);
            }

            if (e.InnerException != null)
            {
                Console.Error.WriteLine(e.InnerException.Message);
                Console.Error.WriteLine(e.InnerException.ToString());
            }

            return e.Code;
        }

        long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // NOpenNLP: upstream formats with %.3f and a literal \n, and writes to stderr.
        // The invariant culture keeps the decimal point a period as Java's does.
        Console.Error.Write(string.Format(CultureInfo.InvariantCulture,
            "Execution time: {0:F3} seconds\n", (endTime - startTime) / 1000.0));

        return 0;
    }
}
