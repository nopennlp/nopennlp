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

using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Tokenize.Lang;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// The factory that provides <see cref="ITokenizer"/> default implementations and
/// resources. Users can extend this class if their application requires
/// overriding the <see cref="ITokenContextGenerator"/>, <see cref="NOpenNLP.Tools.Dictionary.Dictionary"/> etc.
/// </summary>
public class TokenizerFactory : BaseToolFactory
{
    private string? languageCode;
    private NOpenNLP.Tools.Dictionary.Dictionary? abbreviationDictionary;
    private bool useAlphaNumericOptimization /* = false */;
    private Regex? alphaNumericPattern;
    private const string ABBREVIATIONS_ENTRY_NAME = "abbreviations.dictionary";
    private const string USE_ALPHA_NUMERIC_OPTIMIZATION = "useAlphaNumericOptimization";
    private const string ALPHA_NUMERIC_PATTERN = "alphaNumericPattern";

    /// <summary>
    /// Creates a <see cref="TokenizerFactory"/> that provides the default implementation
    /// of the resources.
    /// </summary>
    public TokenizerFactory()
    {
    }

    /// <summary>
    /// Creates a <see cref="TokenizerFactory"/>. Use this constructor to
    /// programmatically create a factory.
    /// </summary>
    /// <param name="languageCode">
    ///          the language of the natural text</param>
    /// <param name="abbreviationDictionary">
    ///          an abbreviations dictionary</param>
    /// <param name="useAlphaNumericOptimization">
    ///          if true alpha numerics are skipped</param>
    /// <param name="alphaNumericPattern">
    ///          null or a custom alphanumeric pattern (default is:
    ///          "^[A-Za-z0-9]+$", provided by <see cref="Lang.Factory.DEFAULT_ALPHANUMERIC"/></param>
    public TokenizerFactory(string languageCode, NOpenNLP.Tools.Dictionary.Dictionary? abbreviationDictionary, bool useAlphaNumericOptimization, Regex alphaNumericPattern)
    {
        this.Init(languageCode, abbreviationDictionary, useAlphaNumericOptimization, alphaNumericPattern);
    }

    protected virtual void Init(string languageCode, NOpenNLP.Tools.Dictionary.Dictionary? abbreviationDictionary, bool useAlphaNumericOptimization, Regex alphaNumericPattern)
    {
        this.languageCode = languageCode;
        this.useAlphaNumericOptimization = useAlphaNumericOptimization;
        this.alphaNumericPattern = alphaNumericPattern;
        this.abbreviationDictionary = abbreviationDictionary;
    }

    public override void ValidateArtifactMap()
    {
        if (this.artifactProvider?.GetManifestProperty(USE_ALPHA_NUMERIC_OPTIMIZATION) == null)
            throw new InvalidFormatException(USE_ALPHA_NUMERIC_OPTIMIZATION + " is a mandatory property!");
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
        {
            artifactMap.Put(ABBREVIATIONS_ENTRY_NAME, abbreviationDictionary);
        }

        return artifactMap;
    }

    public override IDictionary<string, string> CreateManifestEntries()
    {
        var manifestEntries = base.CreateManifestEntries();
        manifestEntries.Put(USE_ALPHA_NUMERIC_OPTIMIZATION, UseAlphaNumericOptmization.ToString());

        // alphanumeric pattern is optional
        if (AlphaNumericPattern is { } alpha)
        {
            manifestEntries.Put(ALPHA_NUMERIC_PATTERN, alpha.ToString());
        }

        return manifestEntries;
    }

