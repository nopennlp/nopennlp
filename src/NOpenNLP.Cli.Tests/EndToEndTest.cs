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

using System.IO;
using NOpenNLP.Tools.Cmdline.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Drives real tools over real corpora, end to end through the CLI.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; upstream has no equivalent. These are the tests that would catch
/// a tool that parses its arguments correctly and then does the wrong thing with them, and
/// they exercise the whole path: argument parsing, the format factories, the library, and
/// the stream each message is written to.
/// </remarks>
[NOpenNLPSpecific]
public class EndToEndTest
{
    private TempDirectory temp = null!;

    [SetUp]
    public void Setup() => temp = new TempDirectory();

    [TearDown]
    public void TearDown() => temp.Dispose();

    [Test]
    public void TestSimpleTokenizerTokenizesStandardInput()
    {
        CliResult result = CliRunner.Run(["SimpleTokenizer"], stdin: "Hi. How are you?\n");

        ClassicAssert.AreEqual(0, result.ExitCode);
        // The tokenizer splits the sentence-final punctuation off each token.
        StringAssert.Contains("Hi .", result.Out);
        StringAssert.Contains("you ?", result.Out);
    }

    [Test]
    public void TestSimpleTokenizerRunsWithoutArgumentsRatherThanPrintingHelp()
    {
        // SimpleTokenizer is the only tool with HasParams == false, so a bare invocation
        // tokenizes standard input instead of printing help.
        CliResult result = CliRunner.Run(["SimpleTokenizer"], stdin: "Hello world.\n");

        ClassicAssert.AreEqual(0, result.ExitCode);
        StringAssert.DoesNotContain("Usage:", result.Out);
        StringAssert.Contains("Hello world .", result.Out);
    }

    [Test]
    public void TestTokenizerTrainerWritesAModel()
    {
        string data = temp.CopyResource("token.train");
        string model = temp.PathOf("token.bin");

        CliResult result = CliRunner.Run(
            ["TokenizerTrainer", "-model", model, "-lang", "eng", "-data", data, "-encoding", "UTF-8"]);

        ClassicAssert.AreEqual(0, result.ExitCode, "stderr was: " + result.Error);
        FileAssert.Exists(model);
        ClassicAssert.Greater(new FileInfo(model).Length, 0);

        // Model writing progress goes to standard error, not standard output.
        StringAssert.Contains("Writing tokenizer model ... ", result.Error);
        StringAssert.Contains("Wrote tokenizer model to", result.Error);
    }

    [Test]
    public void TestExecutionTimeIsPrintedToStandardErrorOnSuccess()
    {
        CliResult result = CliRunner.Run(["SimpleTokenizer"], stdin: "Hello world.\n");

        ClassicAssert.AreEqual(0, result.ExitCode);
        StringAssert.Contains("Execution time:", result.Error);
        StringAssert.DoesNotContain("Execution time:", result.Out);
    }

    [Test]
    public void TestConverterWritesSamplesToStandardOutput()
    {
        string data = temp.CopyResource("de-ud-train-sample.conllu");

        // A converter takes its format as a positional argument rather than a .format
        // suffix, which is upstream's shape.
        CliResult result = CliRunner.Run(
            ["POSTaggerConverter", "conllu", "-data", data, "-encoding", "UTF-8"]);

        ClassicAssert.AreEqual(0, result.ExitCode, "stderr was: " + result.Error);
        ClassicAssert.IsNotEmpty(result.Out);
        // Converted POS samples are rendered as word_tag pairs.
        StringAssert.Contains("_", result.Out);
    }

