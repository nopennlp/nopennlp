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

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// Describes one command line parameter a corpus format accepts.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. Upstream describes a
/// format's parameters with an interface whose getters carry
/// <c>@ParameterDescription</c> and <c>@OptionalParameter</c> annotations, and
/// <c>ArgumentParser</c> reflects over it to build a dynamic proxy. .NET has no dynamic
/// proxy in the BCL, and this port drives the command line with System.CommandLine, so a
/// parameter is described by data instead. The names, value names, descriptions and
/// defaults carried here are copied verbatim from the upstream annotations, because they
/// are the user-facing contract.
/// <para/>
/// This lives in NOpenNLP.Tools rather than in the CLI project so the library can
/// describe its formats without taking a dependency on System.CommandLine; the CLI
/// translates these descriptors into options.
/// </remarks>
public interface IFormatParameter
{
    /// <summary>
    /// The option as the user types it, including the leading dash, i.e. <c>-data</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The placeholder shown for the value in help, i.e. <c>sampleData</c>.
    /// </summary>
    string ValueName { get; }

    /// <summary>
    /// The help text for this parameter, or an empty string when upstream gives none.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The value type. One of <see cref="string"/>, <see cref="int"/>,
    /// <see cref="bool"/>, <see cref="System.IO.FileInfo"/> or
    /// <see cref="System.Text.Encoding"/>, matching the types upstream's
    /// <c>ArgumentParser</c> accepts.
    /// </summary>
    Type ValueType { get; }

    /// <summary>
    /// Whether the user may omit this parameter.
    /// </summary>
    bool IsOptional { get; }

    /// <summary>
    /// The value used when an optional parameter is omitted, or <c>null</c> when
    /// upstream declares no default.
    /// </summary>
    object? DefaultValue { get; }
}