    /// <summary>
    /// Factory method the framework uses create a new <see cref="TokenizerFactory"/>.
    /// </summary>
    /// <param name="subclassName">the name of the class implementing the <see cref="TokenizerFactory"/></param>
    /// <param name="languageCode">the language code the tokenizer should use</param>
    /// <param name="abbreviationDictionary">an optional dictionary containing abbreviations, or null if not present</param>
    /// <param name="useAlphaNumericOptimization">indicate if the alpha numeric optimization
    ///     should be enabled or disabled</param>
    /// <param name="alphaNumericPattern">the pattern the alpha numeric optimization should use</param>
    /// <returns>the instance of the ITokenizer Factory</returns>
    /// <exception cref="InvalidFormatException">if once of the input parameters doesn't comply if the expected format</exception>
    public static TokenizerFactory? Create(string? subclassName, string languageCode, NOpenNLP.Tools.Dictionary.Dictionary? abbreviationDictionary, bool useAlphaNumericOptimization, Regex alphaNumericPattern)
    {
        if (subclassName == null)
        {
            // will create the default factory
            return new TokenizerFactory(languageCode, abbreviationDictionary, useAlphaNumericOptimization, alphaNumericPattern);
        }

        try
        {
            TokenizerFactory? theFactory = ExtensionLoader.InstantiateExtension<TokenizerFactory>(subclassName);
            theFactory?.Init(languageCode, abbreviationDictionary, useAlphaNumericOptimization, alphaNumericPattern);
            return theFactory;
        }
        catch (Exception e)
        {
            string msg = $"Could not instantiate the {subclassName}. The initialization throw an exception.";
            Console.Error.WriteLine(msg);
            throw new InvalidFormatException(msg, e);
        }
    }

    /// <summary>
    /// Gets the alpha numeric pattern.
    /// </summary>
    /// <returns>the user specified alpha numeric pattern or a default.</returns>
    public virtual Regex? AlphaNumericPattern
    {
        get
        {
            if (this.alphaNumericPattern == null)
            {
                string? prop = this.artifactProvider?.GetManifestProperty(ALPHA_NUMERIC_PATTERN);

                if (prop != null)
                {
                    this.alphaNumericPattern = new Regex(prop);
                }

                // could not load from manifest, will get from language dependent factory
                if (this.alphaNumericPattern == null)
                {
                    Factory f = new Factory();
                    this.alphaNumericPattern = f.GetAlphanumeric(languageCode);
                }
            }

            return this.alphaNumericPattern;
        }
    }

    /// <summary>
    /// Gets whether to use alphanumeric optimization.
    /// </summary>
    /// <returns>true if the alpha numeric optimization is enabled, otherwise false</returns>
    public virtual bool UseAlphaNumericOptmization
    {
        get
        {
            if (artifactProvider != null)
            {
                this.useAlphaNumericOptimization = bool.Parse(this.artifactProvider.GetManifestProperty(USE_ALPHA_NUMERIC_OPTIMIZATION));
            }

            return this.useAlphaNumericOptimization;
        }
    }

    /// <summary>
    /// Gets the abbreviation dictionary
    /// </summary>
    /// <returns>null or the abbreviation dictionary</returns>
    public virtual NOpenNLP.Tools.Dictionary.Dictionary? AbbreviationDictionary
    {
        get
        {
            if (this.abbreviationDictionary == null && artifactProvider != null)
            {
                this.abbreviationDictionary = this.artifactProvider.GetArtifact<NOpenNLP.Tools.Dictionary.Dictionary>(ABBREVIATIONS_ENTRY_NAME);
            }

            return this.abbreviationDictionary;
        }
    }

    /// <summary>
    /// Retrieves the language code.
    /// </summary>
    /// <returns>the language code</returns>
    public virtual string? LanguageCode
    {
        get
        {
            if (this.languageCode == null && this.artifactProvider != null)
            {
                this.languageCode = this.artifactProvider.Language;
            }

            return this.languageCode;
        }
    }

    /// <summary>
    /// Gets the context generator
    /// </summary>
    /// <returns>a new instance of the context generator</returns>
    public virtual ITokenContextGenerator ContextGenerator
    {
        get
        {
            Factory f = new Factory();
            ISet<string> abbs;
            NOpenNLP.Tools.Dictionary.Dictionary? abbDict = AbbreviationDictionary;
            if (abbDict != null)
            {
                abbs = abbDict.AsStringSet();
            }
            else
            {
                abbs = new JCG.HashSet<string>();
            }

            return f.CreateTokenContextGenerator(LanguageCode, abbs);
        }
    }
}
