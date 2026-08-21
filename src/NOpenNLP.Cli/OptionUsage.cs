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

using System.Collections.Generic;
using System.CommandLine;
using System.Text;
using NOpenNLP.Tools.Formats;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Renders the usage string for a tool's own options followed by the selected format's
/// parameters.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. It reproduces what
/// upstream's <c>ArgumentParser.createUsage(Class&lt;?&gt;...)</c> builds by reflecting
/// over the annotated params interfaces, which have no counterpart here: the shared
/// options in <see cref="ToolParams"/> are System.CommandLine options rather than
/// annotated methods, so the usage text is composed from those instead.
/// </remarks>
internal static class OptionUsage
{
    /// <summary>
    /// Builds the usage line and the arguments description for <paramref name="options"/>
    /// followed by <paramref name="parameters"/>, in that order -- the order upstream
    /// passes the tool's params interface before the factory's.
    /// </summary>
    internal static string CreateUsage(IEnumerable<Option> options,
        IEnumerable<IFormatParameter> parameters)
    {
        var usage = new StringBuilder();
        var details = new StringBuilder();
        var seen = new HashSet<string>();

        foreach (Option option in options)
        {
            Append(usage, details, seen, option.Name, ValueNameOf(option),
                option.Description ?? "", !option.Required);
        }

        foreach (IFormatParameter parameter in parameters)
        {
            Append(usage, details, seen, parameter.Name, parameter.ValueName,
                parameter.Description, parameter.IsOptional);
        }

        if (usage.Length > 0)
        {
            usage.Length -= 1;
        }

        if (details.Length > 0)
        {
            details.Length -= 1;
            usage.Append("\n\nArguments description:\n").Append(details);
        }

        return usage.ToString();
    }

    private static void Append(StringBuilder usage, StringBuilder details, HashSet<string> seen,
        string name, string valueName, string description, bool isOptional)
    {
        // Upstream filters duplicates, since a tool's params interfaces can declare the
        // same option more than once through interface inheritance.
        if (!seen.Add(name))
        {
            return;
        }

        if (isOptional)
        {
            usage.Append('[');
        }

        usage.Append(name).Append(' ').Append(valueName);

        if (isOptional)
        {
            usage.Append(']');
        }

        usage.Append(' ');

        details.Append('\t').Append(name).Append(' ').Append(valueName).Append('\n');

        if (description.Length > 0)
        {
            details.Append("\t\t").Append(description).Append('\n');
        }
    }

    // NOpenNLP: HelpName carries what upstream's @ParameterDescription calls valueName;
    // System.CommandLine leaves it null when it was never set, and falls back to the
    // option name in its own help.
    private static string ValueNameOf(Option option) => option.HelpName ?? option.Name;
}
