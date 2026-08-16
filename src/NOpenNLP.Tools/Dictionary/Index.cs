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
using NOpenNLP.Tools.Util;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Dictionary;

/// <summary>
/// This classes indexes <see cref="StringList"/>s. This makes it possible
/// to check if a certain token is contained in at least one of the
/// <see cref="StringList"/>s.
/// </summary>
public class Index
{
    // NOpenNLP: made readonly
    private readonly JCG.HashSet<string> tokens = new JCG.HashSet<string>();

    /// <summary>
    /// Initializes the current instance with the given
    /// <see cref="StringList"/> <see cref="IEnumerator{T}"/>.
    /// </summary>
    /// <param name="tokenLists">The token lists to index.</param>
    public Index(IEnumerator<StringList> tokenLists)
    {
        while (tokenLists.MoveNext())
        {
            StringList tokens = tokenLists.Current;

            for (int i = 0; i < tokens.Count; i++)
            {
                this.tokens.Add(tokens.GetToken(i));
            }
        }
    }

    /// <summary>
    /// Checks if at least one <see cref="StringList"/> contains the
    /// given token.
    /// </summary>
    /// <param name="token">The token to look for.</param>
    /// <returns><c>true</c> if the token is contained, otherwise <c>false</c>.</returns>
    public virtual bool Contains(string token) => tokens.Contains(token);
}
