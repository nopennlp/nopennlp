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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats;

public class NameFinderCensus90NameStreamTest
{
    /// <exception cref="IOException">if the stream cannot be created</exception>
    private static IObjectStream<StringList?> OpenData(string name)
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/formats/" + name);

        return new NameFinderCensus90NameStream(@in, Encoding.UTF8);
    }

    [Test]
    public void TestParsingEnglishSample()
    {
        IObjectStream<StringList?> sampleStream = OpenData("census90.sample");

        StringList? personName = sampleStream.Read();

        // verify the first 5 taken from the Surname data
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Smith", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Johnson", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Williams", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Jones", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Brown", personName!.GetToken(0));

        // verify the next 5 taken from the female names
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Mary", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Patricia", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Linda", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Barbara", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Elizabeth", personName!.GetToken(0));

        // verify the last 5 taken from the male names
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("James", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("John", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Robert", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("Michael", personName!.GetToken(0));
        personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);
        ClassicAssert.AreEqual("William", personName!.GetToken(0));

        // verify the end of the file.
        personName = sampleStream.Read();
        ClassicAssert.Null(personName);
    }
}
