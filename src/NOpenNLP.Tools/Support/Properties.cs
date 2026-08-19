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
using System.Globalization;
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
        // NOpenNLP: Java reads ISO-8859-1 and unescapes \uXXXX, which Store above
        // mirrors. detectEncodingFromByteOrderMarks is kept so a UTF-8 file carrying a
        // BOM -- which Java would not have written, but which a hand-edited manifest
        // may have -- is still read as UTF-8 rather than as mojibake.
        using var reader = new StreamReader(s, Latin1, detectEncodingFromByteOrderMarks: true,
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
                // NOpenNLP: a backslash escapes the next character, so an escaped
                // separator inside a key is not a separator. Store writes those.
                if (c == '\\')
                {
                    i++;
                    continue;
                }

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

                this[Unescape(key)] = Unescape(rest.Trim());
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
        // NOpenNLP: Java's Properties.store writes ISO-8859-1 and escapes everything
        // outside it as \uXXXX, so the file is pure ASCII in practice. Writing raw
        // UTF-8 instead would produce a manifest that Java's Properties.load decodes
        // as ISO-8859-1 and turns into mojibake -- "cafe" with an acute e becoming two
        // characters. Latin1 plus the escaping below reproduces Java's bytes exactly.
        //
        // Java's store also leaves the stream open, matching Load below. Without
        // leaveOpen the StreamWriter would close the caller's stream on dispose, which
        // breaks callers that write further entries afterwards -- TrainingParameters
        // .Serialize, and BaseModel.Serialize, which writes several artifacts into a
        // single zip stream.
        using var writer = new StreamWriter(s, Latin1, bufferSize: 1024, leaveOpen: true);
        if (!string.IsNullOrEmpty(comments))
        {
            writer.WriteLine("# " + comments);
        }
        foreach (var kvp in this)
        {
            writer.WriteLine(
                $"{Escape(kvp.Key.ToString()!, escapeSpace: true)}=" +
                $"{Escape(kvp.Value.ToString()!, escapeSpace: false)}");
        }
        writer.Flush();
    }

    // NOpenNLP: Java writes ISO-8859-1. Encoding.Latin1 is net5.0+, so the code page
    // is requested by number for the netstandard2.0 build.
    private static readonly Encoding Latin1 = Encoding.GetEncoding(28591);

    /// <summary>
    /// Reproduces the escaping of <c>java.util.Properties.store</c>: the key/value
    /// separators and the escape character are backslash-escaped, and anything
    /// outside printable ASCII becomes a <c>\uXXXX</c> sequence. A key also escapes
    /// spaces, since an unescaped space would end the key.
    /// </summary>
    private static string Escape(string value, bool escapeSpace)
    {
        var builder = new StringBuilder(value.Length * 2);

        foreach (char c in value)
        {
            switch (c)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '=':
                case ':':
                case '#':
                case '!':
                    builder.Append('\\').Append(c);
                    break;
                case ' ':
                    if (escapeSpace)
                    {
                        builder.Append('\\');
                    }
                    builder.Append(' ');
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                default:
                    if (c < 0x20 || c > 0x7E)
                    {
                        builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Reverses <see cref="Escape"/>, matching <c>java.util.Properties.load</c>.
    /// </summary>
    private static string Unescape(string value)
    {
        if (value.IndexOf('\\') < 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c != '\\' || i + 1 >= value.Length)
            {
                builder.Append(c);
                continue;
            }

            char next = value[++i];
            switch (next)
            {
                case 't':
                    builder.Append('\t');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case 'u' when i + 4 < value.Length
                    && int.TryParse(value.Substring(i + 1, 4), NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out int code):
                    builder.Append((char)code);
                    i += 4;
                    break;
                default:
                    // Java drops the backslash before any other character, so "\=" is
                    // "=" and "\q" is "q".
                    builder.Append(next);
                    break;
            }
        }

        return builder.ToString();
    }
}
