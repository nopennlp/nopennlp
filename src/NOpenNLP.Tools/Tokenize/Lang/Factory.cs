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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Tokenize.Lang;

public class Factory
{
    public const string DEFAULT_ALPHANUMERIC = "^[A-Za-z0-9]+$";

    /// <summary>
    /// Gets the alphanumeric pattern for the language. Please save the value
    /// locally because this call is expensive.
    /// </summary>
    /// <param name="languageCode">
    ///          the language code. If null or unknow the default pattern will be
    ///          returned.</param>
    /// <returns>the alphanumeric pattern for the language or the default pattern.</returns>
    public virtual Regex GetAlphanumeric(string? languageCode)
    {
        if ("pt".Equals(languageCode) || "por".Equals(languageCode))
        {
            return new Regex("^[0-9a-záãâàéêíóõôúüçA-ZÁÃÂÀÉÊÍÓÕÔÚÜÇ]+$");
        }

        return new Regex(DEFAULT_ALPHANUMERIC);
    }

    public virtual ITokenContextGenerator CreateTokenContextGenerator(string? languageCode, ISet<string> abbreviations)
    {
        return new DefaultTokenContextGenerator(abbreviations);
    }
}
