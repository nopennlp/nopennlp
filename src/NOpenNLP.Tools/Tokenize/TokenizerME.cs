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

using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Tokenize.Lang;
using NOpenNLP.Tools.Util;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// A ITokenizer for converting raw text into separated tokens.  It uses
/// Maximum Entropy to make its decisions.  The features are loosely
/// based off of Jeff Reynar's UPenn thesis "Topic Segmentation:
/// Algorithms and Applications.", which is available from his
/// homepage: <a href="http://www.cis.upenn.edu/~jcreynar">http://www.cis.upenn.edu/~jcreynar</a>.
/// <para/>
/// This tokenizer needs a statistical model to tokenize a text which reproduces
/// the tokenization observed in the training data used to create the model.
/// The <see cref="TokenizerModel"/> class encapsulates the model and provides
/// methods to create it from the binary representation.
/// <para/>
/// A tokenizer instance is not thread safe. For each thread one tokenizer
/// must be instantiated which can share one <code>TokenizerModel</code> instance
/// to safe memory.
/// <para/>
/// To train a new model {<c>Train</c> method
/// can be used.
/// <para/>
/// Sample usage:
/// <para/>
/// <code>
/// Stream modelIn;<br/>
/// <br/>
/// ...<br/>
/// <br/>
/// TokenizerModel model = TokenizerModel(modelIn);<br/>
/// <br/>
/// ITokenizer tokenizer = new TokenizerME(model);<br/>
/// <br/>
/// String tokens[] = tokenizer.tokenize("A sentence to be tokenized.");
/// </code>
/// </summary>
/// <remarks>
/// <seealso cref="ITokenizer"/>
/// <seealso cref="TokenizerModel"/>
/// See <c>TokenSample</c>.
/// </remarks>
public class TokenizerME : AbstractTokenizer
{
    /// <summary>
    /// Constant indicates a token split.
    /// </summary>
    public const string SPLIT = "T";

    /// <summary>
    /// Constant indicates no token split.
    /// </summary>
    public const string NO_SPLIT = "F";

    /// <summary>
    /// Alpha-Numeric Regex
    /// </summary>
    /// <remarks>Deprecated: As of release 1.5.2, replaced by <see cref="Lang.Factory.GetAlphanumeric(string)"/></remarks>
    public static readonly Regex alphaNumeric = new(Factory.DEFAULT_ALPHANUMERIC);

    private readonly Regex? alphanumeric;

    /// <summary>
    /// The maximum entropy model to use to evaluate contexts.
    /// </summary>
    private readonly IMaxentModel model; // NOpenNLP: made readonly

    /// <summary>
    /// The context generator.
    /// </summary>
    private readonly ITokenContextGenerator cg;

    /// <summary>
    /// Optimization flag to skip alpha numeric tokens for further
    /// tokenization
    /// </summary>
    private readonly bool useAlphaNumericOptimization; // NOpenNLP: made readonly

    /// <summary>
    /// List of probabilities for each token returned from a call to
    /// <see cref="TokenizePos"/>.
    /// </summary>
    private readonly IList<double> tokProbs; // NOpenNLP: made readonly

    private readonly IList<Span> newTokens; // NOpenNLP: made readonly

