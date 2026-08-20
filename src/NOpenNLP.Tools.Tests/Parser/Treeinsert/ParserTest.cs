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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using HeadRules = NOpenNLP.Tools.Parser.Lang.En.HeadRules;

namespace NOpenNLP.Tools.Parser.Treeinsert;

/// <summary>
/// Tests for the <see cref="Parser"/> class.
/// </summary>
public class ParserTest
{
    /// <summary>
    /// Verify that training and tagging does not cause
    /// runtime problems.
    /// </summary>
    [Test]
    public void TestTreeInsertParserTraining()
    {
        IObjectStream<Parse?> parseSamples = ParserTestUtil.OpenTestTrainingData();
        HeadRules headRules = ParserTestUtil.CreateTestHeadRules();

        ParserModel model = Parser.Train("eng", parseSamples, headRules, 100, 0);

        IParser parser = ParserFactory.Create(model);

        // Tests parsing to make sure the code does not has
        // a bug which fails always with a runtime exception
        parser.Parse(Parse.ParseParse("She was just another freighter from the " +
            "States and she seemed as commonplace as her name ."));

        // Test serializing and de-serializing model
        MemoryStream outArray = new();
        model.Serialize(outArray);
        outArray.Close();

        _ = new ParserModel(new MemoryStream(outArray.ToArray()));

        // TODO: compare both models
    }
}
