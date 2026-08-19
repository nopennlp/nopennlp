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

// NOpenNLP: upstream calls new TrainingParameters(Collections.emptyMap()), which
// ports onto the deprecated string-map constructor. The obsoletion warning is
// suppressed here rather than deviating from the upstream test.
#pragma warning disable CS0618 // Type or member is obsolete
public class OnePassDataIndexerTest
{
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

        IDataIndexer indexer = new OnePassDataIndexer();
        indexer.Init(new TrainingParameters(new Dictionary<string, string>()), null);
        indexer.Index(eventStream);
        ClassicAssert.AreEqual(3, indexer.Contexts.Length);
        CollectionAssert.AreEqual(new int[] { 0 }, indexer.Contexts[0]);
        CollectionAssert.AreEqual(new int[] { 0 }, indexer.Contexts[1]);
        CollectionAssert.AreEqual(new int[] { 0 }, indexer.Contexts[2]);
        ClassicAssert.IsNull(indexer.Values);
        ClassicAssert.AreEqual(5, indexer.NumEvents);
        CollectionAssert.AreEqual(new int[] { 0, 1, 2 }, indexer.OutcomeList);
        CollectionAssert.AreEqual(new int[] { 3, 1, 1 }, indexer.NumTimesEventsSeen);
        CollectionAssert.AreEqual(new string[] { "ppo=other" }, indexer.PredLabels);
        CollectionAssert.AreEqual(new string[] { "other", "org-start", "org-cont" }, indexer.OutcomeLabels);
        CollectionAssert.AreEqual(new int[] { 5 }, indexer.PredCounts);
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
