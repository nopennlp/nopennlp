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

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// Supplies the parsed value of each <see cref="IFormatParameter"/> a factory declared.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. It stands in for the
/// dynamic proxy upstream's <c>ArgumentParser.parse</c> returns: where upstream calls
/// <c>params.getData()</c> on a generated implementation of an annotated interface, a
/// ported factory calls <see cref="Get{T}"/> with the parameter it declared. Keeping this
/// an interface lets the CLI back it with a System.CommandLine parse result while the
/// library stays free of that dependency, and lets tests supply values directly.
/// </remarks>
public interface IFormatParameterValues
{
    /// <summary>
    /// Returns the parsed value of <paramref name="parameter"/>, which is the default
    /// when the user omitted an optional parameter.
    /// </summary>
    /// <typeparam name="T">the parameter's value type</typeparam>
    /// <param name="parameter">a parameter this factory declared</param>
    T? Get<T>(IFormatParameter parameter);
}
