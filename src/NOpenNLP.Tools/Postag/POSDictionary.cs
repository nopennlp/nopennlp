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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Dictionary.Serializer;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// Provides a means of determining which tags are valid for a particular word
/// based on a tag dictionary read from a file.
/// </summary>
public class POSDictionary : IEnumerable<string>, IMutableTagDictionary, ISerializableArtifact
{
    private JCG.Dictionary<string, string[]> dictionary;
    private bool caseSensitive /* = true */; // NOpenNLP note: `= true` is always overwritten

    /// <summary>
    /// Initializes an empty case sensitive <see cref="POSDictionary"/>.
    /// </summary>
    public POSDictionary() : this(true)
    {
    }

    /// <summary>
    /// Initializes an empty <see cref="POSDictionary"/>.
    /// </summary>
    /// <param name="caseSensitive">the <see cref="POSDictionary"/> case sensitivity</param>
    public POSDictionary(bool caseSensitive)
    {
        dictionary = new JCG.Dictionary<string, string[]>();
        this.caseSensitive = caseSensitive;
    }

    /// <summary>
    /// Returns a list of valid tags for the specified word.
    /// </summary>
    /// <param name="word">The word.</param>
    /// <returns>A list of valid tags for the specified word or
    ///     null if no information is available for that word.</returns>
    public virtual string[]? GetTags(string word)
    {
        // NOpenNLP: Java's Map.get returns null for an absent word, which is the
        // documented "no information available" result. The C# indexer throws
        // instead, so the lookup goes through TryGetValue.
        string key = caseSensitive ? word : StringUtil.ToLowerCase(word);
        return dictionary.TryGetValue(key, out string[]? tags) ? tags : null;
    }

    // /// <summary>
    // /// Associates the specified tags with the specified word. If the dictionary
    // /// previously contained the word, the old tags are replaced by the specified
    // /// ones.
    // /// </summary>
    // /// <param name="word">
    // ///          The word to be added to the dictionary.</param>
    // /// <param name="tags">
    // ///          The set of tags associated with the specified word.</param>
    // /// <remarks>Deprecated: Use <c>Put(string, string[])</c> instead.</remarks>
    // // NOpenNLP: upstream is package-private and its varargs signature collides
    // // with Put(string, string[]) in C#; it is deprecated in favor of Put anyway.
    // internal virtual void AddTags(string word, params string[] tags)
    // {
    //     Put(word, tags);
    // }

    /// <summary>
    /// Retrieves an iterator over all words in the dictionary.
    /// </summary>
    public virtual IEnumerator<string> GetEnumerator() => dictionary.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static string TagsToString(string[] tags)
    {
        var tagString = new StringBuilder();
        foreach (string tag in tags)
        {
            tagString.Append(tag);
            tagString.Append(' ');
        }

        // remove last space
        if (tagString.Length > 0)
        {
            tagString.Length -= 1;
        }

        return tagString.ToString();
    }

    ///// <summary>
    ///// Writes the <see cref="POSDictionary"/> to the given <see cref="System.IO.Stream"/>;
    ///// <para/>
    ///// After the serialization is finished the provided
    ///// <see cref="System.IO.Stream"/> remains open.
    ///// </summary>
    ///// <param name="out">
    /////            the <see cref="System.IO.Stream"/> to write the dictionary into.</param>
    ///// <exception cref="System.IO.IOException">
    /////             if writing to the <see cref="System.IO.Stream"/> fails</exception>
    // public virtual void Serialize(Stream @out)
    // {
    //     IEnumerator<Entry> entries = new AnonymousIEnumerator(this);
    //     DictionaryEntryPersistor.Serialize(@out, entries, caseSensitive);
    // }

