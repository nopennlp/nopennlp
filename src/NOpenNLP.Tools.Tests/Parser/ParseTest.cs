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

using System.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Parser;

/// <summary>
/// Tests for the <see cref="Parse"/> class.
/// </summary>
public class ParseTest
{
    public const string PARSE_STRING = "(TOP  (S (S (NP-SBJ (PRP She)  )(VP (VBD was)  "
        + "(ADVP (RB just)  )(NP-PRD (NP (DT another)  (NN freighter)  )(PP (IN from)  (NP (DT the)  "
        + "(NNPS States)  )))))(, ,)  (CC and) (S (NP-SBJ (PRP she)  )(VP (VBD seemed)  "
        + "(ADJP-PRD (ADJP (RB as)  (JJ commonplace)  )(PP (IN as)  (NP (PRP$ her)  "
        + "(NN name)  )))))(. .)  ))";

    [Test]
    public void TestToHashCode()
    {
        Parse p1 = Parse.ParseParse(PARSE_STRING);
        p1.GetHashCode();
    }

    [Test]
    public void TestToString()
    {
        Parse p1 = Parse.ParseParse(PARSE_STRING);
        p1.ToString();
    }

    [Test]
    public void TestEquals()
    {
        Parse p1 = Parse.ParseParse(PARSE_STRING);
        ClassicAssert.IsTrue(p1.Equals(p1));
    }

    [Test]
    public void TestParseClone()
    {
        Parse p1 = Parse.ParseParse(PARSE_STRING);
        Parse p2 = (Parse)p1.Clone();
        ClassicAssert.IsTrue(p1.Equals(p2));
        ClassicAssert.IsTrue(p2.Equals(p1));
    }

    [Test]
    public void TestGetText()
    {
        Parse p = Parse.ParseParse(PARSE_STRING);

        // TODO: Why does parse attaches a space to the end of the text ???
        string expectedText = "She was just another freighter from the States , "
            + "and she seemed as commonplace as her name . ";

        ClassicAssert.AreEqual(expectedText, p.Text);
    }

    [Test]
    public void TestShow()
    {
        Parse p1 = Parse.ParseParse(PARSE_STRING);

        StringBuilder parseString = new();
        p1.Show(parseString);
        Parse p2 = Parse.ParseParse(parseString.ToString());
        ClassicAssert.AreEqual(p1, p2);
    }

    [Test]
    public void TestTokenReplacement()
    {
        Parse p1 = Parse.ParseParse("(TOP  (S-CLF (NP-SBJ (PRP It)  )(VP (VBD was) "
            + " (NP-PRD (NP (DT the)  (NN trial)  )(PP (IN of) "
            + " (NP (NP (NN oleomargarine)  (NN heir)  )(NP (NNP Minot) "
            + " (PRN (-LRB- -LRB-) (NNP Mickey) "
            + " (-RRB- -RRB-) )(NNP Jelke)  )))(PP (IN for) "
            + " (NP (JJ compulsory)  (NN prostitution) "
            + " ))(PP-LOC (IN in)  (NP (NNP New)  (NNP York) "
            + " )))(SBAR (WHNP-1 (WDT that)  )(S (VP (VBD put) "
            + " (NP (DT the)  (NN spotlight)  )(PP (IN on)  (NP (DT the) "
            + " (JJ international)  (NN play-girl)  ))))))(. .)  ))");

        StringBuilder parseString = new();
        p1.Show(parseString);

        Parse p2 = Parse.ParseParse(parseString.ToString());
        ClassicAssert.AreEqual(p1, p2);
    }

    [Test]
    public void TestGetTagNodes()
    {
        Parse p = Parse.ParseParse(PARSE_STRING);

        Parse[] tags = p.GetTagNodes();

        foreach (Parse node in tags)
        {
            ClassicAssert.IsTrue(node.IsPosTag);
        }

        ClassicAssert.AreEqual("PRP", tags[0].Type);
        ClassicAssert.AreEqual("VBD", tags[1].Type);
        ClassicAssert.AreEqual("RB", tags[2].Type);
        ClassicAssert.AreEqual("DT", tags[3].Type);
        ClassicAssert.AreEqual("NN", tags[4].Type);
        ClassicAssert.AreEqual("IN", tags[5].Type);
        ClassicAssert.AreEqual("DT", tags[6].Type);
        ClassicAssert.AreEqual("NNPS", tags[7].Type);
        ClassicAssert.AreEqual(",", tags[8].Type);
        ClassicAssert.AreEqual("CC", tags[9].Type);
        ClassicAssert.AreEqual("PRP", tags[10].Type);
        ClassicAssert.AreEqual("VBD", tags[11].Type);
        ClassicAssert.AreEqual("RB", tags[12].Type);
        ClassicAssert.AreEqual("JJ", tags[13].Type);
        ClassicAssert.AreEqual("IN", tags[14].Type);
        ClassicAssert.AreEqual("PRP$", tags[15].Type);
        ClassicAssert.AreEqual("NN", tags[16].Type);
        ClassicAssert.AreEqual(".", tags[17].Type);
    }
}
