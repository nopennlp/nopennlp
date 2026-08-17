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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Util.Eval;

/// <summary>
/// The <see cref="FMeasure"/> is an utility class for evaluators
/// which measure precision, recall and the resulting f-measure.
/// <para/>
/// Evaluation results are the arithmetic mean of the precision
/// scores calculated for each reference sample and
/// the arithmetic mean of the recall scores calculated for
/// each reference sample.
/// </summary>
public sealed class FMeasure
{
    /// <summary>
    /// |selected| = true positives + false positives <br/>
    /// the count of selected (or retrieved) items.
    /// </summary>
    private long selected;

    /// <summary>
    /// |target| = true positives + false negatives <br/>
    /// the count of target (or correct) items.
    /// </summary>
    private long target;

    /// <summary>
    /// Storing the number of true positives found.
    /// </summary>
    private long truePositive;

    /// <summary>
    /// Retrieves the arithmetic mean of the precision scores calculated for each
    /// evaluated sample.
    /// </summary>
    /// <returns>the arithmetic mean of all precision scores</returns>
    public double PrecisionScore => selected > 0 ? (double)truePositive / selected : 0;

    /// <summary>
    /// Retrieves the arithmetic mean of the recall score calculated for each
    /// evaluated sample.
    /// </summary>
    /// <returns>the arithmetic mean of all recall scores</returns>
    public double RecallScore => target > 0 ? (double)truePositive / target : 0;

    /// <summary>
    /// Retrieves the f-measure score.
    /// <para/>
    /// f-measure = 2 * precision * recall / (precision + recall)
    /// </summary>
    /// <returns>the f-measure or -1 if precision + recall &lt;= 0</returns>
    public double Value
    {
        get
        {
            if (PrecisionScore + RecallScore > 0)
            {
                return 2 * (PrecisionScore * RecallScore) / (PrecisionScore + RecallScore);
            }
            else
            {
                // cannot divide by zero, return error code
                return -1;
            }
        }
    }

    /// <summary>
    /// Updates the score based on the number of true positives and
    /// the number of predictions and references.
    /// </summary>
    /// <param name="references">the provided references</param>
    /// <param name="predictions">the predicted spans</param>
    public void UpdateScores(object[] references, object[] predictions)
    {
        truePositive += CountTruePositives(references, predictions);
        selected += predictions.Length;
        target += references.Length;
    }

    /// <summary>
    /// Merge results into fmeasure metric.
    /// </summary>
    /// <param name="measure">the fmeasure</param>
    public void MergeInto(FMeasure measure)
    {
        this.selected += measure.selected;
        this.target += measure.target;
        this.truePositive += measure.truePositive;
    }

    /// <summary>
    /// Creates a human read-able <c>string</c> representation.
    /// </summary>
    /// <returns>the results</returns>
    // NOpenNLP: J2N's "J" format reproduces Java's Double.toString, which differs
    // from .NET's "R" -- Java renders integral values as "1.0" and small magnitudes
    // as "1.0E-5" where "R" gives "1" and "1E-05".
    public override string ToString() =>
        "Precision: " + J2N.Numerics.Double.ToString(PrecisionScore, "J", CultureInfo.InvariantCulture) + "\n"
        + "Recall: " + J2N.Numerics.Double.ToString(RecallScore, "J", CultureInfo.InvariantCulture) + "\n"
        + "F-Measure: " + J2N.Numerics.Double.ToString(Value, "J", CultureInfo.InvariantCulture);

    /// <summary>
    /// This method counts the number of objects which are equal and occur in the
    /// references and predictions arrays.
    /// Matched items are removed from the prediction list.
    /// </summary>
    /// <param name="references">the gold standard</param>
    /// <param name="predictions">the predictions</param>
    /// <returns>number of true positives</returns>
    internal static int CountTruePositives(object[] references, object[] predictions)
    {
        JCG.List<object> predListSpans = new(predictions.Length);
        predListSpans.AddRange(predictions);
        int truePositives = 0;
        object? matchedItem = null;

        // NOpenNLP: the inner loop deliberately mirrors upstream rather than breaking
        // on the first match and resetting matchedItem per reference. Upstream keeps
        // counting after a hit and never clears matchedItem between references, so a
        // reference that matches nothing still removes the previously matched item.
        // Changing either would alter the scores this method reports.
        foreach (object referenceName in references)
        {
            foreach (object predListSpan in predListSpans)
            {
                if (referenceName.Equals(predListSpan))
                {
                    matchedItem = predListSpan;
                    truePositives++;
                }
            }

            if (matchedItem != null)
            {
                predListSpans.Remove(matchedItem);
            }
        }

        return truePositives;
    }

    /// <summary>
    /// Calculates the precision score for the given reference and predicted spans.
    /// </summary>
    /// <param name="references">the gold standard spans</param>
    /// <param name="predictions">the predicted spans</param>
    /// <returns>the precision score or NaN if there are no predicted spans</returns>
    public static double Precision(object[] references, object[] predictions)
    {
        if (predictions.Length > 0)
        {
            return CountTruePositives(references, predictions) / (double)predictions.Length;
        }
        else
        {
            return double.NaN;
        }
    }

    /// <summary>
    /// Calculates the recall score for the given reference and predicted spans.
    /// </summary>
    /// <param name="references">the gold standard spans</param>
    /// <param name="predictions">the predicted spans</param>
    /// <returns>the recall score or NaN if there are no reference spans</returns>
    public static double Recall(object[] references, object[] predictions)
    {
        if (references.Length > 0)
        {
            return CountTruePositives(references, predictions) / (double)references.Length;
        }
        else
        {
            return double.NaN;
        }
    }
}
