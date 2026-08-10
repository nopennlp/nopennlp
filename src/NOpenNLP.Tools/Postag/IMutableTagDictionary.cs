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

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// Interface that allows <see cref="ITagDictionary"/> entries to be added and removed.
/// This can be used to induce the dictionary from training data.
/// </summary>
public interface IMutableTagDictionary : ITagDictionary
{
    /// <summary>
    /// Associates the specified tags with the specified word. If the dictionary
    /// previously contained keys for the word, the old tags are replaced by the
    /// specified tags.
    /// </summary>
    /// <param name="word">
    ///          word with which the specified tags is to be associated</param>
    /// <param name="tags">
    ///          tags to be associated with the specified word</param>
    /// <returns>the previous tags associated with the word, or null if there was no
    ///         mapping for word.</returns>
    string[] Put(string word, params string[] tags);

    /// <summary>
    /// Whether if the dictionary is case sensitive or not
    /// </summary>
    /// <returns>true if the dictionary is case sensitive</returns>
    // TODO: move to ITagDictionary, can't do it now because of backward
    // compatibility.
    bool IsCaseSensitive();
}
