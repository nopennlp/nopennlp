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

using System.Linq;
using System.Text;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Namefind;

public class NameSampleTypeFilterTest
{
    private static NameSampleTypeFilter filter = null!;

    private const string text = "<START:organization> NATO <END> Secretary - General " +
        "<START:person> Anders Fogh Rasmussen <END> made clear that despite an intensifying " +
        "insurgency and uncertainty over whether <START:location> U . S . <END> President " +
        "<START:person> Barack Obama <END> will send more troops , <START:location> NATO <END> " +
        "will remain in <START:location> Afghanistan <END> .";

    private const string person = "person";
    private const string organization = "organization";

    [Test]
    public void TestNoFilter()
    {
        string[] types = [];

        filter = new NameSampleTypeFilter(types, SampleStream(text));

        NameSample? ns = filter.Read();

        ClassicAssert.AreEqual(0, ns!.Names.Length);
    }

    [Test]
    public void TestSingleFilter()
    {
        string[] types = [organization];

        filter = new NameSampleTypeFilter(types, SampleStream(text));

        NameSample? ns = filter.Read();

        ClassicAssert.AreEqual(1, ns!.Names.Length);
        ClassicAssert.AreEqual(organization, ns.Names[0].Type);
    }

    [Test]
    public void TestMultiFilter()
    {
        string[] types = [person, organization];

        filter = new NameSampleTypeFilter(types, SampleStream(text));

        NameSample? ns = filter.Read();

        var collect = ns!.Names
            .GroupBy(s => s.Type!)
            .ToDictionary(g => g.Key, g => g.ToList());
        ClassicAssert.AreEqual(2, collect.Count);
        ClassicAssert.AreEqual(2, collect[person].Count);
        ClassicAssert.AreEqual(1, collect[organization].Count);
    }

    private static IObjectStream<NameSample?> SampleStream(string sampleText)
    {
        // NOpenNLP: upstream supplies the InputStreamFactory as a lambda; the
        // ported MockInputStreamFactory takes the string and charset directly.
        IInputStreamFactory @in = new MockInputStreamFactory(sampleText, Encoding.UTF8);

        return new NameSampleDataStream(
            new PlainTextByLineStream(@in, Encoding.UTF8));
    }
}
