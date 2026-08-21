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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NOpenNLP.Tools.Formats;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Base class for tools which process samples of some type coming from a stream of a
/// certain format.
/// </summary>
// NOpenNLP: upstream is generic in the sample type and carries a Class<T> so it can ask
// the registry for that type's factories. C# generics keep the type at compile time, so
// the type argument lives on the derived TypedCmdLineTool<T> and this non-generic base
// exists only so CLI can set the format and ask for help without knowing the sample
// type -- the job upstream's raw TypedCmdLineTool<?> casts do.
public abstract class TypedCmdLineTool : CmdLineTool
{
    /// <summary>
    /// The format named by the <c>.format</c> suffix, or
    /// <see cref="StreamFactoryRegistry.DefaultFormat"/> when the user named none.
    /// </summary>
    public string Format { get; set; } = StreamFactoryRegistry.DefaultFormat;

    /// <inheritdoc/>
    public override string GetHelp() => GetHelp(Format);

    /// <summary>
    /// A description of how to use the tool with the given <paramref name="format"/>.
    /// </summary>
    public abstract string GetHelp(string format);

    /// <summary>
    /// The format names registered for this tool's sample type, in registration order.
    /// </summary>
    protected abstract IEnumerable<string> GetFormatNames();

    /// <summary>
    /// The parameters the named format accepts, or <c>null</c> when it is not registered.
    /// </summary>
    protected abstract IEnumerable<IFormatParameter>? GetFormatParameters(string format);

    /// <summary>
    /// Renders the <c>[.fmt1|.fmt2]</c> alternation upstream puts between the tool name
    /// and its arguments when more than one format is registered.
    /// </summary>
    protected string GetFormatsHelp()
    {
        List<string> formats = GetFormatNames()
            .Where(f => !StreamFactoryRegistry.DefaultFormat.Equals(f, System.StringComparison.Ordinal))
            .ToList();

        if (GetFormatNames().Count() <= 1)
        {
            return " ";
        }

        var builder = new StringBuilder();

        foreach (string format in formats)
        {
            builder.Append('.').Append(format).Append('|');
        }

        return "[" + builder.ToString(0, builder.Length - 1) + "] ";
    }
}
