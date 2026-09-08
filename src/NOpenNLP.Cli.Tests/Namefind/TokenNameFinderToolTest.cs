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


using System.IO;
using NOpenNLP.Tools.Cmdline.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Cmdline.Namefind;

/// <summary>
/// Tests for <see cref="TokenNameFinderTool"/>.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream trains the model in-process, then swaps System.in and System.out
/// for byte array streams and calls tool.run(args) directly. The port drives the real
/// CLI through <see cref="CliRunner"/> instead: the tool reads standard input through
/// SystemInputStreamFactory, which takes the process's real handle and so ignores a
/// Console.SetIn redirect. CliRunner spawns the executable for stdin-reading tools,
/// which exercises the same path a user does. The model is trained the same way, by
/// running TokenNameFinderTrainer over the corpus upstream uses.
/// </remarks>
public class TokenNameFinderToolTest
{
    private TempDirectory temp = null!;

    [SetUp]
    public void Setup() => temp = new TempDirectory();

    [TearDown]
    public void TearDown() => temp.Dispose();

    [Test]
    public void Run()
    {
        string model1 = TrainModel();

        const string @in = "It is Stefanie Schmidt.\n\nNothing in this sentence.";

        var result = CliRunner.Run(["TokenNameFinder", model1], stdin: @in);

        ClassicAssert.AreEqual(0, result.ExitCode, "stderr was: " + result.Error);
        StringAssert.Contains("It is <START:person> Stefanie Schmidt. <END>", result.Out);
    }

    [Test]
    public void InvalidModel()
    {
        // NOpenNLP: upstream expects a TerminateToolException to escape tool.run(...).
        // The CLI catches it at the top level and turns it into the exit code it carries,
        // which is the behavior a user sees, so the test asserts on that instead.
        var result = CliRunner.Run(["TokenNameFinder", temp.PathOf("invalidmodel.bin")]);

        ClassicAssert.AreNotEqual(0, result.ExitCode);
    }

    [Test]
    public void Usage()
    {
        var result = CliRunner.Run(["TokenNameFinder"]);

        ClassicAssert.AreEqual(0, result.ExitCode, "stderr was: " + result.Error);
        ClassicAssert.AreEqual(new TokenNameFinderTool().GetHelp(), result.Out.Trim());
    }

    private string TrainModel()
    {
        string data = temp.CopyResource("AnnotatedSentencesWithTypes.txt");
        string model = temp.PathOf("namefind.bin");

        // NOpenNLP: upstream sets these on a TrainingParameters instance it passes to
        // NameFinderME.train directly. Driving the trainer through the CLI, they go in
        // a -params file, which is how the tool takes them.
        string @params = temp.PathOf("training.params");
        File.WriteAllText(@params, "Iterations=70\nCutoff=1\n");

        var result = CliRunner.Run(
        [
            "TokenNameFinderTrainer",
            "-model", model,
            "-lang", "eng",
            "-data", data,
            "-encoding", "ISO-8859-1",
            "-params", @params,
        ]);

        ClassicAssert.AreEqual(0, result.ExitCode, "training failed, stderr was: " + result.Error);
        FileAssert.Exists(model);

        return model;
    }
}
