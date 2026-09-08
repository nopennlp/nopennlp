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
using NOpenNLP.Tools.Cmdline.Chunker;
using NOpenNLP.Tools.Cmdline.Dictionary;
using NOpenNLP.Tools.Cmdline.Doccat;
using NOpenNLP.Tools.Cmdline.Entitylinker;
using NOpenNLP.Tools.Cmdline.Langdetect;
using NOpenNLP.Tools.Cmdline.Languagemodel;
using NOpenNLP.Tools.Cmdline.Lemmatizer;
using NOpenNLP.Tools.Cmdline.Namefind;
using NOpenNLP.Tools.Cmdline.Parser;
using NOpenNLP.Tools.Cmdline.Postag;
using NOpenNLP.Tools.Cmdline.Sentdetect;
using NOpenNLP.Tools.Cmdline.Tokenizer;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Registers the tools the CLI exposes.
/// </summary>
// NOpenNLP: upstream builds this list inline in CLI's static initializer. It is split out
// so the order -- which is the order the usage listing prints -- reads as one block, and
// so tests can assert on it without going through CLI. The order below is upstream's
// CLI.java registration order and is user-visible; keep it.
internal static class ToolRegistry
{
    internal static void AddTools(IList<CmdLineTool> tools)
    {
        // Document Categorizer
        tools.Add(new DoccatTool());
        tools.Add(new DoccatTrainerTool());
        tools.Add(new DoccatEvaluatorTool());
        tools.Add(new DoccatCrossValidatorTool());
        tools.Add(new DoccatConverterTool());

        // Language Detector
        tools.Add(new LanguageDetectorTool());
        tools.Add(new LanguageDetectorTrainerTool());
        tools.Add(new LanguageDetectorConverterTool());
        tools.Add(new LanguageDetectorCrossValidatorTool());
        tools.Add(new LanguageDetectorEvaluatorTool());

        // Dictionary Builder
        tools.Add(new DictionaryBuilderTool());

        // Tokenizer
        tools.Add(new SimpleTokenizerTool());
        tools.Add(new TokenizerMETool());
        tools.Add(new TokenizerTrainerTool());
        tools.Add(new TokenizerMEEvaluatorTool());
        tools.Add(new TokenizerCrossValidatorTool());
        tools.Add(new TokenizerConverterTool());
        tools.Add(new DictionaryDetokenizerTool());

        // Sentence detector
        tools.Add(new SentenceDetectorTool());
        tools.Add(new SentenceDetectorTrainerTool());
        tools.Add(new SentenceDetectorEvaluatorTool());
        tools.Add(new SentenceDetectorCrossValidatorTool());
        tools.Add(new SentenceDetectorConverterTool());

        // Name Finder
        tools.Add(new TokenNameFinderTool());
        tools.Add(new TokenNameFinderTrainerTool());
        tools.Add(new TokenNameFinderEvaluatorTool());
        tools.Add(new TokenNameFinderCrossValidatorTool());
        tools.Add(new TokenNameFinderConverterTool());
        tools.Add(new CensusDictionaryCreatorTool());

        // POS Tagger
        tools.Add(new POSTaggerTool());
        tools.Add(new POSTaggerTrainerTool());
        tools.Add(new POSTaggerEvaluatorTool());
        tools.Add(new POSTaggerCrossValidatorTool());
        tools.Add(new POSTaggerConverterTool());

        // Lemmatizer
        tools.Add(new LemmatizerMETool());
        tools.Add(new LemmatizerTrainerTool());
        tools.Add(new LemmatizerEvaluatorTool());

        // Chunker
        tools.Add(new ChunkerMETool());
        tools.Add(new ChunkerTrainerTool());
        tools.Add(new ChunkerEvaluatorTool());
        tools.Add(new ChunkerCrossValidatorTool());
        tools.Add(new ChunkerConverterTool());

        // Parser
        tools.Add(new ParserTool());
        tools.Add(new ParserTrainerTool());
        tools.Add(new ParserEvaluatorTool());
        tools.Add(new ParserConverterTool());
        tools.Add(new BuildModelUpdaterTool());
        tools.Add(new CheckModelUpdaterTool());
        tools.Add(new TaggerModelReplacerTool());

        // Entity Linker
        tools.Add(new EntityLinkerTool());

        // Language Model
        tools.Add(new NGramLanguageModelTool());
    }
}
