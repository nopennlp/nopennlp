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

using System.Collections;
using System.Collections.Generic;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ngram;

/// <summary>
/// The <see cref="NGramCharModel"/> can be used to create character ngrams.
/// </summary>
/// <seealso cref="NGramModel"/>
public class NGramCharModel : IEnumerable<string>
{
    // NOpenNLP: unused const; commented out
    // protected const string COUNT = "count";

    // NOpenNLP: made readonly
    private readonly JCG.Dictionary<string, int> mNGrams = new();

    /// <summary>
    /// Initializes an empty instance.
    /// </summary>
    public NGramCharModel()
    {
    }

    /// <summary>
    /// Retrieves the count of the given ngram.
    /// </summary>
    /// <param name="ngram">an ngram</param>
    /// <returns>count of the ngram or 0 if it is not contained</returns>
    public virtual int GetCount(string ngram) =>
        mNGrams.TryGetValue(ngram, out int count) ? count : 0;

    /// <summary>
    /// Sets the count of an existing ngram.
    /// </summary>
    /// <param name="ngram"></param>
    /// <param name="count"></param>
    public virtual void SetCount(string ngram, int count)
    {
        // NOpenNLP: upstream puts the value first, then removes the key and throws when the
        // ngram was absent; the put is reproduced here so the observable behavior matches.
        bool existed = mNGrams.ContainsKey(ngram);
        mNGrams[ngram] = count;

        if (!existed)
        {
            mNGrams.Remove(ngram);
            throw new KeyNotFoundException();
        }
    }

    /// <summary>
    /// Adds one NGram, if it already exists the count increase by one.
    /// </summary>
    /// <param name="ngram"></param>
    public virtual void Add(string ngram)
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
    /// Adds a char sequence that will be ngrammed into chars.
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

                Add(gram);
            }
        }
    }

    /// <summary>
    /// Removes the specified tokens form the NGram model, they are just dropped.
    /// </summary>
    /// <param name="ngram"></param>
    public virtual void Remove(string ngram) => mNGrams.Remove(ngram);

    /// <summary>
    /// Checks if the given tokens are contained by the current instance.
    /// </summary>
    /// <param name="ngram"></param>
    /// <returns>true if the ngram is contained</returns>
    public virtual bool Contains(string ngram) => mNGrams.ContainsKey(ngram);

    /// <summary>
    /// Retrieves the number of <see cref="string"/> entries in the current instance.
    /// </summary>
    /// <returns>number of different grams</returns>
    public virtual int Count => mNGrams.Count;

    /// <summary>
    /// Retrieves an <see cref="IEnumerator{T}"/> over all <see cref="string"/> entries.
    /// </summary>
    /// <returns>iterator over all grams</returns>
    public IEnumerator<string> GetEnumerator() => mNGrams.Keys.GetEnumerator();

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
            // NOpenNLP: the Java iterator removes in place; .NET forbids mutating while enumerating.
            var toRemove = new List<string>();

            foreach (var ngram in this)
            {
                int count = GetCount(ngram);

                if (count < cutoffUnder ||
                    count > cutoffOver)
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

    public override bool Equals(object? obj)
    {
        bool result;

        if (obj == this)
        {
            result = true;
        }
        else if (obj is NGramCharModel model)
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
