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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace NOpenNLP.Tools.Cmdline.Support;

/// <summary>
/// The result of running the CLI: its exit code and what it wrote to each stream.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// </remarks>
internal sealed record CliResult(int ExitCode, string Out, string Error);

/// <summary>
/// Runs the CLI and captures its exit code, standard output and standard error.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. The tools' output, and
/// which stream each line goes to, are part of the user-facing contract, so the tests
/// assert on both.
/// <para/>
/// Tools that do not read standard input run in-process, which is fast and gives clean
/// stack traces when a test fails. Tools that do read it run as a child process instead:
/// they reach standard input through <c>SystemInputStreamFactory</c>, which calls
/// <see cref="Console.OpenStandardInput"/> and so takes the process's real handle,
/// bypassing a <see cref="Console.SetIn"/> redirect entirely. Redirecting the handle
/// itself would mean P/Invoking <c>dup2</c>, which is Unix-only and leaves the test host
/// with a permanently altered standard input; spawning the real executable avoids both
/// problems and exercises the same path a user does.
/// </remarks>
internal static class CliRunner
{
    /// <summary>
    /// Runs the CLI with <paramref name="args"/> and captures its output.
    /// </summary>
    /// <param name="stdin">
    /// text to supply on standard input; when given, the CLI runs as a child process
    /// </param>
    public static CliResult Run(string[] args, string? stdin = null) =>
        stdin is null ? RunInProcess(args) : RunAsProcess(args, stdin);

    private static CliResult RunInProcess(string[] args)
    {
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;

        var capturedOut = new StringWriter();
        var capturedError = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);
            Console.SetError(capturedError);

            int exitCode = CLI.Run(args);

            return new CliResult(exitCode, capturedOut.ToString(), capturedError.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static CliResult RunAsProcess(string[] args, string stdin)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add(CliAssemblyPath);

        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the nopennlp process.");

        process.StandardInput.Write(stdin);
        process.StandardInput.Close();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return new CliResult(process.ExitCode, output, error);
    }

    /// <summary>
    /// The built <c>nopennlp.dll</c>, which sits beside the test assembly because the
    /// test project references the CLI project.
    /// </summary>
    private static string CliAssemblyPath
    {
        get
        {
            string directory = Path.GetDirectoryName(typeof(CliRunner).Assembly.Location)!;
            string path = Path.Combine(directory, "nopennlp.dll");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "nopennlp.dll was not found next to the test assembly; the CLI project " +
                    "reference should place it there.", path);
            }

            return path;
        }
    }
}
