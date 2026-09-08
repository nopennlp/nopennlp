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
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Ext;

public class ExtensionLoaderTest
{
    // define an interface here
    // NOpenNLP: interfaces are prefixed with I per .NET convention.
    internal interface ITestStringGenerator
    {
        string GenerateTestString();
    }

    internal class TestStringGeneratorImpl : ITestStringGenerator
    {
        public string GenerateTestString() => "test";
    }

    [TearDown]
    public void Reset()
    {
        // NOpenNLP: upstream also clears the system property that seeds the allowlist.
        // The port reads that setting once when the type is initialized, and nothing here
        // sets it, so there is nothing to clear.
        ExtensionLoader.UnregisterAllowedPackage("Acme");
        ExtensionLoader.UnregisterAllowedPackage("Example.Nlp");
        ExtensionLoader.UnregisterAllowedPackage("System");
    }

    // --- existing test ---

    [Test]
    public void TestLoadingStringGenerator()
    {
        // NOpenNLP: upstream passes the interface as a Class<T> argument, which becomes
        // the type parameter here. It names the implementation with
        // TestStringGeneratorImpl.class.getName(); the port's ResolveType goes through
        // Type.GetType, so the assembly-qualified name is what it can resolve for a type
        // outside the NOpenNLP.Tools assembly.
        var g = ExtensionLoader.InstantiateExtension<ITestStringGenerator>(
            typeof(TestStringGeneratorImpl).AssemblyQualifiedName!);
        ClassicAssert.AreEqual("test", g!.GenerateTestString());
    }

    // --- allowlist tests ---

    /// <summary>
    /// Types in the library's own namespace are allowed by default -- no registration needed.
    /// </summary>
    // NOpenNLP: upstream's default prefix is "opennlp."; the port also seeds "NOpenNLP.",
    // which is where its own types -- and this test's nested types -- live.
    [Test]
    public void TestBuiltinOpennlpPackageAllowedByDefault()
    {
        var g = ExtensionLoader.InstantiateExtension<ITestStringGenerator>(
            typeof(TestStringGeneratorImpl).AssemblyQualifiedName!);
        ClassicAssert.AreEqual("test", g!.GenerateTestString());
    }

