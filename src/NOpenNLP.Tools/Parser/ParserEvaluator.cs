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
using J2N.Text;
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Eval;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser;

/// <summary>
/// Class for ParserEvaluator.
/// This ParserEvaluator behaves like EVALB with no exceptions, e.g,
/// without removing punctuation tags, or equality between ADVP and PRT
/// (as in COLLINS convention). To follow parsing evaluation conventions
/// (Bikel, Collins, Charniak, etc.) as in EVALB, options are to be added
/// to the <c>ParserEvaluatorTool</c>.
/// </summary>
public class ParserEvaluator : Evaluator<Parse>
{
    /// <summary>
    /// fmeasure.
    /// </summary>
    private readonly FMeasure fmeasure = new(); // NOpenNLP: made readonly

    /// <summary>
    /// The parser to evaluate.
    /// </summary>
    private readonly IParser parser;

    // NOpenNLP: upstream calls opennlp.tools.cmdline.parser.ParserTool.parseLine to
    // turn a sentence into an unparsed Parse. The cmdline package is not ported, so
    // that method's body is inlined below, along with the two patterns it uses.
    private static readonly Regex untokenizedParenPattern1 = new("([^ ])([({)}])", RegexOptions.Compiled);
    private static readonly Regex untokenizedParenPattern2 = new("([({)}])([^ ])", RegexOptions.Compiled);

    /// <summary>
    /// Construct a parser with some evaluation monitors.
    /// </summary>
    /// <param name="aParser">the parser to evaluate</param>
    /// <param name="monitors">the evaluation monitors</param>
    public ParserEvaluator(IParser aParser, params IParserEvaluationMonitor?[]? monitors)
        : base(monitors)
        => this.parser = aParser;

    /// <summary>
    /// Obtain <see cref="Span"/>s for every parse in the sentence.
    /// </summary>
    /// <param name="parse">the parse from which to obtain the spans</param>
    /// <returns>an array containing every span for the parse</returns>
    private static Span[] GetConstituencySpans(Parse parse)
    {
        // NOpenNLP: upstream uses java.util.Stack; a List used as a stack stands in
        // for it, pushing at the end and popping from the end.
        JCG.List<Parse> stack = new();

        if (parse.ChildCount > 0)
        {
            foreach (Parse child in parse.GetChildren())
            {
                stack.Add(child);
            }
        }

        IList<Span> consts = new JCG.List<Span>();

        while (stack.Count > 0)
        {
            Parse constSpan = stack[^1];
            stack.RemoveAt(stack.Count - 1);

            if (!constSpan.IsPosTag)
            {
                Span span = constSpan.Span;
                consts.Add(new Span(span.Start, span.End, constSpan.Type));

                foreach (Parse child in constSpan.GetChildren())
                {
                    stack.Add(child);
                }
            }
        }

        return [.. consts];
    }

    /// <inheritdoc/>
    protected sealed override Parse ProcessSample(Parse reference)
    {
        IList<string> tokens = new JCG.List<string>();
        foreach (Parse token in reference.GetTokenNodes())
        {
            tokens.Add(token.Span.GetCoveredText(reference.Text.AsCharSequence()).ToString());
        }

        Parse[] predictions = ParseLine(string.Join(" ", tokens), parser, 1);

        Parse prediction = null!;
        if (predictions.Length > 0)
        {
            prediction = predictions[0];
            fmeasure.UpdateScores(GetConstituencySpans(reference), GetConstituencySpans(prediction));
        }

        return prediction;
    }

    /// <summary>
    /// NOpenNLP: stands in for <c>opennlp.tools.cmdline.parser.ParserTool.parseLine</c>,
    /// which the unported cmdline package provides upstream.
    /// </summary>
    private static Parse[] ParseLine(string line, IParser parser, int numParses)
    {
        // fix some parens patterns
        line = untokenizedParenPattern1.Replace(line, "$1 $2");
        line = untokenizedParenPattern2.Replace(line, "$1 $2");

        // tokenize
        string[] tokens = WhitespaceTokenizer.INSTANCE.Tokenize(line);
        string text = string.Join(" ", tokens);

        Parse p = new(text, new Span(0, text.Length), AbstractBottomUpParser.INC_NODE, 0, 0);
        int start = 0;
        for (int i = 0; i < tokens.Length; i++)
        {
            string tok = tokens[i];
            p.Insert(new Parse(text, new Span(start, start + tok.Length),
                AbstractBottomUpParser.TOK_NODE, 0, i));
            start += tok.Length + 1;
        }

        Parse[] parses;
        if (numParses == 1)
        {
            parses = [parser.Parse(p)];
        }
        else
        {
            parses = parser.Parse(p, numParses);
        }

        return parses;
    }

    /// <summary>
    /// It returns the fmeasure result.
    /// </summary>
    public FMeasure FMeasure => fmeasure;
}
