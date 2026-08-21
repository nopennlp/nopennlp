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
using System.IO;
using J2N.Text;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Eval;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public abstract class EvaluationErrorPrinter<T> : IEvaluationMonitor<T>
{
    // NOpenNLP: upstream wraps the OutputStream in a PrintStream. A TextWriter is the
    // .NET counterpart, and the callers already hand over Console.Error rather than a
    // raw stream, so no wrapping is needed.
    protected TextWriter printStream;

    protected EvaluationErrorPrinter(TextWriter outputStream)
    {
        this.printStream = outputStream;
    }

    // for the sentence detector
    protected void PrintError(Span[] references, Span[] predictions,
        T referenceSample, T predictedSample, string sentence)
    {
        var falseNegatives = new JCG.List<Span>();
        var falsePositives = new JCG.List<Span>();

        FindErrors(references, predictions, falseNegatives, falsePositives);

        if (falsePositives.Count + falseNegatives.Count > 0)
        {
            PrintSamples(referenceSample, predictedSample);

            PrintErrors(falsePositives, falseNegatives, sentence);
        }
    }

    // for namefinder, chunker...
    protected void PrintError(string? id, Span[] references, Span[] predictions,
        T referenceSample, T predictedSample, string[] sentenceTokens)
    {
        var falseNegatives = new JCG.List<Span>();
        var falsePositives = new JCG.List<Span>();

        FindErrors(references, predictions, falseNegatives, falsePositives);

        if (falsePositives.Count + falseNegatives.Count > 0)
        {
            if (id != null)
            {
                printStream.WriteLine("Id: {" + id + "}");
            }

            PrintSamples(referenceSample, predictedSample);

            PrintErrors(falsePositives, falseNegatives, sentenceTokens);
        }
    }

    protected void PrintError(Span[] references, Span[] predictions,
        T referenceSample, T predictedSample, string[] sentenceTokens) =>
        PrintError(null, references, predictions, referenceSample, predictedSample, sentenceTokens);

    // for pos tagger
    protected void PrintError(string[] references, string[] predictions,
        T referenceSample, T predictedSample, string[] sentenceTokens)
    {
        var filteredDoc = new JCG.List<string>();
        var filteredRefs = new JCG.List<string>();
        var filteredPreds = new JCG.List<string>();

        for (int i = 0; i < references.Length; i++)
        {
            if (!references[i].Equals(predictions[i], System.StringComparison.Ordinal))
            {
                filteredDoc.Add(sentenceTokens[i]);
                filteredRefs.Add(references[i]);
                filteredPreds.Add(predictions[i]);
            }
        }

        if (filteredDoc.Count > 0)
        {
            PrintSamples(referenceSample, predictedSample);

            PrintErrors(filteredDoc, filteredRefs, filteredPreds);
        }
    }

    // for others
    protected virtual void PrintError(T referenceSample, T predictedSample)
    {
        PrintSamples(referenceSample, predictedSample);
        printStream.WriteLine();
    }

    /// <summary>
    /// Auxiliary method to print tag errors.
    /// </summary>
    /// <param name="filteredDoc">the document tokens which were tagged wrong</param>
    /// <param name="filteredRefs">the reference tags</param>
    /// <param name="filteredPreds">the predicted tags</param>
    private void PrintErrors(IList<string> filteredDoc, IList<string> filteredRefs,
        IList<string> filteredPreds)
    {
        printStream.WriteLine("Errors: {");
        printStream.WriteLine("Tok: Ref | Pred");
        printStream.WriteLine("---------------");
        for (int i = 0; i < filteredDoc.Count; i++)
        {
            printStream.WriteLine(filteredDoc[i] + ": " + filteredRefs[i]
                + " | " + filteredPreds[i]);
        }

        printStream.WriteLine("}\n");
    }

    /// <summary>
    /// Auxiliary method to print span errors.
    /// </summary>
    /// <param name="falsePositives">false positives span</param>
    /// <param name="falseNegatives">false negative span</param>
    /// <param name="doc">the document text</param>
    private void PrintErrors(IList<Span> falsePositives,
        IList<Span> falseNegatives, string doc)
    {
        printStream.WriteLine("False positives: {");
        foreach (Span span in falsePositives)
        {
            printStream.WriteLine(span.GetCoveredText(doc.AsCharSequence()).ToString());
        }

        printStream.WriteLine("} False negatives: {");
        foreach (Span span in falseNegatives)
        {
            printStream.WriteLine(span.GetCoveredText(doc.AsCharSequence()).ToString());
        }

        printStream.WriteLine("}\n");
    }

    /// <summary>
    /// Auxiliary method to print span errors.
    /// </summary>
    /// <param name="falsePositives">false positives span</param>
    /// <param name="falseNegatives">false negative span</param>
    /// <param name="toks">the document tokens</param>
    private void PrintErrors(IList<Span> falsePositives,
        IList<Span> falseNegatives, string[] toks)
    {
        printStream.WriteLine("False positives: {");
        printStream.WriteLine(Print(falsePositives, toks));
        printStream.WriteLine("} False negatives: {");
        printStream.WriteLine(Print(falseNegatives, toks));
        printStream.WriteLine("}\n");
    }

    /// <summary>
    /// Auxiliary method to print spans.
    /// </summary>
    /// <param name="spans">the span list</param>
    /// <param name="toks">the tokens array</param>
    /// <returns>the spans as string</returns>
    // NOpenNLP: upstream renders the array with Arrays.toString, which is
    // "[a, b, c]" -- and "[]" for an empty array. Composed here since .NET has no
    // equivalent.
    private static string Print(IList<Span> spans, string[] toks) =>
        "[" + string.Join(", ", Span.SpansToStrings([.. spans], toks)) + "]";

    /// <summary>
    /// Auxiliary method to print expected and predicted samples.
    /// </summary>
    /// <param name="referenceSample">the reference sample</param>
    /// <param name="predictedSample">the predicted sample</param>
    private void PrintSamples<S>(S referenceSample, S predictedSample)
    {
        string details = "Expected: {\n" + referenceSample + "}\nPredicted: {\n"
            + predictedSample + "}";
        printStream.WriteLine(details);
    }

    /// <summary>
    /// Outputs falseNegatives and falsePositives spans from the references and
    /// predictions list.
    /// </summary>
    /// <param name="references">the reference spans</param>
    /// <param name="predictions">the predicted spans</param>
    /// <param name="falseNegatives">[out] the false negatives list</param>
    /// <param name="falsePositives">[out] the false positives list</param>
    private static void FindErrors(Span[] references, Span[] predictions,
        IList<Span> falseNegatives, IList<Span> falsePositives)
    {
        foreach (Span reference in references)
        {
            falseNegatives.Add(reference);
        }

        foreach (Span prediction in predictions)
        {
            falsePositives.Add(prediction);
        }

        foreach (Span referenceName in references)
        {
            foreach (Span prediction in predictions)
            {
                if (referenceName.Equals(prediction))
                {
                    // got it, remove from fn and fp
                    falseNegatives.Remove(referenceName);
                    falsePositives.Remove(prediction);
                }
            }
        }
    }

    public virtual void CorrectlyClassified(T reference, T prediction)
    {
        // do nothing
    }

    public abstract void Misclassified(T reference, T prediction);
}
