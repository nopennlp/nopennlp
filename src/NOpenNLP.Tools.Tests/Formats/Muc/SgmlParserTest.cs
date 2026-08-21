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
using System.Text;
using NOpenNLP.Tools.Support;
using NUnit.Framework;

namespace NOpenNLP.Tools.Formats.Muc;

public class SgmlParserTest
{
    [Test]
    public void TestParse1()
    {
        using var resource = TestResources.OpenResource("/opennlp/tools/formats/muc/parsertest1.sgml");
        using TextReader @in = new StreamReader(resource, Encoding.UTF8);

        var parser = new SgmlParser();
        parser.Parse(@in, new TestParse1ContentHandlerAnonymousClass());
    }

    // NOpenNLP: upstream passes an anonymous subclass of the abstract SgmlParser.ContentHandler
    // that overrides nothing; C# has no anonymous classes, so this stands in for it.
    private sealed class TestParse1ContentHandlerAnonymousClass : SgmlParser.ContentHandler
    {
    }
}