    /// <summary>
    /// A type outside the allowed namespaces is rejected without registration.
    /// This is the core security invariant -- untrusted type names from model
    /// manifests must not reach type resolution without an explicit allowlist entry.
    /// </summary>
    [Test]
    public void TestUnregisteredPackageIsRejected()
    {
        var ex = Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>(
                "Example.Exploit.MaliciousFactory")));

        ClassicAssert.IsTrue(ex!.Message.Contains("not in an allowed package"),
            "exception message should mention allowed package");
    }

    /// <summary>
    /// The allowlist check runs before the type is resolved -- even for non-existent types
    /// the error must be "not in an allowed package", never "could not be located".
    /// </summary>
    [Test]
    public void TestAllowlistGateRunsBeforeClassForName()
    {
        var ex = Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>(
                "Example.DoesNotExistOnClasspath+Probe")));

        ClassicAssert.IsTrue(ex!.Message.Contains("not in an allowed package"),
            $"allowlist must reject before type resolution; got: {ex.Message}");
        ClassicAssert.IsFalse(ex.Message.Contains("could not be located"),
            $"type resolution must not have been reached; got: {ex.Message}");
    }

    /// <summary>
    /// After <see cref="ExtensionLoader.RegisterAllowedPackage"/>, a type from that namespace
    /// passes the allowlist gate. Uses <see cref="string"/> -- outside the default prefixes so
    /// registration is required. The call fails on assignability (a string is not an
    /// <see cref="ITestStringGenerator"/>), not on the allowlist check, proving the gate let
    /// it through.
    /// </summary>
    // NOpenNLP: upstream registers "java.lang" and names "java.lang.String"; the .NET
    // counterpart is "System" and "System.String".
    [Test]
    public void TestRegisteredPackageIsAllowed()
    {
        ExtensionLoader.RegisterAllowedPackage("System");

        var ex = Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>("System.String")));

        ClassicAssert.IsFalse(ex!.Message.Contains("not in an allowed package"),
            $"gate should have passed; got: {ex.Message}");
    }

    /// <summary>
    /// <see cref="ExtensionLoader.UnregisterAllowedPackage"/> removes a previously registered
    /// prefix. A type from that namespace is rejected after removal.
    /// </summary>
    [Test]
    public void TestUnregisterAllowedPackage()
    {
        ExtensionLoader.RegisterAllowedPackage("System");
        ExtensionLoader.UnregisterAllowedPackage("System");

        var ex = Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>("System.String")));

        ClassicAssert.IsTrue(ex!.Message.Contains("not in an allowed package"));
    }

    /// <summary>
    /// Prefix collision: registering "Acme" must not permit "AcmeEvil.*".
    /// </summary>
    [Test]
    public void TestPrefixCollisionPrevented()
    {
        ExtensionLoader.RegisterAllowedPackage("Acme");

        var ex = Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>("AcmeEvil.Exploit")));

        ClassicAssert.IsTrue(ex!.Message.Contains("not in an allowed package"));
    }

    /// <summary>
    /// <see cref="ExtensionLoader.RegisterAllowedPackage"/> throws for null and blank inputs.
    /// </summary>
    // NOpenNLP: upstream throws NullPointerException for null; the .NET counterpart is
    // ArgumentNullException.
    [Test]
    public void TestRegisterAllowedPackageRejectsNullAndBlank()
    {
        Assert.Throws<ArgumentNullException>((Action)(() => ExtensionLoader.RegisterAllowedPackage(null!)));

        Assert.Throws<ArgumentException>((Action)(() => ExtensionLoader.RegisterAllowedPackage("")));

        Assert.Throws<ArgumentException>((Action)(() => ExtensionLoader.RegisterAllowedPackage("   ")));
    }

    /// <summary>
    /// A null type name is rejected with <see cref="ExtensionNotLoadedException"/> before
    /// any allowlist or type-loading logic runs.
    /// </summary>
    [Test]
    public void TestNullClassNameIsRejected()
    {
        Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>(null!)));
    }

    // --- system property escape hatch tests ---

    /// <summary>
    /// The allowlist setting is read once when the type is initialized, so in-process tests
    /// cannot re-trigger that path. The equivalent is verified programmatically: a namespace
    /// outside the defaults registered via <see cref="ExtensionLoader.RegisterAllowedPackage"/>
    /// passes the gate. Uses <see cref="string"/> -- fails on assignability, not on the
    /// allowlist check.
    /// </summary>
    [Test]
    public void TestSystemPropertyAddsAllowedPackage()
    {
        ExtensionLoader.RegisterAllowedPackage("System");

        var ex = Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>("System.String")));

        ClassicAssert.IsFalse(ex!.Message.Contains("not in an allowed package"),
            $"registered package should pass the gate; got: {ex.Message}");
    }

    /// <summary>
    /// Multiple namespaces can be registered independently.
    /// </summary>
    [Test]
    public void TestSystemPropertyMultiplePackages()
    {
        ExtensionLoader.RegisterAllowedPackage("Example.Nlp");
        ExtensionLoader.RegisterAllowedPackage("System");

        var ex = Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>("System.String")));

        ClassicAssert.IsFalse(ex!.Message.Contains("not in an allowed package"),
            $"registered package should pass the gate; got: {ex.Message}");
    }

    /// <summary>
    /// System property prefix collision prevention -- the same dot-normalization applies.
    /// </summary>
    [Test]
    public void TestSystemPropertyPrefixCollisionPrevented()
    {
        ExtensionLoader.RegisterAllowedPackage("Acme");

        Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>("AcmeEvil.Exploit")));
    }

    // --- port-specific coverage ---

    /// <summary>
    /// A serialized OpenNLP model records its factories and serializers under their Java
    /// names, so the <c>opennlp.</c> prefix must remain allowed alongside <c>NOpenNLP.</c>.
    /// The call reaches type resolution and fails there on assignability, not at the gate.
    /// </summary>
    [Test]
    [NOpenNLPSpecific]
    public void TestJavaNamedBuiltinPassesTheGate()
    {
        var ex = Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>(
                "opennlp.tools.postag.POSTaggerFactory")));

        ClassicAssert.IsFalse(ex!.Message.Contains("not in an allowed package"),
            $"a Java-named built-in must pass the gate; got: {ex.Message}");
    }

    /// <summary>
    /// A .NET caller may name a type with its assembly-qualified name. The gate must test the
    /// namespace, which is the part before the first comma, rather than rejecting every such
    /// name or -- worse -- letting an assembly suffix smuggle a disallowed namespace through.
    /// </summary>
    [Test]
    [NOpenNLPSpecific]
    public void TestAssemblyQualifiedNameIsGatedOnItsNamespace()
    {
        var ex = Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>(
                "Example.Exploit.MaliciousFactory, NOpenNLP.Tools, Version=1.0.0.0")));

        ClassicAssert.IsTrue(ex!.Message.Contains("not in an allowed package"),
            $"the namespace, not the assembly name, decides; got: {ex.Message}");
    }

    /// <summary>
    /// The port's <c>ResolveType</c> falls back to matching on the simple type name across
    /// every loaded assembly. That fallback must not become a way around the gate: a
    /// disallowed name is rejected before any resolution runs, even when a type of that
    /// simple name does exist somewhere.
    /// </summary>
    [Test]
    [NOpenNLPSpecific]
    public void TestSimpleNameFallbackCannotBypassTheGate()
    {
        var ex = Assert.Throws<ExtensionNotLoadedException>((Action)(() =>
            ExtensionLoader.InstantiateExtension<ITestStringGenerator>(
                "Example.Exploit.TestStringGeneratorImpl")));

        ClassicAssert.IsTrue(ex!.Message.Contains("not in an allowed package"),
            $"the simple-name fallback must not bypass the gate; got: {ex.Message}");
    }
}
