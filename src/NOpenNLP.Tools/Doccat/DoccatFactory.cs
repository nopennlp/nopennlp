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

using System;
using System.Collections.Generic;
using System.Text;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// The factory that provides Doccat default implementations and resources
/// </summary>
public class DoccatFactory : BaseToolFactory
{
    private const string FEATURE_GENERATORS = "doccat.featureGenerators";

    private IFeatureGenerator[]? featureGenerators;

    /// <summary>
    /// Creates a <see cref="DoccatFactory"/> that provides the default implementation of
    /// the resources.
    /// </summary>
    public DoccatFactory()
    {
    }

    public DoccatFactory(IFeatureGenerator[] featureGenerators)
    {
        this.featureGenerators = featureGenerators;
    }

    protected virtual void Init(IFeatureGenerator[] featureGenerators)
    {
        this.featureGenerators = featureGenerators;
    }

    public override IDictionary<string, string> CreateManifestEntries()
    {
        IDictionary<string, string> manifestEntries = base.CreateManifestEntries();

        if (FeatureGenerators != null)
        {
            manifestEntries[FEATURE_GENERATORS] = FeatureGeneratorsAsString();
        }

        return manifestEntries;
    }

    private string FeatureGeneratorsAsString()
    {
        IFeatureGenerator[] fgs = FeatureGenerators;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < fgs.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(fgs[i].GetType().FullName);
        }

        return sb.ToString();
    }

    public override void ValidateArtifactMap()
    {
        // nothing to validate
    }

    public static DoccatFactory Create(string? subclassName, IFeatureGenerator[] featureGenerators)
    {
        if (subclassName == null)
        {
            // will create the default factory
            return new DoccatFactory(featureGenerators);
        }

        try
        {
            DoccatFactory theFactory = ExtensionLoader.InstantiateExtension<DoccatFactory>(subclassName)!;
            theFactory.Init(featureGenerators);
            return theFactory;
        }
        catch (Exception e)
        {
            string msg = $"Could not instantiate the {subclassName}. The initialization throw an exception.";
            throw new InvalidFormatException(msg, e);
        }
    }

    private static IFeatureGenerator[] LoadFeatureGenerators(string classNames)
    {
        string[] classes = classNames.Split(',');
        IFeatureGenerator[] fgs = new IFeatureGenerator[classes.Length];

        for (int i = 0; i < classes.Length; i++)
        {
            fgs[i] = ExtensionLoader.InstantiateExtension<IFeatureGenerator>(classes[i])!;
        }

        return fgs;
    }

    public virtual IFeatureGenerator[] FeatureGenerators
    {
        get
        {
            if (featureGenerators == null)
            {
                string? classNames = artifactProvider?.GetManifestProperty(FEATURE_GENERATORS);
                if (classNames != null)
                {
                    this.featureGenerators = LoadFeatureGenerators(classNames);
                }

                if (featureGenerators == null)
                {
                    // could not load using artifact provider, load bag of words as default
                    this.featureGenerators = [new BagOfWordsFeatureGenerator()];
                }
            }

            return featureGenerators;
        }
        set => featureGenerators = value;
    }
}
