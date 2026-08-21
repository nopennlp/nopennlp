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

using System.IO;
using System.Text;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// The command line parameters shared by the corpus formats.
/// </summary>
// NOpenNLP: these are the interfaces in opennlp.tools.cmdline.params that the format
// factories use -- EncodingParameter, BasicFormatParams, LanguageParams and
// DetokenizerParameter. Upstream composes them by interface inheritance and reads them
// through a reflection proxy; here each is a shared parameter descriptor a factory
// lists in its Parameters. The names, value names, descriptions and defaults are copied
// verbatim from the upstream annotations, since they are the user-facing contract.
public static class FormatParameters
{
    /// <summary>
    /// The sentinel upstream's <c>@OptionalParameter</c> uses to mean "the platform
    /// default encoding". A user may type it literally, and upstream's
    /// <c>CharsetArgumentFactory</c> resolves it the same way.
    /// </summary>
    public const string DefaultCharset = "DEFAULT_CHARSET";

    /// <summary>
    /// <c>-encoding</c>, from <c>EncodingParameter</c>.
    /// </summary>
    public static readonly IFormatParameter Encoding =
        FormatParameter<string>.Optional("-encoding", "charsetName",
            "encoding for reading and writing text, if absent the system default is used.",
            DefaultCharset);

    /// <summary>
    /// <c>-data</c>, from <c>BasicFormatParams</c>.
    /// </summary>
    public static readonly IFormatParameter Data =
        new FormatParameter<FileInfo>("-data", "sampleData",
            "data to be used, usually a file name.");

    /// <summary>
    /// <c>-lang</c>, from <c>LanguageParams</c>.
    /// </summary>
    public static readonly IFormatParameter Lang =
        new FormatParameter<string>("-lang", "language",
            "language which is being processed.");

    /// <summary>
    /// <c>-detokenizer</c>, from <c>DetokenizerParameter</c>.
    /// </summary>
    public static readonly IFormatParameter Detokenizer =
        new FormatParameter<string>("-detokenizer", "dictionary",
            "specifies the file with detokenizer dictionary.");

    /// <summary>
    /// Resolves the <c>-encoding</c> value, mapping the
    /// <see cref="DefaultCharset"/> sentinel onto the platform default.
    /// </summary>
    // NOpenNLP: upstream's CharsetArgumentFactory does this translation while parsing,
    // and rejects an unsupported name with TerminateToolException(1). Encoding is not
    // one of the value types a parameter can carry here -- the CLI would have to
    // teach System.CommandLine to parse one -- so the parameter is a string and this
    // resolves it at the point of use, with the same error code and message shape.
    public static Encoding ResolveEncoding(string? charsetName)
    {
        if (charsetName is null || DefaultCharset.Equals(charsetName, System.StringComparison.Ordinal))
        {
            return System.Text.Encoding.Default;
        }

        try
        {
            return System.Text.Encoding.GetEncoding(charsetName);
        }
        catch (System.ArgumentException e)
        {
            throw new TerminateToolException(1,
                string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Invalid argument: {0} {1} \nEncoding {1} is not supported on this platform.",
                    "-encoding", charsetName), e);
        }
    }
}
