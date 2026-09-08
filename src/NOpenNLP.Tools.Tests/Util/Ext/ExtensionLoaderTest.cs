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
}
