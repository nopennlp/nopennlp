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
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util;

// NOpenNLP: upstream covers the deprecated string-map constructor and getSettings
// overloads, so the ported test calls them on purpose. The obsoletion warnings are
// suppressed here rather than dropping the coverage upstream has.
#pragma warning disable CS0618 // Type or member is obsolete
public class TrainingParametersTest
{
    [Test]
    public void TestConstructors()
    {
        TrainingParameters tp1 =
            new(BuildMap("key1=val1,key2=val2,key3=val3"));

        TrainingParameters tp2 = new(
            new MemoryStream(Encoding.UTF8.GetBytes("key1=val1\nkey2=val2\nkey3=val3\n"))
        );

        TrainingParameters tp3 = new(tp2);

        AssertEquals(tp1, tp2);
        AssertEquals(tp2, tp3);
    }

    [Test]
    public void TestDefault()
    {
        TrainingParameters tr = TrainingParameters.DefaultParams();

        ClassicAssert.AreEqual(4, tr.GetSettings().Count);
        ClassicAssert.AreEqual("MAXENT", tr.Algorithm());
        // NOpenNLP: upstream asserts against EventTrainer.EVENT_VALUE, which is part
        // of the trainer API and is not ported yet; the literal is inlined here to
        // match the value DefaultParams() puts in place.
        ClassicAssert.AreEqual("Event",
            tr.GetStringParameter(TrainingParameters.TRAINER_TYPE_PARAM,
                "v11"));  // use different defaults
        ClassicAssert.AreEqual(100,
            tr.GetIntParameter(TrainingParameters.ITERATIONS_PARAM,
                200));  // use different defaults
        ClassicAssert.AreEqual(5,
            tr.GetIntParameter(TrainingParameters.CUTOFF_PARAM,
                200));  // use different defaults
    }

    [Test]
    public void TestGetAlgorithm()
    {
        TrainingParameters tp = Build("Algorithm=Perceptron,n1.Algorithm=SVM");

        ClassicAssert.AreEqual("Perceptron", tp.Algorithm());
        ClassicAssert.AreEqual("SVM", tp.Algorithm("n1"));
    }

    [Test]
    public void TestGetSettings()
    {
        TrainingParameters tp = Build("k1=v1,n1.k2=v2,n2.k3=v3,n1.k4=v4");

        AssertEquals(BuildMap("k1=v1"), tp.GetSettings());
        AssertEquals(BuildMap("k2=v2,k4=v4"), tp.GetSettings("n1"));
        AssertEquals(BuildMap("k3=v3"), tp.GetSettings("n2"));
        ClassicAssert.IsTrue(tp.GetSettings("n3").Count == 0);
    }

    [Test]
    public void TestGetParameters()
    {
        TrainingParameters tp = Build("k1=v1,n1.k2=v2,n2.k3=v3,n1.k4=v4");

        AssertEquals(Build("k1=v1"), tp.GetParameters(null));
        AssertEquals(Build("k2=v2,k4=v4"), tp.GetParameters("n1"));
        AssertEquals(Build("k3=v3"), tp.GetParameters("n2"));
        ClassicAssert.IsTrue(tp.GetParameters("n3").GetSettings().Count == 0);
    }

    [Test]
    public void TestPutGet()
    {
        TrainingParameters tp =
            Build("k1=v1,int.k2=123,str.k2=v3,str.k3=v4,boolean.k4=false,double.k5=123.45,k21=234.5");

        ClassicAssert.AreEqual("v1", tp.GetStringParameter("k1", "def"));
        ClassicAssert.AreEqual("def", tp.GetStringParameter("k2", "def"));
        ClassicAssert.AreEqual("v3", tp.GetStringParameter("str", "k2", "def"));
        ClassicAssert.AreEqual("def", tp.GetStringParameter("str", "k4", "def"));

        ClassicAssert.AreEqual(-100, tp.GetIntParameter("k11", -100));
        tp.Put("k11", 234);
        ClassicAssert.AreEqual(234, tp.GetIntParameter("k11", -100));
        ClassicAssert.AreEqual(123, tp.GetIntParameter("int", "k2", -100));
        ClassicAssert.AreEqual(-100, tp.GetIntParameter("int", "k4", -100));

        ClassicAssert.AreEqual(234.5, tp.GetDoubleParameter("k21", -100), 0.001);
        tp.Put("k21", 345.6);
        ClassicAssert.AreEqual(345.6, tp.GetDoubleParameter("k21", -100), 0.001); // should be changed
        tp.PutIfAbsent("k21", 456.7);
        ClassicAssert.AreEqual(345.6, tp.GetDoubleParameter("k21", -100), 0.001); // should be unchanged
        ClassicAssert.AreEqual(123.45, tp.GetDoubleParameter("double", "k5", -100), 0.001);

        ClassicAssert.AreEqual(true, tp.GetBooleanParameter("k31", true));
        tp.Put("k31", false);
        ClassicAssert.AreEqual(false, tp.GetBooleanParameter("k31", true));
        ClassicAssert.AreEqual(false, tp.GetBooleanParameter("boolean", "k4", true));
    }

    // format: k1=v1,k2=v2,...
    private static IDictionary<string, string> BuildMap(string str)
    {
        string[] pairs = str.Split(',');
        Dictionary<string, string> map = new(pairs.Length);
        foreach (string pair in pairs)
        {
            string[] keyValue = pair.Split('=');
            map[keyValue[0]] = keyValue[1];
        }

        return map;
    }

    // format: k1=v1,k2=v2,...
    private static TrainingParameters Build(string str) => new(BuildMap(str));

    private static void AssertEquals(IDictionary<string, string> map1, IDictionary<string, string> map2)
    {
        ClassicAssert.IsNotNull(map1);
        ClassicAssert.IsNotNull(map2);
        ClassicAssert.AreEqual(map1.Count, map2.Count);
        foreach (string key in map1.Keys)
        {
            ClassicAssert.AreEqual(map1[key], map2.TryGetValue(key, out string? value) ? value : null);
        }
    }

    private static void AssertEquals(IDictionary<string, string> map, TrainingParameters actual)
    {
        ClassicAssert.IsNotNull(actual);
        AssertEquals(map, actual.GetSettings());
    }

    private static void AssertEquals(TrainingParameters expected, TrainingParameters actual)
    {
        if (expected == null)
        {
            ClassicAssert.IsNull(actual);
        }
        else
        {
            AssertEquals(expected.GetSettings(), actual);
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
