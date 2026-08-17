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
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ml;

public class BeamSearchTest
{
    private sealed class IdentityFeatureGenerator(string[] outcomeSequence) : IBeamSearchContextGenerator<string>
    {
        public string[] GetContext(int index, string[] sequence, string[] priorDecisions,
            object[] additionalContext) =>
            [outcomeSequence[index]];
    }

    // NOpenNLP: upstream passes the validator as a lambda, since Java's SequenceValidator is a
    // functional interface. ISequenceValidator<T> is a plain interface here, so the lambda becomes
    // a small adapter holding the same predicate.
    private sealed class DelegateSequenceValidator(Func<int, string[], string[], string, bool> predicate)
        : ISequenceValidator<string>
    {
        public bool ValidSequence(int i, string[] inputSequence, string[] outcomesSequence, string outcome) =>
            predicate(i, inputSequence, outcomesSequence, outcome);
    }

    private sealed class IdentityModel : IMaxentModel
    {
        private readonly string[] outcomes;

        private readonly JCG.Dictionary<string, int> outcomeIndexMap = [];

        private readonly double bestOutcomeProb = 0.8d;

        private readonly double otherOutcomeProb;

        public IdentityModel(string[] outcomes)
        {
            this.outcomes = outcomes;

            for (int i = 0; i < outcomes.Length; i++)
            {
                outcomeIndexMap[outcomes[i]] = i;
            }

            otherOutcomeProb = 0.2d / (outcomes.Length - 1);
        }

        public double[] Eval(string[] context)
        {
            var probs = new double[outcomes.Length];

            for (int i = 0; i < probs.Length; i++)
            {
                if (outcomes[i].Equals(context[0]))
                {
                    probs[i] = bestOutcomeProb;
                }
                else
                {
                    probs[i] = otherOutcomeProb;
                }
            }

            return probs;
        }

        public double[] Eval(string[] context, double[] probs) => Eval(context);

        public double[] Eval(string[] context, float[] values) => Eval(context);

        public string GetAllOutcomes(double[] outcomes) => null!;

        public string GetBestOutcome(double[] outcomes) => null!;

        public int GetIndex(string outcome) => 0;

        public int NumOutcomes => outcomes.Length;

        public string GetOutcome(int i) => outcomes[i];
    }

    /// <summary>
    /// Tests that beam search does not fail to detect an empty sequence.
    /// </summary>
    [Test]
    public void TestBestSequenceZeroLengthInput()
    {
        var sequence = new string[0];
        IBeamSearchContextGenerator<string> cg = new IdentityFeatureGenerator(sequence);

        string[] outcomes = ["1", "2", "3"];
        IMaxentModel model = new IdentityModel(outcomes);

        var bs = new BeamSearch<string>(3, model);

        var seq = bs.BestSequence(sequence, null, cg,
            new DelegateSequenceValidator((i, inputSequence, outcomesSequence, outcome) => true));

        ClassicAssert.NotNull(seq);
        ClassicAssert.AreEqual(sequence.Length, seq!.Outcomes.Count);
    }

    /// <summary>
    /// Tests finding a sequence of length one.
    /// </summary>
    [Test]
    public void TestBestSequenceOneElementInput()
    {
        string[] sequence = ["1"];
        IBeamSearchContextGenerator<string> cg = new IdentityFeatureGenerator(sequence);

        string[] outcomes = ["1", "2", "3"];
        IMaxentModel model = new IdentityModel(outcomes);

        var bs = new BeamSearch<string>(3, model);

        var seq = bs.BestSequence(sequence, null, cg,
            new DelegateSequenceValidator((i, inputSequence, outcomesSequence, outcome) => true));

        ClassicAssert.NotNull(seq);
        ClassicAssert.AreEqual(sequence.Length, seq!.Outcomes.Count);
        ClassicAssert.AreEqual("1", seq.Outcomes[0]);
    }

    /// <summary>
    /// Tests finding the best sequence on a short input sequence.
    /// </summary>
    [Test]
    public void TestBestSequence()
    {
        string[] sequence = ["1", "2", "3", "2", "1"];
        IBeamSearchContextGenerator<string> cg = new IdentityFeatureGenerator(sequence);

        string[] outcomes = ["1", "2", "3"];
        IMaxentModel model = new IdentityModel(outcomes);

        var bs = new BeamSearch<string>(2, model);

        var seq = bs.BestSequence(sequence, null, cg,
            new DelegateSequenceValidator((i, inputSequence, outcomesSequence, outcome) => true));

        ClassicAssert.NotNull(seq);
        ClassicAssert.AreEqual(sequence.Length, seq!.Outcomes.Count);
        ClassicAssert.AreEqual("1", seq.Outcomes[0]);
        ClassicAssert.AreEqual("2", seq.Outcomes[1]);
        ClassicAssert.AreEqual("3", seq.Outcomes[2]);
        ClassicAssert.AreEqual("2", seq.Outcomes[3]);
        ClassicAssert.AreEqual("1", seq.Outcomes[4]);
    }

    /// <summary>
    /// Tests finding the best sequence on a short input sequence.
    /// </summary>
    [Test]
    public void TestBestSequenceWithValidator()
    {
        string[] sequence = ["1", "2", "3", "2", "1"];
        IBeamSearchContextGenerator<string> cg = new IdentityFeatureGenerator(sequence);

        string[] outcomes = ["1", "2", "3"];
        IMaxentModel model = new IdentityModel(outcomes);

        var bs = new BeamSearch<string>(2, model, 0);

        var seq = bs.BestSequence(sequence, null, cg,
            new DelegateSequenceValidator((i, inputSequence, outcomesSequence, outcome) => !"2".Equals(outcome)));

        ClassicAssert.NotNull(seq);
        ClassicAssert.AreEqual(sequence.Length, seq!.Outcomes.Count);
        ClassicAssert.AreEqual("1", seq.Outcomes[0]);
        ClassicAssert.AreNotSame("2", seq.Outcomes[1]);
        ClassicAssert.AreEqual("3", seq.Outcomes[2]);
        ClassicAssert.AreNotSame("2", seq.Outcomes[3]);
        ClassicAssert.AreEqual("1", seq.Outcomes[4]);
    }
}
