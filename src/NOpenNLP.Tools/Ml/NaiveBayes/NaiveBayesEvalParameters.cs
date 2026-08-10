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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using NOpenNLP.Tools.Ml.Model;

namespace NOpenNLP.Tools.Ml.Naivebayes;

/// <summary>
/// Parameters for the evalution of a naive bayes classifier
/// </summary>
public class NaiveBayesEvalParameters(Context[] @params, int numOutcomes, double[] outcomeTotals, long vocabulary)
    : EvalParameters(@params, numOutcomes)
{
    // NOpenNLP: made readonly
    protected readonly double[] outcomeTotals = outcomeTotals;
    protected readonly long vocabulary = vocabulary;

    public virtual double[] GetOutcomeTotals()
    {
        return outcomeTotals;
    }

    public virtual long GetVocabulary()
    {
        return vocabulary;
    }
}
