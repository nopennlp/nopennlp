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
using System.IO;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Compat;

/// <summary>
/// Verifies two-way model compatibility between NOpenNLP and Apache OpenNLP
/// 1.9.5 running on a real JVM.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. This is the
/// .NET half of the harness in <c>src/java/model-compat</c>, for issue #46.
/// <para/>
/// Each runtime trains the same five models from the same corpora with the same
/// hyperparameters (<see cref="CompatCorpus"/>), serializes them, and records
/// the inference output it got (<see cref="CompatResults"/>). The other runtime
/// then loads those model files and must produce the same output from the same
/// inputs. Loading alone would be a weak check: a model reader that silently
/// mangled a parameter table would still open the file. Comparing the analysis,
/// including the probabilities behind it, is what makes this a compatibility
/// test rather than a smoke test.
/// <para/>
/// Two environment variables drive the cross-runtime directions, both set by the
/// <c>run-compat</c> driver script in the Maven project:
/// <list type="bullet">
/// <item><description><c>NOPENNLP_COMPAT_READ_DIR</c> - a directory of
/// Java-trained models to read. If unset or missing, the read test is
/// <b>inconclusive</b> (skipped), because a JDK may not be present in every
/// environment.</description></item>
/// <item><description><c>NOPENNLP_COMPAT_WRITE_DIR</c> - where to write the
/// NOpenNLP models for Java to read. Defaults to a temp directory.</description></item>
/// </list>
/// The Java side takes the opposite stance on a missing input: when it is asked
/// to read NOpenNLP's models, their absence means the pipeline that was supposed
/// to produce them is broken, so that test fails rather than skipping.
/// </remarks>
[Category("Integration")]
[NOpenNLPSpecific]
public class JavaCompatibilityTest
{
    private const string ReadDirVariable = "NOPENNLP_COMPAT_READ_DIR";
    private const string WriteDirVariable = "NOPENNLP_COMPAT_WRITE_DIR";

    /// <summary>
    /// Pure-NOpenNLP round trip: train the five models, write them, read them
    /// back through the model readers, and confirm the analysis survives the
    /// round trip unchanged.
    /// </summary>
    /// <remarks>
    /// This is the CI-safe baseline that needs no JDK. It does not prove
    /// anything about Java, but it does guard the shared contract: if the corpora
    /// or the inference inputs are changed in a way the port cannot handle, this
    /// fails on every build rather than only in the hand-run compatibility job.
    /// <para/>
    /// The comparison is against a fresh in-memory analysis rather than against
    /// the file, so a bug in <see cref="CompatResults"/> serialization cannot
    /// make it pass; the file round trip is asserted separately below.
    /// </remarks>
    [Test]
    public void TestNOpenNLPRoundTrip()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            CompatModels.TrainAll(directory);

            CompatResults first = CompatModels.Analyze(directory);
            CompatResults second = CompatModels.Analyze(directory);

            // Deterministic: loading the same model twice and asking the same
            // questions has to give the same answers, or nothing downstream of
            // here means anything.
            AssertSameResults(first.Entries, second.Entries, "NOpenNLP (first pass)", "NOpenNLP (second pass)");

            ClassicAssert.IsNotEmpty(first.Entries, "the harness recorded no results at all");

            // The results file has to survive a write/read cycle, since that is
            // how the value crosses to the other runtime.
            string resultsPath = Path.Combine(directory, CompatCorpus.ResultsFile);
            first.Write(resultsPath);
            AssertSameResults(first.Entries, CompatResults.Read(resultsPath),
                "in memory", "read back from " + CompatCorpus.ResultsFile);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Java to .NET direction: load models trained by Apache OpenNLP on a real
    /// JVM (path from <c>NOPENNLP_COMPAT_READ_DIR</c>) and confirm NOpenNLP's
    /// analysis matches what Java recorded.
    /// </summary>
    [Test]
    public void TestReadJavaTrainedModels()
    {
        string? directory = Environment.GetEnvironmentVariable(ReadDirVariable);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            Assert.Inconclusive(
                $"No Java-trained models to read. Set the '{ReadDirVariable}' environment variable " +
                "to a directory produced by the Java harness (see src/java/model-compat/README.md).");
            return;
        }

        string recorded = Path.Combine(directory!, CompatCorpus.ResultsFile);
        if (!File.Exists(recorded))
        {
            Assert.Inconclusive(
                $"'{directory}' has no {CompatCorpus.ResultsFile}, so there is nothing to compare " +
                "against. Re-run the Java harness (see src/java/model-compat/README.md).");
            return;
        }

        // Analyze opens every model before it can run anything, so a model
        // NOpenNLP cannot read fails here with the reader's own exception, which
        // says more than an assertion would.
        IDictionary<string, string> actual = CompatModels.Analyze(directory!).Entries;

        AssertSameResults(CompatResults.Read(recorded), actual, "Apache OpenNLP (Java)", "NOpenNLP");
    }

    /// <summary>
    /// .NET to Java direction (writer half): train the five models, write them
    /// into <c>NOPENNLP_COMPAT_WRITE_DIR</c> (or a temp folder), and record the
    /// analysis alongside them so the Java JUnit test can compare against it.
    /// The cross-runtime assertions happen on the Java side.
    /// </summary>
    [Test]
    public void TestWriteModelsForJava()
    {
        string? configured = Environment.GetEnvironmentVariable(WriteDirVariable);
        string directory = string.IsNullOrEmpty(configured) ? CreateTemporaryDirectory() : configured!;
        Directory.CreateDirectory(directory);

        CompatModels.TrainAll(directory);

        CompatResults results = CompatModels.Analyze(directory);
        results.Write(Path.Combine(directory, CompatCorpus.ResultsFile));

        ClassicAssert.IsNotEmpty(results.Entries, "the harness recorded no results at all");

        TestContext.Progress.WriteLine(
            $"Wrote NOpenNLP models and {results.Entries.Count} results under: {directory}");
    }

    /// <summary>
    /// Compares two result sets key by key, reporting every difference at once.
    /// </summary>
    /// <remarks>
    /// A single assertion on the whole set would print two hundred-line strings
    /// and leave the reader to spot the difference. The Java side has the same
    /// helper for the same reason.
    /// </remarks>
    private static void AssertSameResults(
        IDictionary<string, string> expected,
        IDictionary<string, string> actual,
        string expectedLabel,
        string actualLabel)
    {
        JCG.LinkedHashSet<string> keys = [.. expected.Keys];
        keys.UnionWith(actual.Keys);

        JCG.List<string> differences = [];
        foreach (string key in keys)
        {
            bool haveExpected = expected.TryGetValue(key, out string? want);
            bool haveActual = actual.TryGetValue(key, out string? got);

            if (!haveExpected)
            {
                differences.Add($"{key}: only {actualLabel} produced a value ({got})");
            }
            else if (!haveActual)
            {
                differences.Add($"{key}: only {expectedLabel} produced a value ({want})");
            }
            else if (!string.Equals(want, got, StringComparison.Ordinal))
            {
                differences.Add($"{key}:\n    {expectedLabel}: {want}\n    {actualLabel}: {got}");
            }
        }

        if (differences.Count > 0)
        {
            Assert.Fail($"{expectedLabel} and {actualLabel} disagree on {differences.Count} of " +
                $"{keys.Count} results:\n  " + string.Join("\n  ", differences));
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "nopennlp-compat-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
