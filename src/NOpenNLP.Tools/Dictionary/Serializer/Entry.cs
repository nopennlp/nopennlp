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

namespace NOpenNLP.Tools.Dictionary.Serializer;

/// <summary>
/// An <see cref="Entry"/> is a <see cref="StringList"/> which can
/// optionally be mapped to attributes.
///
/// <see cref="Entry"/>s is a read and written by the <see cref="DictionaryEntryPersistor"/>.
/// </summary>
/// <remarks>
/// <seealso cref="DictionaryEntryPersistor"/>
/// <seealso cref="Attributes"/>
/// </remarks>
public class Entry(StringList tokens, Attributes? attributes)
{
    /// <summary>
    /// Retrieves the tokens.
    /// </summary>
    /// <returns>the tokens</returns>
    public virtual StringList Tokens => tokens;

    /// <summary>
    /// Retrieves the <see cref="Attributes"/>.
    /// </summary>
    /// <returns>the <see cref="Attributes"/></returns>
    public virtual Attributes? Attributes => attributes;
}
