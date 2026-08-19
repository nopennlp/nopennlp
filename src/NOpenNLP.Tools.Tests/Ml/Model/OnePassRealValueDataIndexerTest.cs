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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Model;

public class OnePassRealValueDataIndexerTest
{
    private IDataIndexer indexer;

    [SetUp]
    public void SetUp()
    {
        indexer = new OnePassRealValueDataIndexer();
        indexer.Init(new TrainingParameters(new Dictionary<string, string>()), null);
    }

    [Test]
    public void TestIndex()
    {
        // He belongs to <START:org> Apache Software Foundation <END> .
        IObjectStream<Event?> eventStream = new SimpleEventStreamBuilder()
            .Add("other/w=he n1w=belongs n2w=to po=other pow=other,He powf=other,ic ppo=other")
            .Add("other/w=belongs p1w=he n1w=to n2w=apache po=other pow=other,belongs powf=other,lc ppo=other")
            .Add("other/w=to p1w=belongs p2w=he n1w=apache n2w=software po=other pow=other,to" +
                " powf=other,lc ppo=other")
            .Add("org-start/w=apache p1w=to p2w=belongs n1w=software n2w=foundation po=other pow=other,Apache" +
                " powf=other,ic ppo=other")
            .Add("org-cont/w=software p1w=apache p2w=to n1w=foundation n2w=. po=org-start" +
                " pow=org-start,Software powf=org-start,ic ppo=other")
            .Add("org-cont/w=foundation p1w=software p2w=apache n1w=. po=org-cont pow=org-cont,Foundation" +
                " powf=org-cont,ic ppo=org-start")
            .Add("other/w=. p1w=foundation p2w=software po=org-cont pow=org-cont,. powf=org-cont,other" +
                " ppo=org-cont")
            .Build();

        indexer.Index(eventStream);
        ClassicAssert.AreEqual(3, indexer.Contexts.Length);
        CollectionAssert.AreEqual(new int[] { 0 }, indexer.Contexts[0]);
        CollectionAssert.AreEqual(new int[] { 0 }, indexer.Contexts[1]);
        CollectionAssert.AreEqual(new int[] { 0 }, indexer.Contexts[2]);
        ClassicAssert.AreEqual(3, indexer.Values!.Length);
        ClassicAssert.IsNull(indexer.Values[0]);
        ClassicAssert.IsNull(indexer.Values[1]);
        ClassicAssert.IsNull(indexer.Values[2]);
        ClassicAssert.AreEqual(5, indexer.NumEvents);
        CollectionAssert.AreEqual(new int[] { 0, 1, 2 }, indexer.OutcomeList);
        CollectionAssert.AreEqual(new int[] { 3, 1, 1 }, indexer.NumTimesEventsSeen);
        CollectionAssert.AreEqual(new string[] { "ppo=other" }, indexer.PredLabels);
        CollectionAssert.AreEqual(new string[] { "other", "org-start", "org-cont" }, indexer.OutcomeLabels);
        CollectionAssert.AreEqual(new int[] { 5 }, indexer.PredCounts);
    }

    [Test]
    public void TestIndexValues()
    {
        // He belongs to <START:org> Apache Software Foundation <END> .
        IObjectStream<Event?> eventStream = new SimpleEventStreamBuilder()
            .Add("other/w=he;0.1 n1w=belongs;0.2 n2w=to;0.1 po=other;0.1" +
                " pow=other,He;0.1 powf=other,ic;0.1 ppo=other;0.1")
            .Add("other/w=belongs;0.1 p1w=he;0.2 n1w=to;0.1 n2w=apache;0.1" +
                " po=other;0.1 pow=other,belongs;0.1 powf=other,lc;0.1 ppo=other;0.1")
            .Add("other/w=to;0.1 p1w=belongs;0.2 p2w=he;0.1 n1w=apache;0.1" +
                " n2w=software;0.1 po=other;0.1 pow=other,to;0.1 powf=other,lc;0.1 ppo=other;0.1")
            .Add("org-start/w=apache;0.1 p1w=to;0.2 p2w=belongs;0.1 n1w=software;0.1 n2w=foundation;0.1" +
                " po=other;0.1 pow=other,Apache;0.1 powf=other,ic;0.1 ppo=other;0.1")
            .Add("org-cont/w=software;0.1 p1w=apache;0.2 p2w=to;0.1 n1w=foundation;0.1" +
                " n2w=.;0.1 po=org-start;0.1 pow=org-start,Software;0.1 powf=org-start,ic;0.1 ppo=other;0.1")
            .Add("org-cont/w=foundation;0.1 p1w=software;0.2 p2w=apache;0.1 n1w=.;0.1 po=org-cont;0.1" +
                " pow=org-cont,Foundation;0.1 powf=org-cont,ic;0.1 ppo=org-start;0.1")
            .Add("other/w=.;0.1 p1w=foundation;0.1 p2w=software;0.1 po=org-cont;0.1 pow=org-cont,.;0.1" +
                " powf=org-cont,other;0.1 ppo=org-cont;0.1")
            .Build();

        indexer.Index(eventStream);
        ClassicAssert.AreEqual(3, indexer.Contexts.Length);
        CollectionAssert.AreEqual(new int[] { 0 }, indexer.Contexts[0]);
        CollectionAssert.AreEqual(new int[] { 0 }, indexer.Contexts[1]);
        CollectionAssert.AreEqual(new int[] { 0 }, indexer.Contexts[2]);
        ClassicAssert.AreEqual(3, indexer.Values!.Length);
        const float delta = 0.001F;
        // NOpenNLP: ClassicAssert has no array overload with a tolerance, so the
        // constraint model is used for the three float-array comparisons, as
        // CLAUDE.md prescribes for assertArrayEquals with a delta.
        Assert.That(indexer.Values[0],
            Is.EqualTo(new float[] { 0.1F, 0.2F, 0.1F, 0.1F, 0.1F, 0.1F, 0.1F }).Within(delta));
        Assert.That(indexer.Values[1],
            Is.EqualTo(new float[] { 0.1F, 0.2F, 0.1F, 0.1F, 0.1F, 0.1F, 0.1F, 0.1F, 0.1F }).Within(delta));
        Assert.That(indexer.Values[2],
            Is.EqualTo(new float[] { 0.1F, 0.2F, 0.1F, 0.1F, 0.1F, 0.1F, 0.1F, 0.1F, 0.1F }).Within(delta));
        ClassicAssert.AreEqual(5, indexer.NumEvents);
        CollectionAssert.AreEqual(new int[] { 0, 1, 2 }, indexer.OutcomeList);
        CollectionAssert.AreEqual(new int[] { 3, 1, 1 }, indexer.NumTimesEventsSeen);
        CollectionAssert.AreEqual(new string[] { "ppo=other" }, indexer.PredLabels);
        CollectionAssert.AreEqual(new string[] { "other", "org-start", "org-cont" }, indexer.OutcomeLabels);
        CollectionAssert.AreEqual(new int[] { 5 }, indexer.PredCounts);
    }
}
