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

using System.Globalization;
using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// The <see cref="POSEvaluator"/> measures the performance of
/// the given <see cref="IPOSTagger"/> with the provided reference
/// <see cref="POSSample"/>s.
/// </summary>
public class POSEvaluator : Evaluator<POSSample>
{
    private readonly IPOSTagger tagger; // NOpenNLP: made readonly
    private readonly Mean wordAccuracy = new(); // NOpenNLP: made readonly

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="tagger">the <see cref="IPOSTagger"/> to evaluate.</param>
    /// <param name="listeners">an array of evaluation listeners</param>
    public POSEvaluator(IPOSTagger tagger, params IPOSTaggerEvaluationMonitor?[]? listeners)
        : base(listeners)
        => this.tagger = tagger;

    /// <summary>
    /// Evaluates the given reference <see cref="POSSample"/> object.
    /// <para/>
    /// This is done by tagging the sentence from the reference
    /// <see cref="POSSample"/> with the <see cref="IPOSTagger"/>. The
    /// tags are then used to update the word accuracy score.
    /// </summary>
    /// <param name="reference">the reference <see cref="POSSample"/>.</param>
    /// <returns>the predicted <see cref="POSSample"/>.</returns>
    protected override POSSample ProcessSample(POSSample reference)
    {
        string[] predictedTags = tagger.Tag(reference.Sentence, reference.AddictionalContext);
        string[] referenceTags = reference.Tags;

        for (int i = 0; i < referenceTags.Length; i++)
        {
            if (referenceTags[i].Equals(predictedTags[i]))
            {
                wordAccuracy.Add(1);
            }
            else
            {
                wordAccuracy.Add(0);
            }
        }

        return new POSSample(reference.Sentence, predictedTags);
    }

    /// <summary>
    /// Retrieves the word accuracy.
    /// <para/>
    /// This is defined as:
    /// word accuracy = correctly detected tags / total words
    /// </summary>
    public virtual double WordAccuracy => wordAccuracy.Value;

    /// <summary>
    /// Retrieves the total number of words considered
    /// in the evaluation.
    /// </summary>
    public virtual long WordCount => wordAccuracy.Count;

    // NOpenNLP: upstream concatenates the mean, which Java renders with Double.toString --
    // a perfect accuracy prints "1.0" where .NET's default formatting prints "1". J2N's "J"
    // format reproduces that, matching the sibling evaluators in the port.
    /// <summary>
    /// Represents this objects as human readable <see cref="string"/>.
    /// </summary>
    public override string ToString() =>
        "Accuracy:" + J2N.Numerics.Double.ToString(wordAccuracy.Value, "J", CultureInfo.InvariantCulture) +
            " Number of Samples: " + wordAccuracy.Count.ToString(CultureInfo.InvariantCulture);
}
