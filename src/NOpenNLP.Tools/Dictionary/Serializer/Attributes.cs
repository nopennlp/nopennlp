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
using System;
using System.Collections;
using System.Collections.Generic;
using NOpenNLP.Tools.Support;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Dictionary.Serializer;

/// <summary>
/// The <see cref="Attributes"/> class stores name value pairs.
///
/// Problem: The HashMap for storing the name value pairs has a very high
/// memory footprint, replace it.
/// </summary>
public class Attributes : IEnumerable<string>
{
    private readonly JCG.Dictionary<string, string> mNameValueMap = new(); // NOpenNLP-specific: made readonly

    /// <summary>
    /// Retrieves the value for the given key or null if attribute it not set.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>the value</returns>
    public virtual string? GetValue(string key)
    {
        // NOpenNLP: Java's Map.get returns null when the attribute is not set,
        // which is what this method documents. The C# indexer throws instead,
        // so the lookup goes through TryGetValue.
        return mNameValueMap.TryGetValue(key, out string? value) ? value : null;
    }

    /// <summary>
    /// Sets a key/value pair.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public virtual void SetValue(string key, string value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        mNameValueMap.Put(key, value);
    }

    /// <summary>
    /// Iterates over the keys.
    /// </summary>
    /// <returns>key-<see cref="System.Collections.Generic.IEnumerator{T}"/></returns>
    public virtual IEnumerator<string> GetEnumerator() => mNameValueMap.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
