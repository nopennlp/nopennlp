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

using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// The <see cref="TokenNameFinderEvaluator"/> measures the performance
/// of the given <see cref="ITokenNameFinder"/> with the provided
/// reference <see cref="NameSample"/>s.
/// </summary>
/// <seealso cref="Evaluator{T}"/>
/// <seealso cref="ITokenNameFinder"/>
/// <seealso cref="NameSample"/>
public class TokenNameFinderEvaluator : Evaluator<NameSample>
{
    // NOpenNLP: made readonly
    private readonly FMeasure fmeasure = new();

    /// <summary>
    /// The <see cref="ITokenNameFinder"/> used to create the predicted
    /// <see cref="NameSample"/> objects.
    /// </summary>
    // NOpenNLP: made readonly
    private readonly ITokenNameFinder nameFinder;

    /// <summary>
    /// Initializes the current instance with the given
    /// <see cref="ITokenNameFinder"/>.
    /// </summary>
    /// <param name="nameFinder">the <see cref="ITokenNameFinder"/> to evaluate.</param>
    /// <param name="listeners">evaluation sample listeners</param>
    public TokenNameFinderEvaluator(ITokenNameFinder nameFinder,
        params ITokenNameFinderEvaluationMonitor?[]? listeners)
        : base(listeners)
        => this.nameFinder = nameFinder;

    /// <summary>
    /// Evaluates the given reference <see cref="NameSample"/> object.
    /// <para/>
    /// This is done by finding the names with the
    /// <see cref="ITokenNameFinder"/> in the sentence from the reference
    /// <see cref="NameSample"/>. The found names are then used to
    /// calculate and update the scores.
    /// </summary>
    /// <param name="reference">the reference <see cref="NameSample"/>.</param>
    /// <returns>the predicted <see cref="NameSample"/>.</returns>
    protected override NameSample ProcessSample(NameSample reference)
    {
        if (reference.IsClearAdaptiveDataSet)
        {
            nameFinder.ClearAdaptiveData();
        }

        Span[] predictedNames = nameFinder.Find(reference.Sentence);
        Span[] references = reference.Names;

        // OPENNLP-396 When evaluating with a file in the old format
        // the type of the span is null, but must be set to default to match
        // the output of the name finder.
        for (int i = 0; i < references.Length; i++)
        {
            if (references[i].Type == null)
            {
                references[i] = new Span(references[i].Start, references[i].End, "default");
            }
        }

        fmeasure.UpdateScores(references, predictedNames);

        return new NameSample(reference.Sentence, predictedNames, reference.IsClearAdaptiveDataSet);
    }

    public virtual FMeasure FMeasure => fmeasure;
}
