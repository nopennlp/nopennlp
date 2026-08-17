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

using System.IO;
using NOpenNLP.Tools.Support;

namespace NOpenNLP.Tools.Entitylinker;

/// <summary>
/// Properties wrapper for the EntityLinker framework.
/// </summary>
public class EntityLinkerProperties
{
    // NOpenNLP: made readonly
    private readonly Properties props;

    /// <summary>
    /// Initializes a new instance from the given properties file.
    /// </summary>
    /// <param name="propertiesFile">The path of the properties file.</param>
    /// <exception cref="IOException">Thrown if the file cannot be read.</exception>
    public EntityLinkerProperties(string propertiesFile)
    {
        using Stream stream = new FileStream(propertiesFile, FileMode.Open, FileAccess.Read);
        props = Load(stream);
    }

    /// <summary>
    /// Initializes a new instance from the given stream.
    /// </summary>
    /// <param name="propertiesIn">The stream of the properties file. The stream will not be closed.</param>
    /// <exception cref="IOException">Thrown if the stream cannot be read.</exception>
    public EntityLinkerProperties(Stream propertiesIn)
    {
        props = Load(propertiesIn);
    }

    private static Properties Load(Stream propertiesIn)
    {
        var properties = new Properties();
        properties.Load(propertiesIn);
        return properties;
    }

    /// <summary>
    /// Gets a property from the props file.
    /// </summary>
    /// <param name="key">The key to the desired item in the properties file (key=value).</param>
    /// <param name="defaultValue">A default value in case the key, or the value are missing.</param>
    /// <returns>A property value in the form of a string.</returns>
    /// <remarks>
    /// NOpenNLP: Java also declares an IOException for an uninitialized properties object.
    /// Both constructors here assign the field, so that state is unreachable and the check is
    /// omitted rather than left as dead code.
    /// </remarks>
    public string? GetProperty(string key, string? defaultValue) =>
        props.GetProperty(key, defaultValue);
}
