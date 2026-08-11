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

using NOpenNLP.Tools.Sentdetect.Lang;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;
using System;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Sentdetect;

/// <summary>
/// The factory that provides SentenceDetecor default implementations and
/// resources
/// </summary>
public class SentenceDetectorFactory : BaseToolFactory
{
    private string? languageCode;
    private char[]? eosCharacters;
    private NOpenNLP.Tools.Dictionary.Dictionary? abbreviationDictionary;
    private bool? useTokenEnd = null;
    private const string ABBREVIATIONS_ENTRY_NAME = "abbreviations.dictionary";
    private const string EOS_CHARACTERS_PROPERTY = "eosCharacters";
    private const string TOKEN_END_PROPERTY = "useTokenEnd";

    /// <summary>
    /// Creates a <see cref="SentenceDetectorFactory"/> that provides the default
    /// implementation of the resources.
    /// </summary>
    public SentenceDetectorFactory()
    {
    }

    /// <summary>
    /// Creates a <see cref="SentenceDetectorFactory"/>. Use this constructor to
    /// programmatically create a factory.
    /// </summary>
    /// <param name="languageCode"></param>
    /// <param name="abbreviationDictionary"></param>
    /// <param name="eosCharacters"></param>
    public SentenceDetectorFactory(string languageCode, bool useTokenEnd, NOpenNLP.Tools.Dictionary.Dictionary abbreviationDictionary, char[]? eosCharacters)
    {
        this.Init(languageCode, useTokenEnd, abbreviationDictionary, eosCharacters);
    }

    protected virtual void Init(string languageCode, bool useTokenEnd, NOpenNLP.Tools.Dictionary.Dictionary abbreviationDictionary, char[]? eosCharacters)
    {
        this.languageCode = languageCode;
        this.useTokenEnd = useTokenEnd;
        this.eosCharacters = eosCharacters;
        this.abbreviationDictionary = abbreviationDictionary;
    }

    public override void ValidateArtifactMap()
    {
        if (this.artifactProvider?.GetManifestProperty(TOKEN_END_PROPERTY) == null)
            throw new InvalidFormatException(TOKEN_END_PROPERTY + " is a mandatory property!");
        object abbreviationsEntry = this.artifactProvider.GetArtifact<NOpenNLP.Tools.Dictionary.Dictionary>(ABBREVIATIONS_ENTRY_NAME);
        if (abbreviationsEntry != null && abbreviationsEntry is not Dictionary.Dictionary)
        {
            throw new InvalidFormatException("Abbreviations dictionary '" + abbreviationsEntry + "' has wrong type, needs to be of type NOpenNLP.Tools.Dictionary.Dictionary!");
        }
    }

    public override IDictionary<string, object> CreateArtifactMap()
    {
        var artifactMap = base.CreateArtifactMap();

        // Abbreviations are optional
        if (abbreviationDictionary != null)
            artifactMap.Put(ABBREVIATIONS_ENTRY_NAME, abbreviationDictionary);
        return artifactMap;
    }

    public override IDictionary<string, string> CreateManifestEntries()
    {
        var manifestEntries = base.CreateManifestEntries();
        manifestEntries.Put(TOKEN_END_PROPERTY, IsUseTokenEnd.ToString());

        // EOS characters are optional
        if (EOSCharacters is { } ec)
            manifestEntries.Put(EOS_CHARACTERS_PROPERTY, EosCharArrayToString(ec));
        return manifestEntries;
    }

    public static SentenceDetectorFactory Create(string? subclassName, string languageCode, bool useTokenEnd, NOpenNLP.Tools.Dictionary.Dictionary abbreviationDictionary, char[] eosCharacters)
    {
        if (subclassName == null)
        {
            // will create the default factory
            return new SentenceDetectorFactory(languageCode, useTokenEnd, abbreviationDictionary, eosCharacters);
        }

        try
        {
            SentenceDetectorFactory? theFactory = ExtensionLoader.InstantiateExtension<SentenceDetectorFactory>(subclassName);
            theFactory.Init(languageCode, useTokenEnd, abbreviationDictionary, eosCharacters);
            return theFactory;
        }
        catch (Exception e)
        {
            string msg = $"Could not instantiate the {subclassName}. The initialization throw an exception.";
            Console.Error.WriteLine(msg);
            throw new InvalidFormatException(msg, e);
        }
    }

    public virtual char[]? EOSCharacters
    {
        get
        {
            if (this.eosCharacters == null)
            {
                if (artifactProvider != null)
                {
                    string? prop = this.artifactProvider.GetManifestProperty(EOS_CHARACTERS_PROPERTY);
                    if (prop != null)
                    {
                        this.eosCharacters = EosStringToCharArray(prop);
                    }
                }
                else
                {

                    // get from language dependent factory
                    Factory f = new Factory();
                    this.eosCharacters = f.GetEOSCharacters(languageCode);
                }
            }

            return this.eosCharacters;
        }
    }

    public virtual bool IsUseTokenEnd
    {
        get
        {
            if (this.useTokenEnd == null && artifactProvider != null)
            {
                this.useTokenEnd = bool.Parse(artifactProvider.GetManifestProperty(TOKEN_END_PROPERTY));
            }

            return this.useTokenEnd ?? true;
        }
    }

    public virtual NOpenNLP.Tools.Dictionary.Dictionary? AbbreviationDictionary
    {
        get
        {
            if (this.abbreviationDictionary == null && artifactProvider != null)
            {
                this.abbreviationDictionary = artifactProvider.GetArtifact<NOpenNLP.Tools.Dictionary.Dictionary>(ABBREVIATIONS_ENTRY_NAME);
            }

            return this.abbreviationDictionary;
        }
    }

    public virtual string LanguageCode
    {
        get
        {
            if (this.languageCode == null && artifactProvider != null)
            {
                this.languageCode = this.artifactProvider.Language;
            }

            return this.languageCode;
        }
    }

    public virtual IEndOfSentenceScanner EndOfSentenceScanner
    {
        get
        {
            Factory f = new Factory();
            char[] eosChars = EOSCharacters;
            if (eosChars is { Length: > 0 })
            {
                return f.CreateEndOfSentenceScanner(eosChars);
            }
            else
            {
                return f.CreateEndOfSentenceScanner(this.languageCode);
            }
        }
    }

    public virtual ISDContextGenerator GetSDContextGenerator()
    {
        Factory f = new Factory();
        char[]? eosChars = EOSCharacters;
        ISet<string> abbs;
        NOpenNLP.Tools.Dictionary.Dictionary? abbDict = AbbreviationDictionary;
        if (abbDict != null)
        {
            abbs = abbDict.AsStringSet();
        }
        else
        {
            abbs = new HashSet<string>();
        }

        if (eosChars is { Length: > 0 })
        {
            return f.CreateSentenceContextGenerator(abbs, eosChars);
        }
        else
        {
            return f.CreateSentenceContextGenerator(this.languageCode, abbs);
        }
    }

    private static string EosCharArrayToString(char[] eosCharacters) // NOpenNLP: made static
        => Convert.ToString(eosCharacters);

    private static char[] EosStringToCharArray(string eosCharacters) // NOpenNLP: made static
        => eosCharacters.ToCharArray();
}
