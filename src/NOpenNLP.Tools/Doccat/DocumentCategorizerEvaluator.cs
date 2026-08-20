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
using System.Globalization;
using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// The <see cref="DocumentCategorizerEvaluator"/> measures the performance of
/// the given <see cref="IDocumentCategorizer"/> with the provided reference
/// <see cref="DocumentSample"/>s.
/// </summary>
/// <seealso cref="IDocumentCategorizer"/>
/// <seealso cref="DocumentSample"/>
public class DocumentCategorizerEvaluator : Evaluator<DocumentSample>
{
    private readonly IDocumentCategorizer categorizer; // NOpenNLP: made readonly

    private readonly Mean accuracy = new(); // NOpenNLP: made readonly

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="categorizer">the document categorizer instance</param>
    /// <param name="listeners">an array of evaluation listeners</param>
    public DocumentCategorizerEvaluator(IDocumentCategorizer categorizer,
        params IDoccatEvaluationMonitor?[]? listeners)
        : base(listeners)
        => this.categorizer = categorizer;

    /// <summary>
    /// Evaluates the given reference <see cref="DocumentSample"/> object.
    /// <para/>
    /// This is done by categorizing the document from the provided
    /// <see cref="DocumentSample"/>. The detected category is then used
    /// to calculate and update the score.
    /// </summary>
    /// <param name="reference">the reference <see cref="DocumentSample"/>.</param>
    /// <returns>the predicted <see cref="DocumentSample"/>.</returns>
    protected override DocumentSample ProcessSample(DocumentSample reference)
    {
        string[] document = reference.Text;

        double[] probs = categorizer.Categorize(document);

        string cat = categorizer.GetBestCategory(probs);

        if (reference.Category.Equals(cat))
        {
            accuracy.Add(1);
        }
        else
        {
            accuracy.Add(0);
        }

        return new DocumentSample(cat, reference.Text);
    }

    /// <summary>
    /// Retrieves the accuracy of provided <see cref="IDocumentCategorizer"/>.
    /// <para/>
    /// accuracy = correctly categorized documents / total documents
    /// </summary>
    public virtual double Accuracy => accuracy.Value;

    public virtual long DocumentCount => accuracy.Count;

    /// <summary>
    /// Represents this objects as human readable <see cref="string"/>.
    /// </summary>
    public override string ToString() =>
        "Accuracy: " + J2N.Numerics.Double.ToString(accuracy.Value, "J", CultureInfo.InvariantCulture) + "\n" +
            "Number of documents: " + accuracy.Count.ToString(CultureInfo.InvariantCulture);
}