    /// <summary>
    /// Two tools run in one process must not share parsed option values.
    /// </summary>
    /// <remarks>
    /// The format descriptors are shared -- every text-based format declares
    /// <c>FormatParameters.Data</c> -- and System.CommandLine binds a parsed value to the
    /// <c>Option</c> instance. Caching those instances process-wide would let the
    /// <c>-data</c> value from one invocation still be bound when the next tool builds its
    /// command, so <see cref="FormatOptions"/> scopes them per invocation. A single-shot
    /// CLI would never notice; these tests would, and so would anything hosting the CLI.
    /// </remarks>
    [Test]
    public void TestOptionValuesDoNotLeakBetweenInvocations()
    {
        string conllu = temp.CopyResource("de-ud-train-sample.conllu");
        string tokenTrain = temp.CopyResource("token.train");

        CliResult first = CliRunner.Run(
            ["POSTaggerConverter", "conllu", "-data", conllu, "-encoding", "UTF-8"]);

        ClassicAssert.AreEqual(0, first.ExitCode, "stderr was: " + first.Error);
        ClassicAssert.IsNotEmpty(first.Out);

        // A different tool, over a different corpus, in the same process.
        CliResult second = CliRunner.Run(
            ["TokenizerTrainer", "-model", temp.PathOf("tok.bin"), "-lang", "eng",
             "-data", tokenTrain, "-encoding", "UTF-8"]);

        ClassicAssert.AreEqual(0, second.ExitCode, "stderr was: " + second.Error);
        FileAssert.Exists(temp.PathOf("tok.bin"));

        // And the first tool again, to catch a stale binding in the other direction.
        CliResult third = CliRunner.Run(
            ["POSTaggerConverter", "conllu", "-data", conllu, "-encoding", "UTF-8"]);

        ClassicAssert.AreEqual(0, third.ExitCode, "stderr was: " + third.Error);
        ClassicAssert.AreEqual(first.Out, third.Out, "the same command should give the same output");
    }

    [Test]
    public void TestConverterWithoutAFormatPrintsHelp()
    {
        CliResult result = CliRunner.Run(["POSTaggerConverter"]);

        ClassicAssert.AreEqual(0, result.ExitCode);
        StringAssert.Contains("Usage: nopennlp POSTaggerConverter", result.Out);
    }

    [Test]
    public void TestTrainerWithAFormatSuffixReadsThatFormat()
    {
        string data = temp.CopyResource("de-ud-train-sample.conllu");
        string model = temp.PathOf("pos.bin");

        // The .conllu suffix selects the format, and -tagset is that format's own option,
        // merged into the same command as the tool's -model and -lang.
        CliResult result = CliRunner.Run(
            ["POSTaggerTrainer.conllu", "-model", model, "-lang", "deu", "-data", data, "-tagset", "u"]);

        ClassicAssert.AreEqual(0, result.ExitCode, "stderr was: " + result.Error);
        FileAssert.Exists(model);
    }

    [Test]
    public void TestMissingRequiredOptionExitsNonZero()
    {
        // -model is required; omitting it must fail rather than train into nowhere.
        CliResult result = CliRunner.Run(["TokenizerTrainer", "-lang", "eng"]);

        ClassicAssert.AreEqual(1, result.ExitCode);
    }

    /// <summary>
    /// A failure inside a tool must report the tool's message and exit code, not a stack
    /// trace and success.
    /// </summary>
    /// <remarks>
    /// System.CommandLine's <c>ParseResult.Invoke</c> wraps the action in its own
    /// exception handling: it catches what the action throws, prints the stack trace and
    /// returns 0. That turned every failure inside a tool into a stack trace and a
    /// success exit code, so <see cref="CLI.Run"/> invokes the action directly. Both
    /// codes here were checked against the Java CLI on the same arguments.
    /// </remarks>
    [Test]
    public void TestFailureInsideAToolReportsItsMessageAndExitCode()
    {
        string data = temp.CopyResource("token.train");

        // A missing input file is an IO error, which upstream exits -1 for; on POSIX that
        // surfaces as 255, exactly as it does from the JVM.
        CliResult missingFile = CliRunner.Run(
            ["TokenizerTrainer", "-model", temp.PathOf("m.bin"), "-lang", "eng",
             "-data", temp.PathOf("nonexistent.train"), "-encoding", "UTF-8"]);

        ClassicAssert.AreEqual(-1, missingFile.ExitCode);
        StringAssert.Contains("does not exist", missingFile.Error);
        StringAssert.DoesNotContain("Unhandled exception", missingFile.Error);
        StringAssert.DoesNotContain("   at ", missingFile.Error);

        // A bad parameter value is exit 1, and likewise must not surface as a stack trace.
        CliResult badLanguage = CliRunner.Run(
            ["TokenNameFinderConverter", "conll02", "-data", data, "-lang", "eng", "-types", "per"]);

        ClassicAssert.AreEqual(1, badLanguage.ExitCode);
        StringAssert.Contains("Unsupported language: eng", badLanguage.Error);
        StringAssert.DoesNotContain("Unhandled exception", badLanguage.Error);
    }
}