    public TokenizerME(TokenizerModel model)
    {
        TokenizerFactory factory = model.Factory;
        alphanumeric = factory.AlphaNumericPattern;
        cg = factory.ContextGenerator;
        this.model = model.MaxentModel;
        useAlphaNumericOptimization = factory.UseAlphaNumericOptmization;
        newTokens = new List<Span>();
        tokProbs = new List<double>(50);
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    /// Deprecated: Use <see cref="TokenizerFactory"/> to extend the ITokenizer
    ///             functionality
    /// </remarks>
    public TokenizerME(TokenizerModel model, Factory factory)
    {
        string languageCode = model.Language;
        alphanumeric = factory.GetAlphanumeric(languageCode);
        cg = factory.CreateTokenContextGenerator(languageCode, GetAbbreviations(model.Abbreviations));
        this.model = model.MaxentModel;
        useAlphaNumericOptimization = model.UseAlphaNumericOptimization;
        newTokens = new List<Span>();
        tokProbs = new List<double>(50);
    }

    private static JCG.HashSet<string> GetAbbreviations(NOpenNLP.Tools.Dictionary.Dictionary? abbreviations)
    {
        if (abbreviations == null)
        {
            return [];
        }

        return [.. abbreviations.AsStringSet()];
    }

    /// <summary>
    /// Returns the probabilities associated with the most recent
    /// calls to <see cref="AbstractTokenizer.Tokenize(string)"/> or <see cref="TokenizerME.TokenizePos(string)"/>.
    /// </summary>
    /// <returns>probability for each token returned for the most recent
    ///     call to tokenize.  If not applicable an empty array is returned.</returns>
    public virtual double[] TokenProbabilities
    {
        get
        {
            double[] tokProbArray = new double[tokProbs.Count];
            for (int i = 0; i < tokProbArray.Length; i++)
            {
                tokProbArray[i] = tokProbs[i];
            }

            return tokProbArray;
        }
    }

    /// <summary>
    /// Tokenizes the string.
    /// </summary>
    /// <param name="d">The string to be tokenized.</param>
    /// <returns>  A span array containing individual tokens as elements.</returns>
    public override Span[] TokenizePos(string d)
    {
        Span[] tokens = WhitespaceTokenizer.INSTANCE.TokenizePos(d);
        newTokens.Clear();
        tokProbs.Clear();
        foreach (Span s in tokens)
        {
            // NOpenNLP: Java substring(begin, end) takes an end index; .NET takes a length.
            string tok = d.Substring(s.Start, s.End - s.Start);

            // Can't tokenize single characters
            if (tok.Length < 2)
            {
                newTokens.Add(s);
                tokProbs.Add(1);
            }
            else if (UseAlphaNumericOptimization && alphanumeric.IsMatch(tok))
            {
                newTokens.Add(s);
                tokProbs.Add(1);
            }
            else
            {
                int start = s.Start;
                int end = s.End;
                int origStart = s.Start;
                double tokenProb = 1;
                for (int j = origStart + 1; j < end; j++)
                {
                    double[] probs = model.Eval(cg.GetContext(tok, j - origStart));
                    string best = model.GetBestOutcome(probs);
                    tokenProb *= probs[model.GetIndex(best)];
                    if (best.Equals(SPLIT))
                    {
                        newTokens.Add(new Span(start, j));
                        tokProbs.Add(tokenProb);
                        start = j;
                        tokenProb = 1;
                    }
                }

                newTokens.Add(new Span(start, end));
                tokProbs.Add(tokenProb);
            }
        }

        return [.. newTokens];
    }

    // /// <summary>
    // /// Trains a model for the {@link TokenizerME}.
    // /// </summary>
    // /// <param name="samples">
    // ///          the samples used for the training.</param>
    // /// <param name="factory">
    // ///          a {@link TokenizerFactory} to get resources from</param>
    // /// <param name="mlParams">
    // ///          the machine learning train parameters</param>
    // /// <returns>the trained {@link TokenizerModel}</returns>
    // /// <exception cref="IOException">
    // ///           it throws an {@link IOException} if an {@link IOException} is
    // ///           thrown during IO operations on a temp file which is created
    // ///           during training. Or if reading from the {@link ObjectStream}
    // ///           fails.</exception>
    // public static TokenizerModel Train(ObjectStream<TokenSample> samples, TokenizerFactory factory, TrainingParameters mlParams)
    // {
    //     Dictionary<string, string> manifestInfoEntries = new Dictionary<string, string>();
    //     ObjectStream<Event> eventStream = new TokSpanEventStream(samples, factory.IsUseAlphaNumericOptmization(), factory.GetAlphaNumericPattern(), factory.GetContextGenerator());
    //     EventTrainer trainer = TrainerFactory.GetEventTrainer(mlParams, manifestInfoEntries);
    //     IMaxentModel maxentModel = trainer.Train(eventStream);
    //     return new TokenizerModel(maxentModel, manifestInfoEntries, factory);
    // }

    /// <summary>
    /// Returns the value of the alpha-numeric optimization flag.
    /// </summary>
    /// <returns>true if the tokenizer should use alpha-numeric optimization, false otherwise.</returns>
    public virtual bool UseAlphaNumericOptimization => useAlphaNumericOptimization;
}
