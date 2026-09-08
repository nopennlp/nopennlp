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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;

namespace NOpenNLP.Tools.Util.Ext;

/// <summary>
/// The <see cref="ExtensionLoader"/> is responsible to load extensions to the OpenNLP library.
/// <para/>
/// Only types whose full name starts with a registered namespace prefix are permitted. The
/// default allowed prefixes are <c>NOpenNLP.</c> and <c>opennlp.</c>, which cover all built-in
/// factories and serializers, both under their ported names and under the Java names that
/// serialized OpenNLP models record.
/// <para/>
/// To allow custom extension types from other namespaces, either:
/// <list type="bullet">
///   <item><description>Call <see cref="RegisterAllowedPackage"/> programmatically before
///     loading any model that uses the custom type.</description></item>
///   <item><description>Set the <c>OPENNLP_EXT_ALLOWED_PACKAGES</c> setting to a
///     comma-separated list of namespace prefixes before the type is first used.</description></item>
/// </list>
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ExtensionLoader
{
    /// <summary>
    /// Setting for supplying additional allowed namespace prefixes.
    /// The value is a comma-separated list, e.g. <c>Acme.Nlp.,Other.</c>.
    /// <para/>
    /// This setting is read once when the type is initialized. If it cannot be supplied that
    /// early, call <see cref="RegisterAllowedPackage"/> before loading any model that uses a
    /// custom factory or serializer.
    /// </summary>
    // NOpenNLP: upstream reads this from a JVM system property, set with
    // -DOPENNLP_EXT_ALLOWED_PACKAGES=... . .NET has no such thing, so the value is read from
    // AppContext data (settable in runtimeconfig.json) and falls back to the environment
    // variable of the same name.
    public const string ALLOWED_PACKAGES_PROPERTY = "OPENNLP_EXT_ALLOWED_PACKAGES";

    /// <summary>
    /// Namespace prefixes whose types are permitted to be instantiated as extensions.
    /// Seeded from <c>NOpenNLP.</c> and <c>opennlp.</c> plus any prefixes in
    /// <see cref="ALLOWED_PACKAGES_PROPERTY"/>.
    /// </summary>
    // NOpenNLP: upstream seeds only "opennlp." because that is the package its own classes
    // live in. The port's own types are in NOpenNLP.*, and its models still record the Java
    // names, so both prefixes must be allowed for built-in factories and serializers to load.
    private static readonly ConcurrentDictionary<string, byte> AllowedPrefixes = InitAllowedPrefixes();

    private static bool isOsgiAvailable = false;

    private static ConcurrentDictionary<string, byte> InitAllowedPrefixes()
    {
        var prefixes = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        prefixes["NOpenNLP."] = 0;
        prefixes["opennlp."] = 0;

        string prop = AppContext.GetData(ALLOWED_PACKAGES_PROPERTY) as string
                      ?? Environment.GetEnvironmentVariable(ALLOWED_PACKAGES_PROPERTY)
                      ?? string.Empty;

        if (prop.Trim().Length > 0)
        {
            foreach (string prefix in prop.Split(','))
            {
                string trimmed = prefix.Trim();
                if (trimmed.Length > 0)
                {
                    prefixes[Normalize(trimmed)] = 0;
                }
            }
        }

        return prefixes;
    }

    private ExtensionLoader()
    {
    }

    private static string Normalize(string packagePrefix) =>
        packagePrefix.EndsWith(".", StringComparison.Ordinal) ? packagePrefix : packagePrefix + ".";

    /// <summary>
    /// Registers an additional namespace prefix whose types are permitted to be loaded as
    /// OpenNLP extensions. Call this once at application startup, before loading any model
    /// that uses a custom factory or serializer from that namespace.
    /// <para/>
    /// The prefix is normalized to end with <c>'.'</c> to prevent collision attacks
    /// (e.g. registering <c>"Acme"</c> cannot be exploited via <c>"AcmeEvil.*"</c>).
    /// </summary>
    /// <param name="packagePrefix">The namespace prefix to allow, e.g. <c>"Example.Nlp"</c>.
    ///     Must not be <c>null</c> or blank.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="packagePrefix"/>
    ///     is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="packagePrefix"/>
    ///     is blank.</exception>
    public static void RegisterAllowedPackage(string packagePrefix)
    {
        if (packagePrefix is null)
        {
            throw new ArgumentNullException(nameof(packagePrefix), "packagePrefix must not be null");
        }

        if (packagePrefix.Trim().Length == 0)
        {
            throw new ArgumentException("packagePrefix must not be blank", nameof(packagePrefix));
        }

        AllowedPrefixes[Normalize(packagePrefix)] = 0;
    }

    /// <summary>
    /// Removes a previously registered namespace prefix. Has no effect if the prefix was not
    /// registered. The default <c>NOpenNLP.</c> and <c>opennlp.</c> prefixes can also be
    /// removed, though this is not recommended.
    /// <para/>
    /// The prefix is normalized to end with <c>'.'</c> before removal, matching the
    /// normalization applied in <see cref="RegisterAllowedPackage"/>.
    /// </summary>
    /// <param name="packagePrefix">The namespace prefix to remove, e.g. <c>"Example.Nlp"</c>.
    ///     Must not be <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="packagePrefix"/>
    ///     is <c>null</c>.</exception>
    public static void UnregisterAllowedPackage(string packagePrefix)
    {
        if (packagePrefix is null)
        {
            throw new ArgumentNullException(nameof(packagePrefix), "packagePrefix must not be null");
        }

        AllowedPrefixes.TryRemove(Normalize(packagePrefix), out _);
    }

    /// <summary>
    /// NOpenNLP: whether <paramref name="extensionClassName"/> names a type in an allowed
    /// namespace.
    /// <para/>
    /// Upstream compares the raw class name against each prefix. A .NET caller may pass an
    /// assembly-qualified name (<c>"Ns.Type, Assembly, Version=..."</c>), whose namespace is
    /// still the leading segment, so only the part before the first comma is tested. Nothing
    /// is trimmed beyond that: a leading space or any other decoration would make the name
    /// unresolvable anyway.
    /// </summary>
    private static bool IsAllowed(string extensionClassName)
    {
        int comma = extensionClassName.IndexOf(',');
        string typeName = comma >= 0 ? extensionClassName[..comma] : extensionClassName;

        foreach (string prefix in AllowedPrefixes.Keys)
        {
            if (typeName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
        //
        // NOpenNLP: Java's Class.getName() separates a nested class from its outer
        // class with '$', where .NET reflection uses '+'. Models record their artifact
        // serializers by that name, and several of them are nested -- for example
        // "opennlp.tools.postag.POSTaggerFactory$POSDictionarySerializer" -- so without
        // this substitution a Java-trained model carrying a serializer-class- entry
        // fails to load.
        string[] parts = className.Replace('$', '+').Split('.');
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
        // NOpenNLP: for a nested type this must be the innermost name alone, since
        // that is what Type.Name reports -- "POSDictionarySerializer", not
        // "POSTaggerFactory+POSDictionarySerializer".
        string simpleName = parts[^1].Split('+')[^1];
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
    /// NOpenNLP: Renders a ported type under the Java class name that Apache
    /// OpenNLP writes for it, so a model serialized here can be loaded by Apache
    /// OpenNLP as well as by this port.
    /// <para/>
    /// This is the inverse of the translation <see cref="ResolveType"/> applies on
    /// the way in: the namespace segments are lowercased and a nested type's
    /// <c>'+'</c> separator becomes Java's <c>'$'</c>, turning
    /// <c>NOpenNLP.Tools.Sentdetect.SentenceDetectorFactory</c> back into
    /// <c>opennlp.tools.sentdetect.SentenceDetectorFactory</c>. Only the final
    /// segment, the type name itself, keeps its casing, which is what makes the
    /// round trip exact for every ported type: the port renames a Java package by
    /// title-casing it and nothing else.
    /// <para/>
    /// Only types defined in this assembly are translated, since those are exactly
    /// the ones with an upstream counterpart to name. A custom factory or
    /// serializer supplied by a caller is returned unchanged: it has no Java class
    /// to point at, and inventing one would be actively harmful, because the
    /// invented name no longer resolves to the caller's own type on the way back in
    /// and <see cref="ResolveType"/>'s simple-name fallback could bind it to an
    /// unrelated same-named type. Testing the assembly rather than the namespace
    /// matters: a caller's extension may perfectly well sit in a
    /// <c>NOpenNLP.</c>-prefixed namespace of its own.
    /// </summary>
    /// <param name="type">the type to name</param>
    /// <returns>the Java class name for a ported type, otherwise its .NET full name</returns>
    /// <exception cref="InvalidOperationException">if the type has no full name,
    ///     which is the case only for a generic parameter or similar construct that
    ///     could never be an extension.</exception>
    internal static string ToJavaClassName(Type type)
    {
        string fullName = type.FullName
            ?? throw new InvalidOperationException($"Type {type.Name} has no full name");

        if (type.Assembly != typeof(ExtensionLoader).Assembly
            || !fullName.StartsWith("NOpenNLP.", StringComparison.Ordinal))
        {
            return fullName;
        }

        string[] parts = fullName.Replace('+', '$').Split('.');

        // Every segment but the last is a namespace segment, which Java writes in
        // lower case. The last is the type name and keeps its casing; for a nested
        // type it is the whole "Outer$Inner" tail, whose parts are all type names.
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToLowerInvariant(parts[i][0]) + parts[i][1..];
            }
        }

        // "NOpenNLP" lowercases to "nOpenNLP" by the rule above; upstream's root
        // package is "opennlp".
        parts[0] = "opennlp";

        return string.Join(".", parts);
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
        if (extensionClassName is null)
        {
            throw new ExtensionNotLoadedException("extensionClassName must not be null");
        }

        // Validate BEFORE the type is resolved -- resolving a type runs its static
        // constructor (CWE-470), which must not happen for untrusted type names.
        if (!IsAllowed(extensionClassName))
        {
            throw new ExtensionNotLoadedException(
                $"Class '{extensionClassName}' is not in an allowed package. " +
                "Register the package via ExtensionLoader.RegisterAllowedPackage() or set " +
                $"the {ALLOWED_PACKAGES_PROPERTY} setting before first use.");
        }

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
