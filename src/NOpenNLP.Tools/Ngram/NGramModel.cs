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

using NOpenNLP.Tools.Dictionary.Serializer;
using NOpenNLP.Tools.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ngram;

/// <summary>
/// The <see cref="NGramModel"/> can be used to crate ngrams and character ngrams.
/// </summary>
/// <seealso cref="StringList"/>
public class NGramModel : IEnumerable<StringList>
{
    protected const string COUNT = "count";

    // NOpenNLP: made readonly. OrderedDictionary matches the LinkedHashMap upstream
    // adopted in OPENNLP-1321, so iteration order is insertion order.
    private readonly JCG.OrderedDictionary<StringList, int> mNGrams = new();

    /// <summary>
    /// Initializes an empty instance.
    /// </summary>
    public NGramModel()
    {
    }

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="in">the serialized model stream</param>
    /// <exception cref="IOException"/>
    public NGramModel(Stream @in)
    {
        DictionaryEntryPersistor.Create(@in, entry =>
        {
            int count;
            string? countValueString = null;
            try
            {
                countValueString = entry.Attributes?.GetValue(COUNT);
                if (countValueString == null)
                {
                    throw new InvalidFormatException("The count attribute must be set!");
                }

                // NOpenNLP: Java's Integer.parseInt is culture-invariant and rejects
                // surrounding whitespace and group separators, so the styles and
                // culture are given explicitly rather than taking int.Parse's
                // current-culture defaults.
                count = int.Parse(countValueString, NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                throw new InvalidFormatException(
                    "The count attribute '" + countValueString + "' must be a number!", e);
            }

            Add(entry.Tokens);
            SetCount(entry.Tokens, count);
        });
    }

    /// <summary>
    /// Retrieves the count of the given ngram.
    /// </summary>
    /// <param name="ngram">an ngram</param>
    /// <returns>count of the ngram or 0 if it is not contained</returns>
    public virtual int GetCount(StringList ngram)
    {
        if (mNGrams.TryGetValue(ngram, out int count))
        {
            return count;
        }

        return 0;
    }

    /// <summary>
    /// Sets the count of an existing ngram.
    /// </summary>
    /// <param name="ngram"></param>
    /// <param name="count"></param>
    public virtual void SetCount(StringList ngram, int count)
    {
        if (!mNGrams.ContainsKey(ngram))
        {
            throw new InvalidOperationException("Ngram does not exist");
        }

        mNGrams[ngram] = count;
    }

    /// <summary>
    /// Adds one NGram, if it already exists the count increase by one.
    /// </summary>
    /// <param name="ngram"></param>
    public virtual void Add(StringList ngram)
    {
        if (Contains(ngram))
        {
            SetCount(ngram, GetCount(ngram) + 1);
        }
        else
        {
            mNGrams[ngram] = 1;
        }
    }

    /// <summary>
    /// Adds NGrams up to the specified length to the current instance.
    /// </summary>
    /// <param name="ngram">the tokens to build the uni-grams, bi-grams, tri-grams, ..
    ///     from.</param>
    /// <param name="minLength">- minimal length</param>
    /// <param name="maxLength">- maximal length</param>
    public virtual void Add(StringList ngram, int minLength, int maxLength)
    {
        if (minLength < 1 || maxLength < 1)
            throw new ArgumentException("minLength and maxLength param must be at least 1. " + "minLength=" + minLength + ", maxLength= " + maxLength);
        if (minLength > maxLength)
            throw new ArgumentException("minLength param must not be larger than " + "maxLength param. minLength=" + minLength + ", maxLength= " + maxLength);
        for (int lengthIndex = minLength; lengthIndex < maxLength + 1; lengthIndex++)
        {
            for (int textIndex = 0; textIndex + lengthIndex - 1 < ngram.Count; textIndex++)
            {
                string[] grams = new string[lengthIndex];
                for (int i = textIndex; i < textIndex + lengthIndex; i++)
                {
                    grams[i - textIndex] = ngram.GetToken(i);
                }

                Add(new StringList(grams));
            }
        }
    }

    /// <summary>
    /// Adds character NGrams to the current instance.
    /// </summary>
    /// <param name="chars"></param>
    /// <param name="minLength"></param>
    /// <param name="maxLength"></param>
    public virtual void Add(string chars, int minLength, int maxLength)
    {
        for (int lengthIndex = minLength; lengthIndex < maxLength + 1; lengthIndex++)
        {
            for (int textIndex = 0; textIndex + lengthIndex - 1 < chars.Length; textIndex++)
            {
                string gram = StringUtil.ToLowerCase(chars.Substring(textIndex, lengthIndex));
                Add(new StringList([gram]));
            }
        }
    }

    /// <summary>
    /// Removes the specified tokens form the NGram model, they are just dropped.
    /// </summary>
    /// <param name="tokens"></param>
    public virtual void Remove(StringList tokens)
    {
        mNGrams.Remove(tokens);
    }

    /// <summary>
    /// Checks fit he given tokens are contained by the current instance.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns>true if the ngram is contained</returns>
    public virtual bool Contains(StringList tokens)
    {
        return mNGrams.ContainsKey(tokens);
    }

    /// <summary>
    /// Retrieves the number of <see cref="StringList"/> entries in the current instance.
    /// </summary>
    /// <returns>number of different grams</returns>
    public virtual int Count => mNGrams.Count;

    /// <summary>
    /// Retrieves an <see cref="IEnumerator{T}"/> over all <see cref="StringList"/> entries.
    /// </summary>
    /// <returns>iterator over all grams</returns>
    public IEnumerator<StringList> GetEnumerator() => mNGrams.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Retrieves the total count of all Ngrams.
    /// </summary>
    /// <returns>total count of all ngrams</returns>
    public virtual int NumberOfGrams
    {
        get
        {
            int counter = 0;
            foreach (var ngram in this)
            {
                counter += GetCount(ngram);
            }

            return counter;
        }
    }

    /// <summary>
    /// Deletes all ngram which do appear less than the cutoffUnder value
    /// and more often than the cutoffOver value.
    /// </summary>
    /// <param name="cutoffUnder"></param>
    /// <param name="cutoffOver"></param>
    public virtual void Cutoff(int cutoffUnder, int cutoffOver)
    {
        if (cutoffUnder > 0 || cutoffOver < int.MaxValue)
        {
            var toRemove = new List<StringList>();
            foreach (var ngram in this)
            {
                int count = GetCount(ngram);
                if (count < cutoffUnder || count > cutoffOver)
                {
                    toRemove.Add(ngram);
                }
            }
            foreach (var ngram in toRemove)
            {
                Remove(ngram);
            }
        }
    }

    /// <summary>
    /// Creates a dictionary which contain all <see cref="StringList"/> which
    /// are in the current <see cref="NGramModel"/>.
    ///
    /// Entries which are only different in the case are merged into one.
    ///
    /// Calling this method is the same as calling <c>ToDictionary(bool)</c> with true.
    /// </summary>
    /// <returns>a dictionary of the ngrams</returns>
    public virtual NOpenNLP.Tools.Dictionary.Dictionary ToDictionary() => ToDictionary(false);

    /// <summary>
    /// Creates a dictionary which contains all <see cref="StringList"/>s which
    /// are in the current <see cref="NGramModel"/>.
    /// </summary>
    /// <param name="caseSensitive">Specifies whether case distinctions should be kept
    ///                      in the creation of the dictionary.</param>
    /// <returns>a dictionary of the ngrams</returns>
    public virtual NOpenNLP.Tools.Dictionary.Dictionary ToDictionary(bool caseSensitive)
    {
        var dict = new NOpenNLP.Tools.Dictionary.Dictionary(caseSensitive);
        foreach (var stringList in this)
        {
            dict.Put(stringList);
        }

        return dict;
    }

    /// <summary>
    /// Writes the ngram instance to the given <see cref="Stream"/>.
    /// </summary>
    /// <param name="out">the <see cref="Stream"/> to write the ngram instance into.</param>
    /// <exception cref="IOException">if writing to the <see cref="Stream"/> fails</exception>
    public virtual void Serialize(Stream @out)
    {
        // NOpenNLP: upstream builds an anonymous Iterator over this model's own
        // iterator; a C# iterator method expresses the same thing directly.
        using IEnumerator<Entry> entryIterator = CreateEntries();
        DictionaryEntryPersistor.Serialize(@out, entryIterator, false);
    }

    private IEnumerator<Entry> CreateEntries()
    {
        foreach (StringList tokens in this)
        {
            Attributes attributes = new Attributes();

            attributes.SetValue(COUNT, GetCount(tokens).ToString(CultureInfo.InvariantCulture));

            yield return new Entry(tokens, attributes);
        }
    }

    public override bool Equals(object? obj)
    {
        bool result;
        if (obj == this)
        {
            result = true;
        }
        else if (obj is NGramModel model)
        {
            result = mNGrams.Equals(model.mNGrams);
        }
        else
        {
            result = false;
        }

        return result;
    }

    public override string ToString() => $"Size: {Count}";

    public override int GetHashCode() => mNGrams.GetHashCode();
}
