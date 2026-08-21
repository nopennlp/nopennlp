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
using System.Linq;
using NOpenNLP.Tools.Cmdline.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Tests the tool registry and the dispatch behaviour of <see cref="CLI"/>.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; upstream has no CLI test. The tool names, their order and the
/// exit codes are the CLI's user-facing contract, and a typo in any of them is invisible
/// at compile time, so they are pinned here.
/// </remarks>
[NOpenNLPSpecific]
public class CLITest
{
    /// <summary>
    /// Every tool name, in the order <c>CLI.java</c> registers them, which is the order
    /// the usage listing prints.
    /// </summary>
    private static readonly string[] ExpectedToolNames =
    [
        // Document Categorizer
        "Doccat", "DoccatTrainer", "DoccatEvaluator", "DoccatCrossValidator", "DoccatConverter",
        // Language Detector
        "LanguageDetector", "LanguageDetectorTrainer", "LanguageDetectorConverter",
        "LanguageDetectorCrossValidator", "LanguageDetectorEvaluator",
        // Dictionary Builder
        "DictionaryBuilder",
        // Tokenizer
        "SimpleTokenizer", "TokenizerME", "TokenizerTrainer", "TokenizerMEEvaluator",
        "TokenizerCrossValidator", "TokenizerConverter", "DictionaryDetokenizer",
        // Sentence detector
        "SentenceDetector", "SentenceDetectorTrainer", "SentenceDetectorEvaluator",
        "SentenceDetectorCrossValidator", "SentenceDetectorConverter",
        // Name Finder
        "TokenNameFinder", "TokenNameFinderTrainer", "TokenNameFinderEvaluator",
        "TokenNameFinderCrossValidator", "TokenNameFinderConverter", "CensusDictionaryCreator",
        // POS Tagger
        "POSTagger", "POSTaggerTrainer", "POSTaggerEvaluator", "POSTaggerCrossValidator",
        "POSTaggerConverter",
        // Lemmatizer
        "LemmatizerME", "LemmatizerTrainerME", "LemmatizerEvaluator",
        // Chunker
        "ChunkerME", "ChunkerTrainerME", "ChunkerEvaluator", "ChunkerCrossValidator",
        "ChunkerConverter",
        // Parser
        "Parser", "ParserTrainer", "ParserEvaluator", "ParserConverter",
        "BuildModelUpdater", "CheckModelUpdater", "TaggerModelReplacer",
        // Entity Linker
        "EntityLinker",
        // Language Model
        "NGramLanguageModel",
    ];

    [Test]
    public void TestAllToolsAreRegisteredInUpstreamOrder()
    {
        CollectionAssert.AreEqual(ExpectedToolNames, CLI.GetToolNames().ToArray(),
            "tool names and their order are the usage listing, and must match CLI.java");
    }

    [Test]
    public void TestToolCountMatchesUpstream()
    {
        ClassicAssert.AreEqual(51, CLI.GetToolNames().Count);
    }

    /// <summary>
    /// The two tools that override the name derived from their class name.
    /// </summary>
    [Test]
    public void TestTrainerNameOverrides()
    {
        // ChunkerTrainerTool and LemmatizerTrainerTool append "ME" rather than taking the
        // default name of the class minus "Tool".
        CollectionAssert.Contains(CLI.GetToolNames(), "ChunkerTrainerME");
        CollectionAssert.Contains(CLI.GetToolNames(), "LemmatizerTrainerME");
        CollectionAssert.DoesNotContain(CLI.GetToolNames(), "ChunkerTrainer");
        CollectionAssert.DoesNotContain(CLI.GetToolNames(), "LemmatizerTrainer");
    }

    [Test]
    public void TestEveryToolHasAShortDescriptionExceptDictionaryDetokenizer()
    {
        IReadOnlyDictionary<string, CmdLineTool> tools = CLI.GetToolLookupMap();

        foreach (KeyValuePair<string, CmdLineTool> entry in tools)
        {
            if ("DictionaryDetokenizer".Equals(entry.Key, StringComparison.Ordinal))
            {
                // Upstream really does leave this one empty.
                ClassicAssert.AreEqual("", entry.Value.ShortDescription);
            }
            else
            {
                ClassicAssert.IsNotEmpty(entry.Value.ShortDescription,
                    entry.Key + " should have a short description");
            }
        }
    }

    [Test]
    public void TestSimpleTokenizerIsTheOnlyToolWithoutParams()
    {
        string[] withoutParams = CLI.GetToolLookupMap()
            .Where(e => !e.Value.HasParams)
            .Select(e => e.Key)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "SimpleTokenizer" }, withoutParams);
    }

    [Test]
    public void TestNoArgumentsPrintsUsageAndExitsZero()
    {
        CliResult result = CliRunner.Run([]);

        ClassicAssert.AreEqual(0, result.ExitCode);
        StringAssert.Contains("Usage: nopennlp TOOL", result.Out);
        StringAssert.Contains("where TOOL is one of:", result.Out);
        StringAssert.Contains("All tools print help when invoked with help parameter", result.Out);
        StringAssert.Contains("Example: nopennlp SimpleTokenizer help", result.Out);

        // Every tool is listed.
        foreach (string toolName in ExpectedToolNames)
        {
            StringAssert.Contains(toolName, result.Out);
        }
    }

    [Test]
    public void TestUsageDoesNotPrintExecutionTime()
    {
        // Upstream reaches the "Execution time:" line only on the fully successful path,
        // and returns from the no-arguments branch before it.
        CliResult result = CliRunner.Run([]);

        StringAssert.DoesNotContain("Execution time:", result.Out);
        StringAssert.DoesNotContain("Execution time:", result.Error);
    }

    [Test]
    public void TestUnknownToolExitsOneWithMessageOnStandardError()
    {
        CliResult result = CliRunner.Run(["NoSuchTool"]);

        ClassicAssert.AreEqual(1, result.ExitCode);
        StringAssert.Contains("Tool NoSuchTool is not found.", result.Error);
        ClassicAssert.IsEmpty(result.Out);
    }

    [Test]
    public void TestUnknownFormatExitsOneWithMessageOnStandardError()
    {
        CliResult result = CliRunner.Run(["TokenizerTrainer.nosuchformat", "-model", "m.bin"]);

        ClassicAssert.AreEqual(1, result.ExitCode);
        StringAssert.Contains("Format nosuchformat is not found.", result.Error);
    }

    [Test]
    public void TestFormatOnABasicToolExitsOneWithMessageOnStandardError()
    {
        // A BasicCmdLineTool takes positional arguments and has no format to select.
        CliResult result = CliRunner.Run(["SimpleTokenizer.conllu", "somearg"]);

        ClassicAssert.AreEqual(1, result.ExitCode);
        StringAssert.Contains("Tool SimpleTokenizer does not support formats.", result.Error);
    }

    [Test]
    public void TestHelpArgumentPrintsHelpAndExitsZero()
    {
        CliResult result = CliRunner.Run(["TokenizerTrainer", "help"]);

        ClassicAssert.AreEqual(0, result.ExitCode);
        StringAssert.Contains("Usage: nopennlp TokenizerTrainer", result.Out);
    }

    [Test]
    public void TestNoArgumentsToAToolWithParamsPrintsHelpAndExitsZero()
    {
        CliResult result = CliRunner.Run(["TokenizerTrainer"]);

        ClassicAssert.AreEqual(0, result.ExitCode);
        StringAssert.Contains("Usage: nopennlp TokenizerTrainer", result.Out);
    }
}
