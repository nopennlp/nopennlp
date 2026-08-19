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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Model;

public class FileEventStreamTest
{
    private const string EVENTS =
        "other wc=ic w&c=he,ic n1wc=lc n1w&c=belongs,lc n2wc=lc\n" +
        "other wc=lc w&c=belongs,lc p1wc=ic p1w&c=he,ic n1wc=lc\n" +
        "other wc=lc w&c=to,lc p1wc=lc p1w&c=belongs,lc p2wc=ic\n" +
        "org-start wc=ic w&c=apache,ic p1wc=lc p1w&c=to,lc\n" +
        "org-cont wc=ic w&c=software,ic p1wc=ic p1w&c=apache,ic\n" +
        "org-cont wc=ic w&c=foundation,ic p1wc=ic p1w&c=software,ic\n" +
        "other wc=other w&c=.,other p1wc=ic\n";

    [Test]
    public void TestSimpleReading()
    {
        using FileEventStream feStream = new FileEventStream(new StringReader(EVENTS));
        ClassicAssert.AreEqual("other [wc=ic w&c=he,ic n1wc=lc n1w&c=belongs,lc n2wc=lc]",
            feStream.Read()!.ToString());
        ClassicAssert.AreEqual("other [wc=lc w&c=belongs,lc p1wc=ic p1w&c=he,ic n1wc=lc]",
            feStream.Read()!.ToString());
        ClassicAssert.AreEqual("other [wc=lc w&c=to,lc p1wc=lc p1w&c=belongs,lc p2wc=ic]",
            feStream.Read()!.ToString());
        ClassicAssert.AreEqual("org-start [wc=ic w&c=apache,ic p1wc=lc p1w&c=to,lc]",
            feStream.Read()!.ToString());
        ClassicAssert.AreEqual("org-cont [wc=ic w&c=software,ic p1wc=ic p1w&c=apache,ic]",
            feStream.Read()!.ToString());
        ClassicAssert.AreEqual("org-cont [wc=ic w&c=foundation,ic p1wc=ic p1w&c=software,ic]",
            feStream.Read()!.ToString());
        ClassicAssert.AreEqual("other [wc=other w&c=.,other p1wc=ic]",
            feStream.Read()!.ToString());
        ClassicAssert.IsNull(feStream.Read());
    }

    [Test]
    public void TestReset()
    {
        using FileEventStream feStream = new FileEventStream(new StringReader(EVENTS));
        // NOpenNLP: upstream's try/catch around reset() maps onto Assert.Throws.
        Assert.Throws<NotSupportedException>((Action)(() => feStream.Reset()));
    }
}
