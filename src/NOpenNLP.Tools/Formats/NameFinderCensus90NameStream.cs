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

using System;
using System.IO;
using System.Text;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// This class helps to read the US Census data from the files to build a
/// <see cref="StringList"/> for each dictionary entry in the name-finder dictionary.
/// The entries in the source file are as follows:
/// <para/>
///      SMITH          1.006  1.006      1
/// <list type="bullet">
/// <item><description>The first field is the name (in ALL CAPS).</description></item>
/// <item><description>The next field is a frequency in percent.</description></item>
/// <item><description>The next is a cumulative frequency in percent.</description></item>
/// <item><description>The last is a ranking.</description></item>
/// </list>
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class NameFinderCensus90NameStream : ObjectStreamBase<StringList?>
{
    // NOpenNLP: upstream keeps a java.util.Locale field set to English and passes it to
    // every toUpperCase/toLowerCase call. StringUtil.ToUpperCase/ToLowerCase are
    // invariant, which is what an English locale amounts to for this ASCII census data,
    // so the field has no C# counterpart and is omitted.
    private readonly Encoding encoding;
    private readonly IObjectStream<string?> lineStream;

    /// <summary>
    /// This constructor takes an <see cref="IObjectStream{T}"/> and initializes the class to handle
    /// the stream.
    /// </summary>
    /// <param name="lineStream">an <c>IObjectStream&lt;string&gt;</c> that represents the
    /// input file to be attached to this class.</param>
    public NameFinderCensus90NameStream(IObjectStream<string?> lineStream)
    {
        // NOpenNLP: upstream uses Charset.defaultCharset(); Encoding.Default is the
        // .NET counterpart. The field is never read, matching upstream's own
        // "todo how do we find the encoding for an already open ObjectStream() ?".
        encoding = Encoding.Default;
        this.lineStream = lineStream;
    }

    /// <summary>
    /// This constructor takes an <see cref="IInputStreamFactory"/> and an <see cref="Encoding"/>
    /// and opens an associated stream object with the specified encoding specified.
    /// </summary>
    /// <param name="in">an <see cref="IInputStreamFactory"/> for the input file.</param>
    /// <param name="encoding">the <see cref="Encoding"/> to apply to the input stream.</param>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public NameFinderCensus90NameStream(IInputStreamFactory @in, Encoding encoding)
    {
        this.encoding = encoding;
        lineStream = new PlainTextByLineStream(@in, this.encoding);
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override StringList? Read()
    {
        string? line = lineStream.Read();
        StringList? name = null;

        if (line != null && !StringUtil.IsEmpty(line))
        {
            string name2;
            // find the location of the name separator in the line of data.
            int pos = line.IndexOf(' ');
            if (pos != -1)
            {
                string parsed = line.Substring(0, pos);
                // the data is in ALL CAPS ... so the easiest way is to convert
                // back to standard mixed case.
                if (parsed.Length > 2 && parsed.StartsWith("MC", StringComparison.Ordinal))
                {
                    name2 = StringUtil.ToUpperCase(parsed.Substring(0, 1)) +
                            StringUtil.ToLowerCase(parsed.Substring(1, 1)) +
                            StringUtil.ToUpperCase(parsed.Substring(2, 1)) +
                            StringUtil.ToLowerCase(parsed.Substring(3));
                }
                else
                {
                    name2 = StringUtil.ToUpperCase(parsed.Substring(0, 1)) +
                            StringUtil.ToLowerCase(parsed.Substring(1));
                }

                name = new StringList(name2);
            }
        }

        return name;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    public override void Reset() => lineStream.Reset();

    protected override void Dispose(bool disposing) => lineStream.Dispose();
}
