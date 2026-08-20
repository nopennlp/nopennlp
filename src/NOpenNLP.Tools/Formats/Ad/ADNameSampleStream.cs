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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Ad;

/// <summary>
/// Parser for Floresta Sita(c)tica Arvores Deitadas corpus, output to for the
/// Portuguese NER training.
/// <para/>
/// The data contains four named entity types: Person, Organization, Group,
/// Place, Event, ArtProd, Abstract, Thing, Time and Numeric.<br/>
/// <para/>
/// Data can be found on this web site:<br/>
/// http://www.linguateca.pt/floresta/corpus.html
/// <para/>
/// Information about the format:<br/>
/// Susana Afonso.
/// "Árvores deitadas: Descrição do formato e das opções de análise na Floresta Sintáctica"
/// .<br/>
/// 12 de Fevereiro de 2006.
/// http://www.linguateca.pt/documentos/Afonso2006ArvoresDeitadas.pdf
/// <para/>
/// Detailed info about the NER tagset:
/// http://beta.visl.sdu.dk/visl/pt/info/portsymbol.html#semtags_names
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ADNameSampleStream : ObjectStreamBase<NameSample?>
{
    /// <summary>
    /// Pattern of a NER tag in Arvores Deitadas.
    /// </summary>
    private static readonly Regex tagPattern = new Regex("<(NER:)?(.*?)>");

    private static readonly Regex whitespacePattern = new Regex("\\s+");
    private static readonly Regex underlinePattern = new Regex("[_]+");
    private static readonly Regex hyphenPattern =
        new Regex("((\\p{L}+)-$)|(^-(\\p{L}+)(.*))|((\\p{L}+)-(\\p{L}+)(.*))");
    private static readonly Regex alphanumericPattern = new Regex("^[\\p{L}\\p{Nd}]+$");

    /// <summary>
    /// Map to the Arvores Deitadas types to our types. It is read-only.
    /// </summary>
    private static readonly IDictionary<string, string> Harem;

    static ADNameSampleStream()
    {
        JCG.Dictionary<string, string> harem = new JCG.Dictionary<string, string>();

        const string person = "person";
        harem["hum"] = person;
        harem["official"] = person;
        harem["member"] = person;

        const string organization = "organization";
        harem["admin"] = organization;
        harem["org"] = organization;
        harem["inst"] = organization;
        harem["media"] = organization;
        harem["party"] = organization;
        harem["suborg"] = organization;

        const string group = "group";
        harem["groupind"] = group;
        harem["groupofficial"] = group;

        const string place = "place";
        harem["top"] = place;
        harem["civ"] = place;
        harem["address"] = place;
        harem["site"] = place;
        harem["virtual"] = place;
        harem["astro"] = place;

        const string @event = "event";
        harem["occ"] = @event;
        harem["event"] = @event;
        harem["history"] = @event;

        const string artprod = "artprod";
        harem["tit"] = artprod;
        harem["pub"] = artprod;
        harem["product"] = artprod;
        harem["V"] = artprod;
        harem["artwork"] = artprod;

        const string @abstract = "abstract";
        harem["brand"] = @abstract;
        harem["genre"] = @abstract;
        harem["school"] = @abstract;
        harem["idea"] = @abstract;
        harem["plan"] = @abstract;
        harem["author"] = @abstract;
        harem["absname"] = @abstract;
        harem["disease"] = @abstract;

        const string thing = "thing";
        harem["object"] = thing;
        harem["common"] = thing;
        harem["mat"] = thing;
        harem["class"] = thing;
        harem["plant"] = thing;
        harem["currency"] = thing;

        const string time = "time";
        harem["date"] = time;
        harem["hour"] = time;
        harem["period"] = time;
        harem["cyclic"] = time;

        const string numeric = "numeric";
        harem["quantity"] = numeric;
        harem["prednum"] = numeric;
        // NOpenNLP: upstream puts "currency" a second time here, overwriting the "thing" mapping
        // set above, so the effective value is "numeric". Java's Map.put overwrites where C#'s
        // Add would throw, so the indexer is used to preserve that.
        harem["currency"] = numeric;

        Harem = new ReadOnlyDictionary<string, string>(harem);
    }

    private readonly IObjectStream<ADSentenceStream.Sentence?> adSentenceStream;

    /// <summary>
    /// To keep the last left contraction part.
    /// </summary>
    private string? leftContractionPart = null;

    private readonly bool splitHyphenatedTokens;

    /// <summary>
    /// Creates a new <see cref="NameSample"/> stream from a line stream, i.e.
    /// <see cref="IObjectStream{T}"/> of <see cref="string"/>, that could be a
    /// <see cref="PlainTextByLineStream"/> object.
    /// </summary>
    /// <param name="lineStream">a stream of lines as <see cref="string"/></param>
    /// <param name="splitHyphenatedTokens">
    /// if true hyphenated tokens will be separated: "carros-monstro" &gt; "carros" "-" "monstro"
    /// </param>
    public ADNameSampleStream(IObjectStream<string?> lineStream, bool splitHyphenatedTokens)
    {
        adSentenceStream = new ADSentenceStream(lineStream);
        this.splitHyphenatedTokens = splitHyphenatedTokens;
    }

    /// <summary>
    /// Creates a new <see cref="NameSample"/> stream from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="in">the Corpus <see cref="Stream"/></param>
    /// <param name="charsetName">the charset of the Arvores Deitadas Corpus</param>
    /// <param name="splitHyphenatedTokens">
    /// if true hyphenated tokens will be separated: "carros-monstro" &gt; "carros" "-" "monstro"
    /// </param>
    /// <exception cref="IOException">if there is an error during reading</exception>
    // NOpenNLP: upstream wraps this in a catch for UnsupportedEncodingException that its own
    // comment notes can never happen; the wrapper is dropped here.
    [Obsolete("Deprecated upstream.")]
    public ADNameSampleStream(IInputStreamFactory @in, string charsetName, bool splitHyphenatedTokens)
    {
        adSentenceStream = new ADSentenceStream(new PlainTextByLineStream(@in, charsetName));
        this.splitHyphenatedTokens = splitHyphenatedTokens;
    }

    private int textID = -1;

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override NameSample? Read()
    {
        ADSentenceStream.Sentence? paragraph;
        // we should look for text here.
        while ((paragraph = adSentenceStream.Read()) != null)
        {
            int currentTextID = GetTextID(paragraph);
            bool clearData = false;
            if (currentTextID != textID)
            {
                clearData = true;
                textID = currentTextID;
            }

            ADSentenceStream.SentenceParser.Node? root = paragraph.Root;
            IList<string> sentence = new JCG.List<string>();
            IList<Span> names = new JCG.List<Span>();
            Process(root, sentence, names);

            return new NameSample([.. sentence], [.. names], clearData);
        }
        return null;
    }

    /// <summary>
    /// Recursive method to process a node in Arvores Deitadas format.
    /// </summary>
    /// <param name="node">the node to be processed</param>
    /// <param name="sentence">the sentence tokens we got so far</param>
    /// <param name="names">the names we got so far</param>
    private void Process(ADSentenceStream.SentenceParser.Node? node, IList<string> sentence, IList<Span> names)
    {
        if (node != null)
        {
            foreach (ADSentenceStream.SentenceParser.TreeElement element in node.Elements)
            {
                if (element.IsLeaf)
                {
                    ProcessLeaf((ADSentenceStream.SentenceParser.Leaf)element, sentence, names);
                }
                else
                {
                    Process((ADSentenceStream.SentenceParser.Node)element, sentence, names);
                }
            }
        }
    }

    /// <summary>
    /// Process a Leaf of Arvores Detaitadas format.
    /// </summary>
    /// <param name="leaf">the leaf to be processed</param>
    /// <param name="sentence">the sentence tokens we got so far</param>
    /// <param name="names">the names we got so far</param>
    private void ProcessLeaf(ADSentenceStream.SentenceParser.Leaf leaf, IList<string> sentence, IList<Span> names)
    {
        bool alreadyAdded = false;

        if (leftContractionPart != null)
        {
            // will handle the contraction
            string right = leaf.Lexeme!;

            string? c = PortugueseContractionUtility.ToContraction(leftContractionPart, right);
            if (c != null)
            {
                string[] parts = SplitDroppingTrailingEmpty(whitespacePattern, c);
                foreach (string part in parts)
                {
                    sentence.Add(part);
                }
                alreadyAdded = true;
            }
            else
            {
                // contraction was missing! why?
                sentence.Add(leftContractionPart);
                // keep alreadyAdded false.
            }
            leftContractionPart = null;
        }

        string? namedEntityTag = null;
        int startOfNamedEntity = -1;

        string? leafTag = leaf.SecondaryTag;
        bool expandLastNER = false; // used when we find a <NER2> tag

        if (leafTag != null)
        {
            if (leafTag.Contains("<sam->") && !alreadyAdded)
            {
                string[] lexemes = SplitDroppingTrailingEmpty(underlinePattern, leaf.Lexeme!);
                if (lexemes.Length > 1)
                {
                    for (int i = 0; i < lexemes.Length - 1; i++)
                    {
                        sentence.Add(lexemes[i]);
                    }
                }
                leftContractionPart = lexemes[lexemes.Length - 1];
                return;
            }
            if (leafTag.Contains("<NER2>"))
            {
                // this one an be part of the last name
                expandLastNER = true;
            }
            namedEntityTag = GetNER(leafTag);
        }

        if (namedEntityTag != null)
        {
            startOfNamedEntity = sentence.Count;
        }

        if (!alreadyAdded)
        {
            foreach (string token in ProcessLexeme(leaf.Lexeme!))
            {
                sentence.Add(token);
            }
        }

        if (namedEntityTag != null)
        {
            names.Add(new Span(startOfNamedEntity, sentence.Count, namedEntityTag));
        }

        if (expandLastNER)
        {
            // if the current leaf has the tag <NER2>, it can be the continuation of
            // a NER.
            // we check if it is true, and expand the last NER
            int lastIndex = names.Count - 1;
            if (names.Count > 0)
            {
                Span last = names[lastIndex];
                if (last.End == sentence.Count - 1)
                {
                    names[lastIndex] = new Span(last.Start, sentence.Count, last.Type);
                }
            }
        }
    }

    private IList<string> ProcessLexeme(string lexemeStr)
    {
        IList<string> @out = new JCG.List<string>();
        string[] parts = SplitDroppingTrailingEmpty(underlinePattern, lexemeStr);
        foreach (string tok in parts)
        {
            if (tok.Length > 1 && !IsWholeMatch(alphanumericPattern, tok))
            {
                foreach (string processed in ProcessTok(tok))
                {
                    @out.Add(processed);
                }
            }
            else
            {
                @out.Add(tok);
            }
        }
        return @out;
    }

    private IList<string> ProcessTok(string tok)
    {
        bool tokAdded = false;
        string original = tok;
        IList<string> @out = new JCG.List<string>();
        // NOpenNLP: upstream uses a LinkedList purely as an append-then-append-all buffer.
        JCG.List<string> suffix = new JCG.List<string>();
        char first = tok[0];
        if (first == '«')
        {
            @out.Add(first.ToString());
            tok = tok.Substring(1);
        }
        char last = tok[tok.Length - 1];
        if (last == '»' || last == ':' || last == ',' || last == '!')
        {
            suffix.Add(last.ToString());
            tok = tok.Substring(0, tok.Length - 1);
        }

        // lets split all hyphens
        if (splitHyphenatedTokens && tok.Contains("-") && tok.Length > 1)
        {
            // NOpenNLP: Java's Matcher.matches() anchors the whole input; hyphenPattern has no
            // outer anchors of its own, so the match is checked against the full input length.
            Match matcher = MatchWholeString(hyphenPattern, tok);

            string? firstTok = null;
            string hyphen = "-";
            string? secondTok = null;
            string? rest = null;

            if (matcher.Success)
            {
                // NOpenNLP: upstream tests group(1)/group(3)/group(6) for null to find which of the
                // three alternatives fired. .NET returns "" with Success == false for a group that
                // did not participate, so Success is the correct test -- an empty-string check
                // would misread a group that matched empty.
                if (matcher.Groups[1].Success)
                {
                    firstTok = matcher.Groups[2].Value;
                }
                else if (matcher.Groups[3].Success)
                {
                    secondTok = matcher.Groups[4].Value;
                    rest = matcher.Groups[5].Value;
                }
                else if (matcher.Groups[6].Success)
                {
                    firstTok = matcher.Groups[7].Value;
                    secondTok = matcher.Groups[8].Value;
                    rest = matcher.Groups[9].Value;
                }

                AddIfNotEmpty(firstTok, @out);
                AddIfNotEmpty(hyphen, @out);
                AddIfNotEmpty(secondTok, @out);
                AddIfNotEmpty(rest, @out);
                tokAdded = true;
            }
        }
        if (!tokAdded)
        {
            if (!original.Equals(tok, StringComparison.Ordinal) && tok.Length > 1
                && !IsWholeMatch(alphanumericPattern, tok))
            {
                foreach (string processed in ProcessTok(tok))
                {
                    @out.Add(processed);
                }
            }
            else
            {
                @out.Add(tok);
            }
        }
        foreach (string s in suffix)
        {
            @out.Add(s);
        }
        return @out;
    }

    private void AddIfNotEmpty(string? firstTok, IList<string> @out)
    {
        if (firstTok != null && firstTok.Length > 0)
        {
            foreach (string processed in ProcessTok(firstTok))
            {
                @out.Add(processed);
            }
        }
    }

    /// <summary>
    /// Parse a NER tag in Arvores Deitadas format.
    /// </summary>
    /// <param name="tags">the NER tag in Arvores Deitadas format</param>
    /// <returns>the NER tag, or null if not a NER tag in Arvores Deitadas format</returns>
    private static string? GetNER(string tags)
    {
        if (tags.Contains("<NER2>"))
        {
            return null;
        }
        string[] tag = SplitDroppingTrailingEmpty(whitespacePattern, tags);
        foreach (string t in tag)
        {
            // NOpenNLP: Java's Matcher.matches() anchors the whole input; tagPattern is unanchored,
            // so an unanchored .NET IsMatch here would accept tags with trailing junk that upstream
            // rejects.
            Match matcher = MatchWholeString(tagPattern, t);
            if (matcher.Success)
            {
                string ner = matcher.Groups[2].Value;
                // NOpenNLP: upstream pairs containsKey with get; TryGetValue does both in one lookup.
                if (Harem.TryGetValue(ner, out string? haremType))
                {
                    return haremType;
                }
            }
        }
        return null;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    /// <exception cref="NotSupportedException">if the underlying stream does not support resetting</exception>
    public override void Reset() => adSentenceStream.Reset();

    protected override void Dispose(bool disposing) => adSentenceStream.Dispose();

    // NOpenNLP: upstream names this enum Type; C# casing makes it CorpusType, which also avoids
    // colliding with System.Type.
    internal enum CorpusType
    {
        ama,
        cie,
        lit
    }

    private CorpusType? corpusType = null;

    private Regex? metaPattern;

    // works for Amazonia
    //  private static final Pattern meta1 = Pattern
    //      .compile("^(?:[a-zA-Z\\-]*(\\d+)).*?p=(\\d+).*");
    //
    //  // works for selva cie
    //  private static final Pattern meta2 = Pattern
    //    .compile("^(?:[a-zA-Z\\-]*(\\d+)).*?p=(\\d+).*");

    private int textIdMeta2 = -1;
    private string textMeta2 = "";

    private int GetTextID(ADSentenceStream.Sentence paragraph)
    {
        string meta = paragraph.Metadata!;

        if (corpusType == null)
        {
            if (meta.StartsWith("LIT", StringComparison.Ordinal))
            {
                corpusType = CorpusType.lit;
                metaPattern = new Regex("^([a-zA-Z\\-]+)(\\d+).*?p=(\\d+).*");
            }
            else if (meta.StartsWith("CIE", StringComparison.Ordinal))
            {
                corpusType = CorpusType.cie;
                metaPattern = new Regex("^.*?source=\"(.*?)\".*");
            }
            else
            { // ama
                corpusType = CorpusType.ama;
                metaPattern = new Regex("^(?:[a-zA-Z\\-]*(\\d+)).*?p=(\\d+).*");
            }
        }

        if (corpusType == CorpusType.lit)
        {
            Match m2 = MatchWholeString(metaPattern!, meta);
            if (m2.Success)
            {
                string textId = m2.Groups[1].Value;
                if (!textId.Equals(textMeta2, StringComparison.Ordinal))
                {
                    textIdMeta2++;
                    textMeta2 = textId;
                }
                return textIdMeta2;
            }
            else
            {
                throw new InvalidOperationException("Invalid metadata: " + meta);
            }
        }
        else if (corpusType == CorpusType.cie)
        {
            Match m2 = MatchWholeString(metaPattern!, meta);
            if (m2.Success)
            {
                string textId = m2.Groups[1].Value;
                if (!textId.Equals(textMeta2, StringComparison.Ordinal))
                {
                    textIdMeta2++;
                    textMeta2 = textId;
                }
                return textIdMeta2;
            }
            else
            {
                throw new InvalidOperationException("Invalid metadata: " + meta);
            }
        }
        else if (corpusType == CorpusType.ama)
        {
            Match m2 = MatchWholeString(metaPattern!, meta);
            if (m2.Success)
            {
                // NOpenNLP: parsing must be culture-invariant; a bare int.Parse is culture-sensitive.
                return int.Parse(m2.Groups[1].Value, CultureInfo.InvariantCulture);
                // currentPara = Integer.parseInt(m.group(2));
            }
            else
            {
                throw new InvalidOperationException("Invalid metadata: " + meta);
            }
        }

        return 0;
    }

    // NOpenNLP-specific: Java's Matcher.matches() requires the entire input to match, while .NET's
    // Regex.Match finds a match anywhere. Anchoring to the full input length reproduces matches()
    // without rewriting each pattern.
    private static Match MatchWholeString(Regex regex, string input)
    {
        Match match = regex.Match(input);
        if (match.Success && match.Index == 0 && match.Length == input.Length)
        {
            return match;
        }
        return Match.Empty;
    }

    private static bool IsWholeMatch(Regex regex, string input) => MatchWholeString(regex, input).Success;

    // NOpenNLP-specific: reproduces Java's String.split(regex)/Pattern.split trailing-empty-string
    // behavior, which .NET's Regex.Split does not share. Java drops trailing empty strings but
    // keeps interior ones. This matters because callers read parts[parts.Length - 1].
    private static string[] SplitDroppingTrailingEmpty(Regex separator, string value)
    {
        string[] parts = separator.Split(value);

        int length = parts.Length;
        while (length > 0 && parts[length - 1].Length == 0)
        {
            length--;
        }

        if (length == parts.Length)
        {
            return parts;
        }

        string[] trimmed = new string[length];
        Array.Copy(parts, trimmed, length);
        return trimmed;
    }
}
