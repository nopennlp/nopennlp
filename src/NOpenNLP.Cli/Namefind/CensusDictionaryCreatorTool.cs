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
using System.IO;
using System.Text;
using NOpenNLP.Tools.Dictionary;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Namefind;

/// <summary>
/// This tool helps create a loadable dictionary for the <c>NameFinder</c>,
/// from data collected from US Census data.
/// <para/>
/// Data for the US Census and names can be found here for the 1990 Census:
/// <br/>
/// <a href="http://www.census.gov/genealogy/names/names_files.html">www.census.gov</a>
/// </summary>
// NOpenNLP: upstream extends BasicCmdLineTool yet still declares named options, through a
// Parameters proxy interface that ArgumentParser validates and parses. There is no
// ArgumentParser here, so the options are declared on the command directly and Run is not
// the entry point -- CreateCommand is. Run stays overridden because the base class
// requires it, and it forwards to the same body.
public class CensusDictionaryCreatorTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "Converts 1990 US Census names into a dictionary";

    /// <inheritdoc/>
    // NOpenNLP: reproduces what upstream's ArgumentParser.createUsage() emits for the
    // Parameters interface. None of these four parameters carries a description upstream,
    // so the details block lists the names alone.
    public override string GetHelp() =>
        "Usage: " + CLI.Cmd + " " + Name
            + " [-encoding charsetName] [-lang code] -censusData censusDict -dict dict\n"
            + "\n"
            + "Arguments description:\n"
            + "\t-encoding charsetName\n"
            + "\t-lang code\n"
            + "\t-censusData censusDict\n"
            + "\t-dict dict";

    /// <inheritdoc/>
    public override Command CreateCommand(string commandName)
    {
        Option<string> encoding = EncodingOption();
        Option<string> lang = LangOption();
        Option<string> censusData = CensusDataOption();
        Option<string> dict = DictOption();

        var command = new Command(commandName, ShortDescription);
        command.Options.Add(encoding);
        command.Options.Add(lang);
        command.Options.Add(censusData);
        command.Options.Add(dict);

        command.SetAction(parseResult =>
        {
            Build(parseResult.GetValue(censusData)!, parseResult.GetValue(dict)!,
                parseResult.GetValue(encoding)!);
            return 0;
        });

        return command;
    }

    /// <summary>
    /// From the upstream <c>Parameters</c> interface. Unlike <c>EncodingParameter</c> this
    /// one is a plain string with no description, and it defaults to <c>UTF-8</c> rather
    /// than to the platform default.
    /// </summary>
    private static Option<string> EncodingOption() =>
        new Option<string>("-encoding")
        {
            HelpName = "charsetName",
            DefaultValueFactory = _ => "UTF-8",
        };

    /// <summary>From the upstream <c>Parameters</c> interface.</summary>
    private static Option<string> LangOption() =>
        new Option<string>("-lang")
        {
            HelpName = "code",
            DefaultValueFactory = _ => "eng",
        };

    /// <summary>From the upstream <c>Parameters</c> interface.</summary>
    private static Option<string> CensusDataOption() =>
        new Option<string>("-censusData")
        {
            HelpName = "censusDict",
            Required = true,
        };

    /// <summary>From the upstream <c>Parameters</c> interface.</summary>
    private static Option<string> DictOption() =>
        new Option<string>("-dict")
        {
            HelpName = "dict",
            Required = true,
        };

    /// <summary>
    /// Creates a dictionary.
    /// </summary>
    /// <param name="sampleStream">stream of samples.</param>
    /// <returns>
    /// a <see cref="Tools.Dictionary.Dictionary"/> class containing the name dictionary
    /// built from the input file.
    /// </returns>
    /// <exception cref="IOException">IOException</exception>
    public static Tools.Dictionary.Dictionary CreateDictionary(IObjectStream<StringList?> sampleStream)
    {
        var mNameDictionary = new Tools.Dictionary.Dictionary(true);
        StringList? entry;

        entry = sampleStream.Read();
        while (entry != null)
        {
            if (!mNameDictionary.Contains(entry))
            {
                mNameDictionary.Put(entry);
            }
            entry = sampleStream.Read();
        }

        return mNameDictionary;
    }

    /// <inheritdoc/>
    // NOpenNLP: the positional entry point BasicCmdLineTool declares. Upstream's run()
    // parses these same options out of the raw argument array, so this does too and then
    // shares the body with CreateCommand.
    public override void Run(string[] args)
    {
        string censusData = CmdLineUtil.GetParameter("-censusData", args)
            ?? throw new TerminateToolException(1,
                "-censusData is a required parameter.\n" + GetHelp());
        string dict = CmdLineUtil.GetParameter("-dict", args)
            ?? throw new TerminateToolException(1, "-dict is a required parameter.\n" + GetHelp());

        Build(censusData, dict, CmdLineUtil.GetParameter("-encoding", args) ?? "UTF-8");
    }

    private static void Build(string censusData, string dict, string encodingName)
    {
        var testData = new FileInfo(censusData);
        var dictOutFile = new FileInfo(dict);

        CmdLineUtil.CheckInputFile("Name data", testData);
        CmdLineUtil.CheckOutputFile("Dictionary file", dictOutFile);

        IInputStreamFactory sampleDataIn = CmdLineUtil.CreateInputStreamFactory(testData);

        Tools.Dictionary.Dictionary mDictionary;
        try
        {
            using IObjectStream<StringList?> sampleStream = new NameFinderCensus90NameStream(
                sampleDataIn, ResolveEncoding(encodingName));
            Console.WriteLine("Creating Dictionary...");
            mDictionary = CreateDictionary(sampleStream);
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1,
                "IO error while reading training data or indexing data: " + e.Message, e);
        }

        Console.WriteLine("Saving Dictionary...");

        try
        {
            using Stream @out = dictOutFile.Create();
            mDictionary.Serialize(@out);
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1,
                "IO error while writing dictionary file: " + e.Message, e);
        }
    }

    // NOpenNLP: stands in for Charset.forName(name), which throws
    // UnsupportedCharsetException on an unknown name. Encoding.GetEncoding throws
    // ArgumentException instead, so it is mapped onto the same TerminateToolException(1)
    // shape the other tools use for a bad -encoding value.
    private static Encoding ResolveEncoding(string encodingName)
    {
        try
        {
            return Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException e)
        {
            throw new TerminateToolException(1,
                "Invalid argument: -encoding " + encodingName + " \nEncoding " + encodingName
                    + " is not supported on this platform.", e);
        }
    }
}
