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

using System.Text;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Stream filter which merges text lines into paragraphs. The boundary of paragraph is defined
/// by an empty text line. If the last paragraph in the stream is not terminated by an empty line
/// the left over is assumed to be a paragraph.
/// </summary>
public class ParagraphStream(IObjectStream<string?> lineStream)
    : FilterObjectStream<string?, string?>(lineStream)
{
    public override string? Read()
    {
        StringBuilder paragraph = new StringBuilder();

        while (true)
        {
            string? line = samples.Read();

            // The last paragraph in the input might not
            // be terminated well with a new line at the end.

            if (line == null || line.Equals(""))
            {
                if (paragraph.Length > 0)
                {
                    return paragraph.ToString();
                }
            }
            else
            {
                paragraph.Append(line).Append('\n');
            }

            if (line == null)
                return null;
        }
    }
}
