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
using System.IO;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Dictionary;

/// <summary>
/// This class is a dictionary.
/// </summary>
public class Dictionary : IEnumerable<StringList> //, ISerializableArtifact
{
    private class StringListWrapper
    {
        private readonly StringList stringList;
        // NOpenNLP: upstream is a Java non-static inner class that reads the
        // outer Dictionary's isCaseSensitive field directly. C# inner classes
        // have no implicit outer reference, so the flag is passed in instead.
        private readonly bool isCaseSensitive;

        internal StringListWrapper(StringList stringList, bool isCaseSensitive)
        {
            this.stringList = stringList;
            this.isCaseSensitive = isCaseSensitive;
        }

        internal StringList GetStringList()
        {
            return stringList;
        }

        public override bool Equals(object? obj)
        {
            bool result;

            if (obj == this)
            {
                result = true;
            }
            else if (obj is StringListWrapper other)
            {
                result = isCaseSensitive
                    ? stringList.Equals(other.GetStringList())
                    : stringList.CompareToIgnoreCase(other.GetStringList());
            }
            else
            {
                result = false;
            }

            return result;
        }

        public override int GetHashCode()
        {
            // if lookup is too slow optimize this
            return StringUtil.ToLowerCase(stringList.ToString()).GetHashCode();
        }

        public override string ToString() => stringList.ToString();
    }

    // NOpenNLP: J2N's HashSet, not the BCL's, because Equals and GetHashCode below delegate to it.
    // Java's AbstractSet defines both structurally, so two dictionaries holding equal entries are
    // equal; the BCL HashSet inherits reference equality from object and would report them unequal.
    private readonly JCG.HashSet<StringListWrapper> entrySet = []; // NOpenNLP: made readonly
    private readonly bool isCaseSensitive;
    private int minTokenCount = 99999;
    private int maxTokenCount /* = 0 */;

    /// <summary>
    /// Initializes an empty <see cref="Dictionary"/>.
    /// </summary>
    public Dictionary() : this(false)
    {
    }

    public Dictionary(bool caseSensitive)
    {
        isCaseSensitive = caseSensitive;
    }

    /// <summary>
    /// Initializes the <see cref="Dictionary"/> from an existing dictionary resource.
    /// </summary>
    /// <param name="in"><see cref="System.IO.Stream"/></param>
    public Dictionary(Stream @in)
    {
        isCaseSensitive = DictionaryEntryPersistor.Create(@in, entry => Put(entry.Tokens));
    }

    /// <summary>
    /// Adds the tokens to the dictionary as one new entry.
    /// </summary>
    /// <param name="tokens">the new entry</param>
    public virtual void Put(StringList tokens)
    {
        entrySet.Add(new StringListWrapper(tokens, isCaseSensitive));
        minTokenCount = Math.Min(minTokenCount, tokens.Count);
        maxTokenCount = Math.Max(maxTokenCount, tokens.Count);
    }

    /// <summary>
    /// </summary>
    /// <returns>minimum token count in the dictionary</returns>
    public virtual int MinTokenCount => minTokenCount;

    /// <summary>
    /// </summary>
    /// <returns>maximum token count in the dictionary</returns>
    public virtual int MaxTokenCount => maxTokenCount;

    /// <summary>
    /// Checks if this dictionary has the given entry.
    /// </summary>
    /// <param name="tokens">query</param>
    /// <returns>true if it contains the entry otherwise false</returns>
    public virtual bool Contains(StringList tokens)
    {
        return entrySet.Contains(new StringListWrapper(tokens, isCaseSensitive));
    }

    /// <summary>
    /// Removes the given tokens form the current instance.
    /// </summary>
    /// <param name="tokens">filter tokens</param>
    public virtual void Remove(StringList tokens)
    {
        entrySet.Remove(new StringListWrapper(tokens, isCaseSensitive));
    }

