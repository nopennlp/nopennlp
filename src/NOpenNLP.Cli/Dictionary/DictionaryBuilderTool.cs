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
using System.IO;
using System.Text;
using NOpenNLP.Tools.Formats;
using JDict = NOpenNLP.Tools.Dictionary;

namespace NOpenNLP.Tools.Cmdline.Dictionary;

// NOpenNLP: upstream extends BasicCmdLineTool yet still declares named options, through a
// DictionaryBuilderParams proxy interface that ArgumentParser validates and parses. There
// is no ArgumentParser here, so the options are declared on the command directly and Run
// is not the entry point -- CreateCommand is. Run stays overridden because the base class
// requires it, and it forwards to the same body so the tool is still callable with a
// parsed argument array.
public class DictionaryBuilderTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "builds a new dictionary";

    /// <inheritdoc/>
    // NOpenNLP: reproduces what upstream's ArgumentParser.createUsage() emits for
    // DictionaryBuilderParams, including the option order, which comes from Java
    // reflection and is what the OpenNLP manual documents.
    public override string GetHelp() =>
        "Usage: " + CLI.Cmd + " " + Name
            + " -outputFile out -inputFile in [-encoding charsetName]\n"
            + "\n"
            + "Arguments description:\n"
            + "\t-outputFile out\n"
            + "\t\tThe dictionary file.\n"
            + "\t-inputFile in\n"
            + "\t\tPlain file with one entry per line\n"
            + "\t-encoding charsetName\n"
            + "\t\tencoding for reading and writing text, if absent the system default is used.";

    /// <inheritdoc/>
    public override Command CreateCommand(string commandName)
    {
        Option<FileInfo> outputFile = OutputFileOption();
        Option<FileInfo> inputFile = InputFileOption();
        Option<string> encoding = ToolParams.Encoding();

        var command = new Command(commandName, ShortDescription);
        command.Options.Add(outputFile);
        command.Options.Add(inputFile);
        command.Options.Add(encoding);

        command.SetAction(parseResult =>
        {
            Build(parseResult.GetValue(inputFile)!, parseResult.GetValue(outputFile)!,
                FormatParameters.ResolveEncoding(parseResult.GetValue(encoding)));
            return 0;
        });

        return command;
    }

    /// <summary>From <c>DictionaryBuilderParams</c>.</summary>
    private static Option<FileInfo> InputFileOption() =>
        new Option<FileInfo>("-inputFile")
        {
            Description = "Plain file with one entry per line",
            HelpName = "in",
            Required = true,
        };

    /// <summary>From <c>DictionaryBuilderParams</c>.</summary>
    private static Option<FileInfo> OutputFileOption() =>
        new Option<FileInfo>("-outputFile")
        {
            Description = "The dictionary file.",
            HelpName = "out",
            Required = true,
        };

    /// <inheritdoc/>
    // NOpenNLP: the positional entry point BasicCmdLineTool declares. Upstream's run()
    // parses these same three options out of the raw argument array, so this does too and
    // then shares the body with CreateCommand.
    public override void Run(string[] args)
    {
        var dictInFile = new FileInfo(CmdLineUtil.GetParameter("-inputFile", args)
            ?? throw new TerminateToolException(1, "-inputFile is a required parameter.\n" + GetHelp()));
        var dictOutFile = new FileInfo(CmdLineUtil.GetParameter("-outputFile", args)
            ?? throw new TerminateToolException(1, "-outputFile is a required parameter.\n" + GetHelp()));

        Build(dictInFile, dictOutFile,
            FormatParameters.ResolveEncoding(CmdLineUtil.GetParameter("-encoding", args)));
    }

    private static void Build(FileInfo dictInFile, FileInfo dictOutFile, Encoding encoding)
    {
        CmdLineUtil.CheckInputFile("dictionary input file", dictInFile);
        CmdLineUtil.CheckOutputFile("dictionary output file", dictOutFile);

        try
        {
            using var @in = new StreamReader(dictInFile.OpenRead(), encoding);
            using Stream @out = dictOutFile.Create();

            JDict.Dictionary dict = JDict.Dictionary.ParseOneEntryPerLine(@in);
            dict.Serialize(@out);
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1,
                "IO error while reading training data or indexing data: " + e.Message, e);
        }
    }
}
