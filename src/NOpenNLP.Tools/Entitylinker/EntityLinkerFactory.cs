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
using System.IO;
using NOpenNLP.Tools.Util.Ext;

namespace NOpenNLP.Tools.Entitylinker;

/// <summary>
/// Generates an <see cref="IEntityLinker"/> implementation via properties file configuration.
/// </summary>
public static class EntityLinkerFactory
{
    // NOpenNLP: stands in for Java's `static synchronized`, which locks on the class object.
    private static readonly object syncLock = new();

    /// <summary>
    /// Gets an <see cref="IEntityLinker"/> implementation for the given entity type.
    /// </summary>
    /// <param name="entityType">
    /// The type of entity being linked to. This value is used to retrieve the implementation of
    /// the entitylinker from the entitylinker properties file.
    /// </param>
    /// <param name="properties">
    /// An object that extends <see cref="EntityLinkerProperties"/>. This object will be passed
    /// into the implemented <see cref="IEntityLinker.Init"/> method, so it is an appropriate
    /// place to put additional resources.
    /// </param>
    /// <returns>An <see cref="IEntityLinker"/> impl.</returns>
    /// <exception cref="IOException">Thrown if the linker cannot be initialized.</exception>
    public static IEntityLinker GetLinker(string entityType, EntityLinkerProperties properties)
    {
        lock (syncLock)
        {
            if (entityType == null || properties == null)
            {
                throw new ArgumentException("Null argument in entityLinkerFactory");
            }

            string? linkerImplFullName = properties.GetProperty("linker." + entityType, "");

            if (string.IsNullOrEmpty(linkerImplFullName))
            {
                throw new ArgumentException("linker." + entityType + "  property must be set!");
            }

            return InstantiateAndInit(linkerImplFullName!, properties);
        }
    }

    /// <summary>
    /// Gets an <see cref="IEntityLinker"/> implementation named by the <c>linker</c> property.
    /// </summary>
    /// <param name="properties">
    /// An object that extends <see cref="EntityLinkerProperties"/>. This object will be passed
    /// into the implemented <see cref="IEntityLinker.Init"/> method, so it is an appropriate
    /// place to put additional resources. In the properties file, the linker implementation must
    /// be provided using "linker" as the properties key, and the full class name as value.
    /// </param>
    /// <returns>An <see cref="IEntityLinker"/> impl.</returns>
    /// <exception cref="IOException">Thrown if the linker cannot be initialized.</exception>
    public static IEntityLinker GetLinker(EntityLinkerProperties properties)
    {
        lock (syncLock)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties), "properties argument must not be null");
            }

            string? linkerImplFullName = properties.GetProperty("linker", "");

            if (string.IsNullOrEmpty(linkerImplFullName))
            {
                throw new ArgumentException("\"linker\" property must be set!");
            }

            return InstantiateAndInit(linkerImplFullName!, properties);
        }
    }

    private static IEntityLinker InstantiateAndInit(string linkerImplFullName, EntityLinkerProperties properties)
    {
        IEntityLinker? linker = ExtensionLoader.InstantiateExtension<IEntityLinker>(linkerImplFullName);

        // NOpenNLP: InstantiateExtension returns null when the type cannot be resolved, where Java
        // throws from the loader. Surface it rather than dereferencing null.
        if (linker is null)
        {
            throw new InvalidOperationException(
                "Could not instantiate the " + linkerImplFullName + " entity linker!");
        }

        linker.Init(properties);
        return linker;
    }
}
