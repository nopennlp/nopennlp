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
using NOpenNLP.Tools.Support;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// Returns a <see cref="RegexNameFinder"/> based on a selection of
/// defaults or a configuration and a selection of defaults.
/// </summary>
public class RegexNameFinderFactory
{
    private static readonly object syncLock = new object();

    /// <summary>
    /// Allows for use of selected defaults as well as regexes from external
    /// configuration.
    /// </summary>
    /// <param name="config">A map where the key is a type, and the value is a
    /// <see cref="Regex"/>[]. If the keys clash with default keys, the config
    /// map will win.</param>
    /// <param name="defaults">The OpenNLP default regexes.</param>
    /// <returns>A <see cref="RegexNameFinder"/>.</returns>
    public static RegexNameFinder GetDefaultRegexNameFinders(
        IDictionary<string, Regex[]> config, params DefaultRegexNameFinder[] defaults)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config), "config must not be null");
        }

        lock (syncLock)
        {
            IDictionary<string, Regex[]> defaultsToMap = new JCG.Dictionary<string, Regex[]>();
            if (defaults != null)
            {
                defaultsToMap = DefaultsToMap(defaults);
            }

            foreach (KeyValuePair<string, Regex[]> entry in config)
            {
                defaultsToMap.Put(entry.Key, entry.Value);
            }

            return new RegexNameFinder(defaultsToMap);
        }
    }

    /// <summary>
    /// Returns a <see cref="RegexNameFinder"/> that will utilize specified default regexes.
    /// </summary>
    /// <param name="defaults">The OpenNLP default regexes.</param>
    /// <returns>A <see cref="RegexNameFinder"/>.</returns>
    public static RegexNameFinder GetDefaultRegexNameFinders(params DefaultRegexNameFinder[] defaults)
    {
        if (defaults == null)
        {
            throw new ArgumentNullException(nameof(defaults), "defaults must not be null");
        }

        lock (syncLock)
        {
            return new RegexNameFinder(DefaultsToMap(defaults));
        }
    }

    private static IDictionary<string, Regex[]> DefaultsToMap(params DefaultRegexNameFinder[] defaults)
    {
        lock (syncLock)
        {
            IDictionary<string, Regex[]> regexMap = new JCG.Dictionary<string, Regex[]>();
            foreach (DefaultRegexNameFinder def in defaults)
            {
                foreach (KeyValuePair<string, Regex[]> entry in def.RegexMap)
                {
                    regexMap.Put(entry.Key, entry.Value);
                }
            }

            return regexMap;
        }
    }

    public interface IRegexAble
    {
        IDictionary<string, Regex[]> RegexMap { get; }

        string Type { get; }
    }

    // NOpenNLP: upstream is a Java enum whose constants each override
    // getRegexMap()/getType(). C# enums cannot carry behavior, so this is a
    // sealed class exposing the same constants as static readonly instances.
    public sealed class DefaultRegexNameFinder : IRegexAble
    {
        public static readonly DefaultRegexNameFinder USA_PHONE_NUM = new DefaultRegexNameFinder(
            "PHONE_NUM",
            [new Regex("((\\(\\d{3}\\) ?)|(\\d{3}-))?\\d{3}-\\d{4}", RegexOptions.Compiled)]);

        public static readonly DefaultRegexNameFinder EMAIL = new DefaultRegexNameFinder(
            "EMAIL",
            [
                new Regex("([a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*" +
                    "|\"([\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09" +
                    "\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9]([a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]" +
                    "*[a-z0-9])?|\\[((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]" +
                    "?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]" +
                    "|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled)
            ]);

        public static readonly DefaultRegexNameFinder URL = new DefaultRegexNameFinder(
            "URL",
            [
                new Regex("\\b(((ht|f)tp(s?)\\:\\/\\/|~\\/|\\/)|www.)"
                    + "(\\w+:\\w+@)?(([-\\w]+\\.)+(com|org|net|gov"
                    + "|mil|biz|info|mobi|name|aero|jobs|museum"
                    + "|travel|[a-z]{2}))(:[\\d]{1,5})?"
                    + "(((\\/([-\\w~!$+|.,=]|%[a-f\\d]{2})+)+|\\/)+|\\?|#)?"
                    + "((\\?([-\\w~!$+|.,*:]|%[a-f\\d{2}])+=?"
                    + "([-\\w~!$+|.,*:=]|%[a-f\\d]{2})*)"
                    + "(&(?:[-\\w~!$+|.,*:]|%[a-f\\d{2}])+=?"
                    + "([-\\w~!$+|.,*:=]|%[a-f\\d]{2})*)*)*"
                    + "(#([-\\w~!$+|.,*:=]|%[a-f\\d]{2})*)?\\b",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled)
            ]);

        public static readonly DefaultRegexNameFinder MGRS = new DefaultRegexNameFinder(
            "MGRS",
            [
                new Regex("\\d{1,2}[A-Za-z]\\s*[A-Za-z]{2}\\s*\\d{1,5}\\s*\\d{1,5}",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled)
            ]);

        public static readonly DefaultRegexNameFinder DEGREES_MIN_SEC_LAT_LON = new DefaultRegexNameFinder(
            "DEGREES_MIN_SEC_LAT_LON",
            [
                new Regex("([-|\\+]?\\d{1,3}[d|D|\\u00B0|\\s](\\s*\\d{1,2}['|\\u2019|\\s])" +
                    "?(\\s*\\d{1,2}[\\\"|\\u201d])?\\s*[N|n|S|s]?)(\\s*|,|,\\s*)([-|\\+]?\\d{1,3}[d|D|\\u00B0|" +
                    "\\s](\\s*\\d{1,2}['|\\u2019|\\s])?(\\s*\\d{1,2}[\\\"|\\u201d])?\\s*[E|e|W|w]?)",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled)
            ]);

        private readonly Regex[] patterns;

        private DefaultRegexNameFinder(string type, Regex[] patterns)
        {
            Type = type;
            this.patterns = patterns;
        }

        public string Type { get; }

        public IDictionary<string, Regex[]> RegexMap
        {
            get
            {
                IDictionary<string, Regex[]> regexMap = new JCG.Dictionary<string, Regex[]>();
                regexMap.Put(Type, patterns);
                return regexMap;
            }
        }

        public override string ToString() => Type;
    }
}
