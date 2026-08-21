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
using NOpenNLP.Tools.Parser;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Ontonotes;

// Should be possible with this one, to train the parser and pos tagger!
public class OntoNotesParseSampleStream(IObjectStream<string?> samples)
    : FilterObjectStream<string?, Parse?>(samples)
{
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override Parse? Read()
    {
        var parseString = new StringBuilder();

        while (true)
        {
            string? parse = samples.Read();

            parse = parse?.Trim();

            if (string.IsNullOrEmpty(parse))
            {
                return parseString.Length > 0 ? Parse.ParseParse(parseString.ToString()) : null;
            }

            parseString.Append(parse).Append(' ');
        }
    }
}
