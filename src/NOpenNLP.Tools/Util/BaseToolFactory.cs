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
using NOpenNLP.Tools.Util.Ext;
using NOpenNLP.Tools.Util.Model;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Base class for all tool factories.
///
/// Extensions of this class should:
/// <list type="bullet">
///  <item><description>implement an empty constructor (TODO is it necessary?)</description></item>
///  <item><description>implement a constructor that takes the <see cref="IArtifactProvider"/> and
///      calls <c>BaseToolFactory(Map)</c></description></item>
///  <item><description>override <see cref="CreateArtifactMap()"/> and
///      <see cref="CreateArtifactSerializersMap()"/> methods if necessary.</description></item>
/// </list>
/// </summary>
public abstract class BaseToolFactory
{
    protected IArtifactProvider? artifactProvider;

    /// <summary>
    /// All sub-classes should have an empty constructor
    /// </summary>
    protected BaseToolFactory()
    {
    }

    /// <summary>
    /// Initializes the ToolFactory with an artifact provider.
    /// </summary>
    protected virtual void Init(IArtifactProvider artifactProvider)
    {
        this.artifactProvider = artifactProvider;
    }

    /// <summary>
    /// Creates an <see cref="IDictionary{TKey, TValue}"/> with pairs of keys and <see cref="IArtifactSerializer"/>.
    /// The models implementation should call this method from
    /// <c>BaseModel.CreateArtifactSerializersMap</c>
    /// <para/>
    /// The base implementation will return a <see cref="Dictionary{TKey, TValue}"/> that should be
    /// populated by sub-classes.
    /// </summary>
    public virtual IDictionary<string, IArtifactSerializer> CreateArtifactSerializersMap()
    {
        return new JCG.Dictionary<string, IArtifactSerializer>();
    }

    /// <summary>
    /// Creates an <see cref="IDictionary{TKey, TValue}"/> with pairs of keys and objects. The models
    /// implementation should call this constructor that creates a model
    /// programmatically.
    /// <para/>
    /// The base implementation will return a <see cref="Dictionary{TKey, TValue}"/> that should be
    /// populated by sub-classes.
    /// </summary>
    public virtual IDictionary<string, object> CreateArtifactMap()
    {
        return new JCG.Dictionary<string, object>();
    }

    /// <summary>
    /// Creates the manifest entries that will be added to the model manifest
    /// </summary>
    /// <returns>the manifest entries to added to the model manifest</returns>
    public virtual IDictionary<string, string> CreateManifestEntries()
    {
        return new JCG.Dictionary<string, string>();
    }

    /// <summary>
    /// Validates the parsed artifacts. If something is not
    /// valid subclasses should throw an <see cref="InvalidFormatException"/>.
    ///
    /// Note:
    /// Subclasses should generally invoke super.validateArtifactMap at the beginning
    /// of this method.
    /// </summary>
    /// <exception cref="InvalidFormatException"></exception>
    public abstract void ValidateArtifactMap();

    public static BaseToolFactory? Create(string subclassName, IArtifactProvider artifactProvider)
    {
        BaseToolFactory? theFactory;
        try
        {

            // load the ToolFactory using the default constructor
            theFactory = ExtensionLoader.InstantiateExtension<BaseToolFactory>(subclassName);
            if (theFactory != null)
            {
                theFactory.Init(artifactProvider);
            }
        }
        catch (Exception e)
        {
            string msg = "Could not instantiate the " + subclassName + ". The initialization throw an exception.";
            throw new InvalidFormatException(msg, e);
        }

        return theFactory;
    }

    public static BaseToolFactory? Create(Type? factoryClass, IArtifactProvider artifactProvider)
    {
        BaseToolFactory? theFactory = null;
        if (factoryClass != null)
        {
            try
            {
                theFactory = (BaseToolFactory)Activator.CreateInstance(factoryClass);
                theFactory.Init(artifactProvider);
            }
            catch (Exception e)
            {
                string msg = "Could not instantiate the " + factoryClass.FullName + ". The initialization throw an exception.";
                throw new InvalidFormatException(msg, e);
            }
        }

        return theFactory;
    }
}
