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
using System.IO;
using NUnit.Framework;

namespace NOpenNLP.Tools.Integration;

/// <summary>
/// Locates the downloaded integration test data.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. Upstream's
/// eval tests read an OPENNLP_DATA_DIR system property that the developer
/// populates by hand. Here the models are fetched by
/// <c>build/download-test-models.sh</c> into a gitignored <c>testdata/</c>
/// directory at the repository root, so the tests run unattended once that
/// script has been executed. NOPENNLP_TEST_DATA_DIR overrides the location,
/// which is what CI sets when it restores the download from its cache.
/// </remarks>
internal static class TestData
{
    private const string DataDirVariable = "NOPENNLP_TEST_DATA_DIR";

    /// <summary>
    /// The test data directory, or null when it has not been downloaded.
    /// </summary>
    public static string? DataDirectory { get; } = ResolveDataDirectory();

    /// <summary>
    /// Marks the calling test inconclusive when <paramref name="relativePath"/> is
    /// missing, and otherwise returns its full path.
    /// </summary>
    /// <remarks>
    /// Inconclusive rather than failed: a developer who has not run the download
    /// script has not broken anything, and a red suite for that reason trains
    /// people to ignore failures. CI runs the script, so a genuine regression
    /// there still shows up as a failure.
    /// </remarks>
    public static string RequireFile(string relativePath)
    {
        if (DataDirectory is null)
        {
            Assert.Inconclusive(
                $"Integration test data not found. Run build/download-test-models.sh, " +
                $"or set {DataDirVariable} to the directory holding models-sf/.");
        }

        string path = Path.Combine(DataDirectory!, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(path))
        {
            Assert.Inconclusive(
                $"Integration test file '{relativePath}' not found under {DataDirectory}. " +
                "Run build/download-test-models.sh to fetch the models.");
        }

        return path;
    }

    private static string? ResolveDataDirectory()
    {
        string? configured = Environment.GetEnvironmentVariable(DataDirVariable);
        if (!string.IsNullOrEmpty(configured))
        {
            return Directory.Exists(configured) ? configured : null;
        }

        // Walk up from the test assembly to the repository root, which is the
        // directory holding testdata/. The assembly sits several levels down in
        // bin/<configuration>/<framework>, and that depth varies per leg, so the
        // marker directory is searched for rather than assumed.
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "testdata");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
