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

using NOpenNLP.Tools.Cmdline;

/// <summary>
/// The entry point of the <c>nopennlp</c> command.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. Upstream's entry point
/// is <c>CLI.main</c>, which calls <c>System.exit</c> itself. Returning the code from
/// <see cref="CLI.Run"/> instead lets the tests drive the whole CLI in-process without
/// ending the test host.
/// </remarks>
internal static class Program
{
    private static int Main(string[] args) => CLI.Run(args);
}
