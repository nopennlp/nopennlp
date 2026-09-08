/*
 * Copyright 2026 NOpenNLP Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Compat;

/// <summary>
/// Trains the harness models with NOpenNLP, and runs the shared inference inputs
/// through a set of models regardless of which runtime produced them.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; the mirror of <c>CompatModels.java</c>. The training
/// half is used only by the writer test; the inference half runs against both
/// NOpenNLP-trained and Java-trained models, which is what makes the comparison
/// meaningful: exactly the same code reads both, so a difference in the recorded
/// output is a difference in the models, not in how they were exercised.
/// </remarks>
internal static class CompatModels
{
    // ------------------------------------------------------------------
    // Training
    // ------------------------------------------------------------------

    /// <summary>Maxent parameters, shared by every tool except the name finder.</summary>
    private static TrainingParameters MaxentParameters()
    {
        var parameters = new TrainingParameters();
        parameters.Put(TrainingParameters.ITERATIONS_PARAM, CompatCorpus.Iterations);
        parameters.Put(TrainingParameters.CUTOFF_PARAM, CompatCorpus.Cutoff);
        return parameters;
    }

    private static IObjectStream<string?> Lines(string corpus) =>
        new PlainTextByLineStream(
            new MarkableFileInputStreamFactory(CompatCorpus.Corpus(corpus)), Encoding.UTF8);

    /// <summary>Trains all five models and writes them into <paramref name="outputDirectory"/>.</summary>
    public static void TrainAll(string outputDirectory)
    {
        TrainSentenceDetector(Path.Combine(outputDirectory, CompatCorpus.SentenceModelFile));
        TrainTokenizer(Path.Combine(outputDirectory, CompatCorpus.TokenizerModelFile));
        TrainNameFinder(Path.Combine(outputDirectory, CompatCorpus.NameFinderModelFile));
        TrainPosTagger(Path.Combine(outputDirectory, CompatCorpus.PosTaggerModelFile));
        TrainChunker(Path.Combine(outputDirectory, CompatCorpus.ChunkerModelFile));
    }

    private static void TrainSentenceDetector(string target)
    {
        using IObjectStream<string?> lines = Lines(CompatCorpus.SentenceCorpus);
        SentenceModel model = SentenceDetectorME.Train(
            CompatCorpus.Language,
            new SentenceSampleStream(lines),
            new SentenceDetectorFactory(CompatCorpus.Language, true, null!, null),
            MaxentParameters());
        model.Serialize(target);
    }

    private static void TrainTokenizer(string target)
    {
        using IObjectStream<string?> lines = Lines(CompatCorpus.TokenizerCorpus);
        TokenizerModel model = TokenizerME.Train(
            new TokenSampleStream(lines),
            // No abbreviation dictionary and no alphanumeric optimization, so the
            // model alone decides every split and nothing is short-circuited by a
            // lookup table that would have to be identical on both sides. The null
            // pattern is what upstream passes here; with the optimization off it is
            // never consulted, and the factory falls back to the language default
            // when the manifest is written.
            TokenizerFactory.Create(null, CompatCorpus.Language, null, false, null!)!,
            MaxentParameters());
        model.Serialize(target);
    }

    private static void TrainNameFinder(string target)
    {
        var parameters = new TrainingParameters();
        parameters.Put(TrainingParameters.ITERATIONS_PARAM, CompatCorpus.NameFinderIterations);
        parameters.Put(TrainingParameters.CUTOFF_PARAM, CompatCorpus.NameFinderCutoff);

        using IObjectStream<string?> lines = Lines(CompatCorpus.NameFinderCorpus);
        TokenNameFinderModel model = NameFinderME.Train(
            CompatCorpus.Language, "default",
            new NameSampleDataStream(lines),
            parameters,
            TokenNameFinderFactory.Create(null, null, new Dictionary<string, object>(), new BioCodec())!);
        model.Serialize(target);
    }

    private static void TrainPosTagger(string target)
    {
        using IObjectStream<string?> lines = Lines(CompatCorpus.PosTaggerCorpus);
        POSModel model = POSTaggerME.Train(
            CompatCorpus.Language,
            new WordTagSampleStream(lines),
            MaxentParameters(),
            new POSTaggerFactory());
        model.Serialize(target);
    }

    private static void TrainChunker(string target)
    {
        using IObjectStream<string?> lines = Lines(CompatCorpus.ChunkerCorpus);
        ChunkerModel model = ChunkerME.Train(
            CompatCorpus.Language,
            new ChunkSampleStream(lines),
            MaxentParameters(),
            new ChunkerFactory());
        model.Serialize(target);
    }

    // ------------------------------------------------------------------
    // Inference
    // ------------------------------------------------------------------

    /// <summary>
    /// Loads every model in <paramref name="modelDirectory"/>, runs the shared
    /// inputs through it, and records what came back.
    /// </summary>
    /// <remarks>
    /// Each tool contributes both its discrete output (the sentences, tokens,
    /// tags, spans it produced) and the probabilities behind it. The discrete
    /// output is what a caller sees; the probabilities are what actually proves
    /// the model was read correctly, because a model whose parameters were
    /// misread will frequently still land on the same argmax for easy cases while
    /// scoring it differently.
    /// </remarks>
    public static CompatResults Analyze(string modelDirectory)
    {
        var results = new CompatResults();

        SentenceDetector(modelDirectory, results);
        Tokenizer(modelDirectory, results);
        NameFinder(modelDirectory, results);
        PosTagger(modelDirectory, results);
        Chunker(modelDirectory, results);

        return results;
    }

    private static FileInfo ModelFile(string directory, string name) =>
        new FileInfo(Path.Combine(directory, name));

    private static void SentenceDetector(string directory, CompatResults results)
    {
        var model = new SentenceModel(ModelFile(directory, CompatCorpus.SentenceModelFile));
        var detector = new SentenceDetectorME(model);

        string[] sentences = detector.SentDetect(CompatCorpus.SentenceInput);
        results.Put("sentence.language", model.Language);
        results.Put("sentence.count", sentences.Length.ToString());
        results.Put("sentence.text", sentences);
        results.Put("sentence.probabilities", detector.SentenceProbabilities);
        results.Put("sentence.spans", Spans(detector.SentPosDetect(CompatCorpus.SentenceInput)));
    }

    private static void Tokenizer(string directory, CompatResults results)
    {
        var model = new TokenizerModel(ModelFile(directory, CompatCorpus.TokenizerModelFile));
        var tokenizer = new TokenizerME(model);

        string[] tokens = tokenizer.Tokenize(CompatCorpus.TokenizerInput);
        results.Put("token.language", model.Language);
        results.Put("token.count", tokens.Length.ToString());
        results.Put("token.text", tokens);
        results.Put("token.probabilities", tokenizer.TokenProbabilities);
        results.Put("token.spans", Spans(tokenizer.TokenizePos(CompatCorpus.TokenizerInput)));
    }

    private static void NameFinder(string directory, CompatResults results)
    {
        var model = new TokenNameFinderModel(ModelFile(directory, CompatCorpus.NameFinderModelFile));
        var finder = new NameFinderME(model);

        Span[] names = finder.Find(CompatCorpus.NameInput);
        results.Put("ner.language", model.Language);
        results.Put("ner.count", names.Length.ToString());
        results.Put("ner.spans", Spans(names));
        results.Put("ner.probabilities", finder.Probs());

        // A second call without ClearAdaptiveData(), so the adaptive feature
        // generators carry state across documents exactly as they would in a
        // caller that forgot to clear. That state is part of the runtime
        // behaviour a compatible model has to reproduce.
        Span[] repeat = finder.Find(CompatCorpus.TokenInput);
        results.Put("ner.adaptive.count", repeat.Length.ToString());
        results.Put("ner.adaptive.spans", Spans(repeat));
    }

    private static void PosTagger(string directory, CompatResults results)
    {
        var model = new POSModel(ModelFile(directory, CompatCorpus.PosTaggerModelFile));
        var tagger = new POSTaggerME(model);

        string[] tags = tagger.Tag(CompatCorpus.TokenInput);
        results.Put("pos.language", model.Language);
        results.Put("pos.tags", tags);
        results.Put("pos.probabilities", tagger.Probs());

        // The n-best sequences exercise the beam search rather than just the
        // single best path through it, which is where a subtly misread model
        // shows up first.
        string[][] best = tagger.Tag(3, CompatCorpus.TokenInput);
        results.Put("pos.nbest.count", best.Length.ToString());
        for (int i = 0; i < best.Length; i++)
        {
            results.Put($"pos.nbest.{i}", best[i]);
        }
    }

    private static void Chunker(string directory, CompatResults results)
    {
        var model = new ChunkerModel(ModelFile(directory, CompatCorpus.ChunkerModelFile));
        var chunker = new ChunkerME(model);

        // The chunker needs POS tags. They come from the harness's own POS model
        // rather than being hard-coded, so the chunker is fed whatever the runtime
        // under test actually produced. If the POS models disagree this has
        // already been caught above; feeding the chunker the real tags keeps the
        // two tools chained the way a caller would chain them.
        var posModel = new POSModel(ModelFile(directory, CompatCorpus.PosTaggerModelFile));
        string[] tags = new POSTaggerME(posModel).Tag(CompatCorpus.TokenInput);

        string[] chunks = chunker.Chunk(CompatCorpus.TokenInput, tags);
        results.Put("chunk.language", model.Language);
        results.Put("chunk.tags", chunks);
        results.Put("chunk.probabilities", chunker.Probs());
        results.Put("chunk.spans",
            Spans(ChunkSample.GetPhrasesAsSpanList(CompatCorpus.TokenInput, tags, chunks)));
    }

    /// <summary>Renders spans as <c>start-end:type</c>, the same on both sides.</summary>
    private static string[] Spans(Span[] spans)
    {
        var result = new string[spans.Length];
        for (int i = 0; i < spans.Length; i++)
        {
            Span span = spans[i];
            result[i] = $"{span.Start}-{span.End}:{span.Type ?? string.Empty}";
        }

        return result;
    }
}