    // private sealed class AnonymousIEnumerator : IEnumerator
    // {
    //     public AnonymousIEnumerator(POSDictionary parent)
    //     {
    //         this.parent = parent;
    //     }
//
    //     private readonly POSDictionary parent;
    //     IEnumerator<string> iterator = dictionary.KeySet().Iterator();
    //     public bool HasNext()
    //     {
    //         return iterator.HasNext();
    //     }
//
    //     public Entry Next()
    //     {
    //         string word = iterator.Next();
    //         Attributes tagAttribute = new Attributes();
    //         tagAttribute.SetValue("tags", TagsToString(GetTags(word)));
    //         return new Entry(new StringList(word), tagAttribute);
    //     }
//
    //     public void Remove()
    //     {
    //         throw new NotSupportedException();
    //     }
    // }

    public override int GetHashCode()
    {
        int[] keyHashes = new int[dictionary.Count];
        int[] valueHashes = new int[dictionary.Count];
        int i = 0;
        foreach (string word in this)
        {
            keyHashes[i] = word.GetHashCode();
            valueHashes[i] = Arrays.GetHashCode(GetTags(word));
            i++;
        }

        Array.Sort(keyHashes);
        Array.Sort(valueHashes);
        return HashCode.Combine(Arrays.GetHashCode(keyHashes), Arrays.GetHashCode(valueHashes));
    }

    public override bool Equals(object? obj)
    {
        if (obj == this)
        {
            return true;
        }

        if (obj is POSDictionary posDictionary)
        {
            if (this.dictionary.Count == posDictionary.dictionary.Count)
            {
                foreach (string word in this)
                {
                    if (!Arrays.Equals(GetTags(word), posDictionary.GetTags(word)))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        // it is time consuming to output the dictionary entries.
        // will output something meaningful for debugging, like
        // POSDictionary{size=100, caseSensitive=true}

        // NOpenNLP: Java renders a boolean as lowercase "true"/"false"; C# renders
        // "True"/"False". The lowercase form is reproduced to match upstream output.
        string caseSensitiveText = this.caseSensitive ? "true" : "false";
        return $"POSDictionary{{size={dictionary.Count}, caseSensitive={caseSensitiveText}}}";
    }

    /// <summary>
    /// Creates a new <see cref="POSDictionary"/> from a provided <see cref="System.IO.Stream"/>.
    ///
    /// After creation is finished the provided <see cref="System.IO.Stream"/> is closed.
    /// </summary>
    /// <param name="in"></param>
    /// <returns>the pos dictionary</returns>
    /// <exception cref="IOException"></exception>
    /// <exception cref="InvalidFormatException"></exception>
    public static POSDictionary Create(Stream @in)
    {
        POSDictionary newPosDict = new POSDictionary();
        bool isCaseSensitive = DictionaryEntryPersistor.Create(@in, (entry) =>
        {
            string tagString = entry.Attributes.GetValue("tags");
            string[] tags = tagString.Split(' ');
            StringList word = entry.Tokens;
            if (word.Count != 1)
                throw new InvalidFormatException("Each entry must have exactly one token! " + word);
            newPosDict.dictionary.Put(word.GetToken(0), tags);
        });
        newPosDict.caseSensitive = isCaseSensitive;

        // TODO: The dictionary API needs to be improved to do this better!
        if (!isCaseSensitive)
        {
            var lowerCasedDictionary = new JCG.Dictionary<string, string[]>();
            foreach (var entry in newPosDict.dictionary)
            {
                lowerCasedDictionary[StringUtil.ToLowerCase(entry.Key)] = entry.Value;
            }

            newPosDict.dictionary = lowerCasedDictionary;
        }

        return newPosDict;
    }

    public virtual string[]? Put(string word, params string[] tags)
    {
        if (this.caseSensitive)
        {
            return dictionary.Put(word, tags);
        }
        else
        {
            return dictionary.Put(StringUtil.ToLowerCase(word), tags);
        }
    }

    public virtual bool IsCaseSensitive => caseSensitive;

    public virtual Type ArtifactSerializerClass => typeof(POSTaggerFactory.POSDictionarySerializer);
}
