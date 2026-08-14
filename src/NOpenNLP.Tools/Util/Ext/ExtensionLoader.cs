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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using NOpenNLP.Tools.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;

namespace NOpenNLP.Tools.Util.Ext;

/// <summary>
/// The <see cref="ExtensionLoader"/> is responsible to load extensions to the OpenNLP library.
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ExtensionLoader
{
    private static bool isOsgiAvailable = false;

    private ExtensionLoader()
    {
    }

    /// <summary>
    /// NOpenNLP: Resolves a type name that may be a Java class name.
    /// <para/>
    /// Serialized OpenNLP models record their tool factory as a Java class name
    /// (for example <c>opennlp.tools.sentdetect.SentenceDetectorFactory</c>).
    /// <see cref="Type.GetType(string)"/> cannot resolve those, so the Java name is
    /// translated to the corresponding ported type by title-casing each segment
    /// and searching the loaded assemblies.
    /// <para/>
    /// Upstream uses <c>Class.forName</c>, which searches the whole classpath, so a
    /// user-supplied factory in another jar resolves. Searching only this assembly
    /// would break that extension point, so the loaded assemblies are searched
    /// too, with this one taking precedence.
    /// </summary>
    internal static Type? ResolveType(string? className)
    {
        if (className is null)
        {
            return null;
        }

        Type? type = Type.GetType(className);
        if (type != null)
        {
            return type;
        }

        // Translate a Java package/class name to the ported namespace, e.g.
        // "opennlp.tools.sentdetect.SentenceDetectorFactory" ->
        // "NOpenNLP.Tools.Sentdetect.SentenceDetectorFactory".
        string[] parts = className.Split('.');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0 && char.IsLower(parts[i][0]))
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
            }
        }

        string portedName = string.Join(".", parts);

        // This assembly first, so a ported type always wins over a same-named type
        // that happens to be loaded elsewhere.
        foreach (Assembly assembly in GetSearchAssemblies())
        {
            type = assembly.GetType(portedName, throwOnError: false);
            if (type != null)
            {
                return type;
            }
        }

        // Fall back to matching on the simple type name, which covers cases where
        // the Java package does not line up with the ported namespace.
        string simpleName = parts[^1];
        foreach (Assembly assembly in GetSearchAssemblies())
        {
            Type[] candidates;
            try
            {
                candidates = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // A partially loadable assembly still contributes the types that
                // did load; an unrelated assembly failing to load must not stop
                // the search.
                candidates = [.. e.Types.Where(t => t != null)!];
            }

            foreach (Type candidate in candidates)
            {
                if (string.Equals(candidate.Name, simpleName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// NOpenNLP: this assembly first, then the rest of the loaded assemblies, so a
    /// factory supplied by the calling application is reachable the way it would be
    /// on the Java classpath. Framework assemblies are skipped: they cannot define
    /// an OpenNLP extension and enumerating their types is expensive.
    /// </summary>
    private static IEnumerable<Assembly> GetSearchAssemblies()
    {
        Assembly self = typeof(ExtensionLoader).Assembly;
        yield return self;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (ReferenceEquals(assembly, self) || assembly.IsDynamic)
            {
                continue;
            }

            string? name = assembly.GetName().Name;
            if (name is null
                || name.StartsWith("System.", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.", StringComparison.Ordinal)
                || string.Equals(name, "mscorlib", StringComparison.Ordinal)
                || string.Equals(name, "netstandard", StringComparison.Ordinal))
            {
                continue;
            }

            yield return assembly;
        }
    }

    internal static bool IsOSGiAvailable
    {
        get => isOsgiAvailable;
        set => isOsgiAvailable = value;
    }

    // Pass in the type (interface) of the class to load
    /// <summary>
    /// Instantiates an user provided extension to OpenNLP.
    /// <para/>
    /// The extension is either loaded from the class path or if running
    /// inside an OSGi environment via an OSGi service.
    /// <para/>
    /// Initially it tries using the public default
    /// constructor. If it is not found, it will check if the class follows the singleton
    /// pattern: a static field named <c>INSTANCE</c> that returns an object of the type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <param name="extensionClassName"></param>
    /// <returns>the instance of the extension class</returns>
    // TODO: Throw custom exception if loading fails ...
    public static T? InstantiateExtension<T>(string extensionClassName)
    {

        // First try to load extension and instantiate extension from class path
        try
        {
            var extClazz = ResolveType(extensionClassName);
            if (extClazz != null && typeof(T).IsAssignableFrom(extClazz))
            {
                try
                {
                    return (T)Activator.CreateInstance(extClazz);
                }
                catch (TargetInvocationException e)
                {
                    throw new ExtensionNotLoadedException(e);
                }
                catch (MethodAccessException e)
                {
                    // constructor is private. Try to load using INSTANCE
                    FieldInfo? instanceField;
                    try
                    {
                        instanceField = extClazz.GetField("INSTANCE", BindingFlags.DeclaredOnly);
                    }
                    // catch (NoSuchFieldException e1)
                    // {
                    //     throw new ExtensionNotLoadedException(e1);
                    // }
                    catch (SecurityException e1)
                    {
                        throw new ExtensionNotLoadedException(e1);
                    }

                    if (instanceField != null)
                    {
                        try
                        {
                            return (T)instanceField.GetValue(null);
                        }
                        catch (ArgumentException e1)
                        {
                            throw new ExtensionNotLoadedException(e1);
                        }
                        catch (FieldAccessException e1)
                        {
                            throw new ExtensionNotLoadedException(e1);
                        }
                    }

                    throw new ExtensionNotLoadedException(e);
                }
            }
            else
            {
                throw new ExtensionNotLoadedException($"Extension class '{extClazz?.Name ?? "null"}' needs to have type: {typeof(T).Name}");
            }
        }
        catch (ClassNotFoundException)
        {
        }

        // Loading from class path failed
        // Either something is wrong with the class name or OpenNLP is
        // running in an OSGi environment. The extension classes are not
        // on our classpath in this case.
        // In OSGi we need to use services to get access to extensions.
        // Determine if OSGi class is on class path
        // Now load class which depends on OSGi API
        if (isOsgiAvailable)
        {
            // The OSGIExtensionLoader class will be loaded when the next line
            // is executed, but not prior, and that is why it is safe to directly
            // reference it here.

            // NOpenNLP TODO: determine if this is needed via MEF or something...
            // OSGiExtensionLoader extLoader = OSGiExtensionLoader.GetInstance();
            // return extLoader.GetExtension(clazz, extensionClassName);
        }

        throw new ExtensionNotLoadedException($"Unable to find implementation for {typeof(T).Name}, the class or service {extensionClassName} could not be located!");
    }
}
