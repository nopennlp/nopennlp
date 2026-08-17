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
using NOpenNLP.Tools.Sentdetect.Lang;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Sentdetect;

public class DefaultSDContextGeneratorTest
{
    [Test]
    public void TestGetContext()
    {
        ISDContextGenerator sdContextGenerator =
            new DefaultSDContextGenerator(new JCG.HashSet<string>(), Factory.defaultEosCharacters);

        var context = sdContextGenerator.GetContext(
            "Mr. Smith joined RONDHUIT Inc. as a manager of sales department.", 2);
        CollectionAssert.AreEqual("sn/eos=./x=Mr/2/xcap/v=/s=/n=Smith/ncap".Split('/'), context);

        context = sdContextGenerator.GetContext(
            "Mr. Smith joined RONDHUIT Inc. as a manager of sales department.", 29);
        CollectionAssert.AreEqual("sn/eos=./x=Inc/3/xcap/v=RONDHUIT/vcap/s=/n=as".Split('/'), context);
    }

    [Test]
    public void TestGetContextWithAbbreviations()
    {
        ISDContextGenerator sdContextGenerator =
            new DefaultSDContextGenerator(new JCG.HashSet<string>("Mr./Inc.".Split('/')),
                Factory.defaultEosCharacters);

        var context = sdContextGenerator.GetContext(
            "Mr. Smith joined RONDHUIT Inc. as a manager of sales department.", 2);
        CollectionAssert.AreEqual("sn/eos=./x=Mr/2/xcap/xabbrev/v=/s=/n=Smith/ncap".Split('/'), context);

        context = sdContextGenerator.GetContext(
            "Mr. Smith joined RONDHUIT Inc. as a manager of sales department.", 29);
        CollectionAssert.AreEqual("sn/eos=./x=Inc/3/xcap/xabbrev/v=RONDHUIT/vcap/s=/n=as".Split('/'), context);
    }
}
