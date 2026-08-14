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
using System.Reflection;
using BenchmarkDotNet.Running;

namespace NOpenNLP.Benchmarks;

internal static class Program
{
    /// <summary>
    /// Runs the benchmarks named on the command line, or prompts for a selection
    /// when given none.
    /// </summary>
    /// <remarks>
    /// The usual invocations:
    /// <code>
    /// dotnet run -c Release --                      # interactive menu
    /// dotnet run -c Release -- --filter '*'         # everything
    /// dotnet run -c Release -- --filter '*Tokeniz*' # one tool
    /// dotnet run -c Release -- --list flat          # what is available
    /// </code>
    /// </remarks>
    private static void Main(string[] args)
        => BenchmarkSwitcher
            .FromAssembly(Assembly.GetExecutingAssembly())
            .Run(args, new BenchmarkConfig());
}