    /// <summary>
    /// Retrieves an Iterator over all tokens.
    /// </summary>
    /// <returns>token-<see cref="System.Collections.Generic.IEnumerator{T}"/></returns>
    public virtual IEnumerator<StringList> GetEnumerator()
    {
        // NOpenNLP: upstream returns an anonymous Iterator; a C# iterator
        // block expresses the same traversal.
        foreach (var entry in entrySet)
        {
            yield return entry.GetStringList();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Retrieves the number of tokens in the current instance.
    /// </summary>
    /// <returns>number of tokens</returns>
    public virtual int Count => entrySet.Count;

    ///// <summary>
    ///// Writes the current instance to the given <see cref="System.IO.Stream"/>.
    ///// </summary>
    ///// <param name="out"><see cref="System.IO.Stream"/></param>
    ///// <exception cref="System.IO.IOException"></exception>
    //public virtual void Serialize(OutputStream @out)
    //{
    //    IEnumerator<Entry> entryIterator = new AnonymousIEnumerator1(this);
    //    DictionaryEntryPersistor.Serialize(@out, entryIterator, isCaseSensitive);
    //}

    //private sealed class AnonymousIEnumerator1 : IEnumerator
    //{
    //    public AnonymousIEnumerator1(StringListWrapper parent)
    //    {
    //        this.parent = parent;
    //    }

    //    private readonly StringListWrapper parent;
    //    private IEnumerator<StringList> dictionaryIterator = this.Iterator();
    //    public bool HasNext()
    //    {
    //        return dictionaryIterator.HasNext();
    //    }

    //    public Entry Next()
    //    {
    //        StringList tokens = dictionaryIterator.Next();
    //        return new Entry(tokens, new Attributes());
    //    }

    //    public void Remove()
    //    {
    //        throw new NotSupportedException();
    //    }
    //}

    public override bool Equals(object? obj)
    {
        bool result;
        if (obj == this)
        {
            result = true;
        }
        else if (obj is Dictionary dictionary)
        {
            result = entrySet.Equals(dictionary.entrySet);
        }
        else
        {
            result = false;
        }

        return result;
    }

    public override int GetHashCode() => entrySet.GetHashCode();

    public override string ToString() => entrySet.ToString();

    /// <summary>
    /// Reads a dictionary which has one entry per line. The tokens inside an
    /// entry are whitespace delimited.
    /// </summary>
    /// <param name="in"><see cref="System.IO.TextReader"/></param>
    /// <returns>the parsed dictionary</returns>
    /// <exception cref="IOException"></exception>
    public static Dictionary ParseOneEntryPerLine(TextReader @in)
    {
        // NOpenNLP: Java's Reader/BufferedReader map to TextReader, and
        // StringTokenizer to a whitespace split that drops empty entries.
        Dictionary dictionary = new Dictionary();
        while (@in.ReadLine() is { } line)
        {
            string[] tokens = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 0)
            {
                dictionary.Put(new StringList(tokens));
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Gets this dictionary as an <see cref="ISet{T}"/> of <see cref="string"/>. Only the
    /// enumerator, <c>Count</c> and <c>Contains</c> members are implemented.
    /// <para/>
    /// If this dictionary entries are multi tokens only the first token of the
    /// entry will be part of the set.
    /// </summary>
    /// <returns>a Set containing the entries of this dictionary</returns>
    public virtual ISet<string> AsStringSet()
    {
        // NOpenNLP: upstream returns an anonymous AbstractSet that implements
        // only iterator(), size() and contains(Object). The equivalent here is
        // a small read-only set view over the same entry set.
        return new StringSetView(entrySet, isCaseSensitive);
    }

    private sealed class StringSetView : ISet<string>
    {
        private readonly ISet<StringListWrapper> entrySet;
        private readonly bool isCaseSensitive;

        internal StringSetView(ISet<StringListWrapper> entrySet, bool isCaseSensitive)
        {
            this.entrySet = entrySet;
            this.isCaseSensitive = isCaseSensitive;
        }

        public IEnumerator<string> GetEnumerator()
        {
            foreach (var entry in entrySet)
            {
                yield return entry.GetStringList().GetToken(0);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => entrySet.Count;

        public bool Contains(string? item)
        {
            if (item is null)
            {
                return false;
            }

            return entrySet.Contains(new StringListWrapper(new StringList(item), isCaseSensitive));
        }

        public bool IsReadOnly => true;

        public void CopyTo(string[] array, int arrayIndex)
        {
            foreach (string item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        // NOpenNLP: the remaining members are not implemented upstream either.
        void ICollection<string>.Add(string item) => throw new NotSupportedException();

        bool ISet<string>.Add(string item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Remove(string item) => throw new NotSupportedException();

        public void ExceptWith(IEnumerable<string> other) => throw new NotSupportedException();

        public void IntersectWith(IEnumerable<string> other) => throw new NotSupportedException();

        public void SymmetricExceptWith(IEnumerable<string> other) => throw new NotSupportedException();

        public void UnionWith(IEnumerable<string> other) => throw new NotSupportedException();

        public bool IsProperSubsetOf(IEnumerable<string> other) => throw new NotSupportedException();

        public bool IsProperSupersetOf(IEnumerable<string> other) => throw new NotSupportedException();

        public bool IsSubsetOf(IEnumerable<string> other) => throw new NotSupportedException();

        public bool IsSupersetOf(IEnumerable<string> other) => throw new NotSupportedException();

        public bool Overlaps(IEnumerable<string> other) => throw new NotSupportedException();

        public bool SetEquals(IEnumerable<string> other) => throw new NotSupportedException();

        // NOpenNLP: upstream's view extends Java's AbstractSet, which defines Equals as
        // "same size, and contains every element of the other set" and GetHashCode as the sum
        // of the element hash codes. A C# class inherits neither, so both are spelled out here;
        // without them the view would fall back to reference equality and two views over equal
        // dictionaries would compare unequal.
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            if (obj is not ISet<string> other || other.Count != Count)
            {
                return false;
            }

            foreach (string item in other)
            {
                if (!Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            // Java sums the element hash codes, so the result does not depend on iteration order.
            // Overflow is part of that contract, and unchecked is the C# default, but it is
            // written out because this is arithmetic that is meant to wrap.
            unchecked
            {
                int hashCode = 0;

                foreach (string item in this)
                {
                    hashCode += item?.GetHashCode() ?? 0;
                }

                return hashCode;
            }
        }
    }

    ///// <summary>
    ///// Gets the Serializer Class for <see cref="Dictionary"/>
    ///// </summary>
    ///// <returns><see cref="DictionarySerializer"/></returns>
    //public virtual Class<TWildcardTodo> GetArtifactSerializerClass()
    //{
    //    return typeof(DictionarySerializer);
    //}
}
