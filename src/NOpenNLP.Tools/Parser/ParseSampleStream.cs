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
using System.Linq;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Parser;

public class ParseSampleStream(IObjectStream<string?> @in)
    : FilterObjectStream<string?, Parse?>(@in)
{
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override Parse? Read()
    {
        // NOpenNLP specific: tail-recursive call replaced with loop
        while (true)
        {
            string? parse = samples.Read();

            if (parse != null)
            {
                // NOpenNLP: Java's String.trim() strips only characters <= U+0020,
                // whereas .NET's Trim() strips all Unicode whitespace. A line made up
                // of, say, NBSP (U+00A0) is non-blank to upstream and would be parsed,
                // so the Java definition is used here rather than skipping the line.
                if (!IsJavaBlank(parse))
                {
                    return Parse.ParseParse(parse);
                }
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// NOpenNLP: equivalent of <c>value.trim().isEmpty()</c> in Java, whose
    /// <c>String.trim()</c> treats only characters &lt;= U+0020 as trimmable.
    /// </summary>
    private static bool IsJavaBlank(string value) => value.All(c => c <= ' ');
}
