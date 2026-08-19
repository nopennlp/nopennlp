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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Maxent;

public class MockDataIndexer : IDataIndexer
{
    public int[][] Contexts => [];

    public int[] NumTimesEventsSeen => [];

    public int[] OutcomeList => [];

    public string[] PredLabels => [];

    public int[] PredCounts => [];

    public string[] OutcomeLabels => [];

    public float[][]? Values => [];

    public int NumEvents => 0;

    public void Init(TrainingParameters trainParams, IDictionary<string, string>? reportMap)
    {
    }

    public void Index(IObjectStream<Event> eventStream)
    {
    }
}
