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
using System.Text;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// A command line parameter of a corpus format.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. See
/// <see cref="IFormatParameter"/> for why parameters are described as data here rather
/// than by the annotated interfaces upstream reflects over.
/// </remarks>
/// <typeparam name="T">
/// the value type; one of <see cref="string"/>, <see cref="int"/>, <see cref="bool"/>,
/// <see cref="FileInfo"/> or <see cref="Encoding"/>, matching the types upstream's
/// <c>ArgumentParser</c> accepts.
/// </typeparam>
public sealed class FormatParameter<T> : IFormatParameter
{
    /// <summary>
    /// Declares a required parameter.
    /// </summary>
    /// <param name="name">the option including its leading dash, i.e. <c>-data</c></param>
    /// <param name="valueName">the value placeholder shown in help</param>
    /// <param name="description">the help text, or an empty string when upstream gives none</param>
    public FormatParameter(string name, string valueName, string description = "")
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ValueName = valueName ?? throw new ArgumentNullException(nameof(valueName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        IsOptional = false;
        DefaultValue = null;
    }

    private FormatParameter(string name, string valueName, string description, T? defaultValue)
        : this(name, valueName, description)
    {
        IsOptional = true;
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Declares an optional parameter, with <paramref name="defaultValue"/> used when the
    /// user omits it. Pass <c>default</c> where upstream's <c>@OptionalParameter</c>
    /// carries no <c>defaultValue</c>.
    /// </summary>
    public static FormatParameter<T> Optional(string name, string valueName,
        string description = "", T? defaultValue = default) =>
        new FormatParameter<T>(name, valueName, description, defaultValue);

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string ValueName { get; }

    /// <inheritdoc/>
    public string Description { get; }

    /// <inheritdoc/>
    public Type ValueType => typeof(T);

    /// <inheritdoc/>
    public bool IsOptional { get; }

    /// <inheritdoc/>
    public object? DefaultValue { get; }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
