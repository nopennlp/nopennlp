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
#nullable enable
using System;
using System.IO;
using System.Text;
using J2N.Collections.Generic;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// A minimal stand-in for Java's <c>java.util.Properties</c>, supporting the
/// subset of behavior the ported OpenNLP model-loading code relies on.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// </remarks>
internal class Properties : Dictionary<object, object>
{
    public string? GetProperty(string key)
    {
        if (TryGetValue(key, out object? value) && value is string s)
        {
            return s;
        }
        return null;
    }

    public string? GetProperty(string key, string? defaultValue)
    {
        if (TryGetValue(key, out object? value) && value is string s)
        {
            return s;
        }
        return defaultValue;
    }

    public void Load(Stream s)
    {
        // NOpenNLP: Java's Properties.load leaves the stream open, and callers such as
        // PropertiesSerializer read further entries from it afterwards, so leaveOpen is required.
        using var reader = new StreamReader(s, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024, leaveOpen: true);
        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            // NOpenNLP: Java treats both '#' and '!' as comment markers.
            // string.StartsWith(char) is net5.0+, so compare with strings.
            if (line.Length == 0 ||
                line.StartsWith("#", StringComparison.Ordinal) ||
                line.StartsWith("!", StringComparison.Ordinal))
                continue;

            // NOpenNLP: Java's Properties.load accepts '=', ':' or whitespace as
            // the key/value separator, so all three are honored here. Splitting on
            // '=' alone would silently skip entries such as
            // "OpenNLP-Version: 1.9.4" in the embedded opennlp.version resource.
            int separatorIndex = -1;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '=' || c == ':' || c == ' ' || c == '\t')
                {
                    separatorIndex = i;
                    break;
                }
            }

            if (separatorIndex > 0)
            {
                string key = line.Substring(0, separatorIndex).Trim();
                // A '=' or ':' directly after whitespace is part of the separator,
                // not the value.
                string rest = line.Substring(separatorIndex).TrimStart(' ', '\t');
                if (rest.Length > 0 && (rest[0] == '=' || rest[0] == ':'))
                {
                    rest = rest.Substring(1);
                }

                this[key] = rest.Trim();
            }
        }
    }

    public object? SetProperty(string key, string value)
    {
        var oldValue = ContainsKey(key) ? this[key] : null;
        this[key] = value;
        return oldValue;
    }

    public void Store(Stream s, string? comments)
    {
        // NOpenNLP: Java's Properties.store leaves the stream open, matching Load
        // above. Without leaveOpen the StreamWriter would close the caller's stream
        // on dispose, which breaks callers that write further entries afterwards --
        // TrainingParameters.Serialize, and the model serializers that write several
        // artifacts into a single zip stream.
        using var writer = new StreamWriter(s, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 1024, leaveOpen: true);
        if (!string.IsNullOrEmpty(comments))
        {
            writer.WriteLine("# " + comments);
        }
        foreach (var kvp in this)
        {
            writer.WriteLine($"{kvp.Key}={kvp.Value}");
        }
        writer.Flush();
    }
}
