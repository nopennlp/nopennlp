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
package org.nopennlp.compat;

import opennlp.tools.chunker.ChunkSample;
import opennlp.tools.chunker.ChunkSampleStream;
import opennlp.tools.chunker.ChunkerFactory;
import opennlp.tools.chunker.ChunkerME;
import opennlp.tools.chunker.ChunkerModel;
import opennlp.tools.namefind.NameFinderME;
import opennlp.tools.namefind.NameSampleDataStream;
import opennlp.tools.namefind.TokenNameFinderFactory;
import opennlp.tools.namefind.TokenNameFinderModel;
import opennlp.tools.postag.POSModel;
import opennlp.tools.postag.POSTaggerFactory;
import opennlp.tools.postag.POSTaggerME;
import opennlp.tools.postag.WordTagSampleStream;
import opennlp.tools.sentdetect.SentenceDetectorFactory;
import opennlp.tools.sentdetect.SentenceDetectorME;
import opennlp.tools.sentdetect.SentenceModel;
import opennlp.tools.sentdetect.SentenceSampleStream;
import opennlp.tools.tokenize.TokenSampleStream;
import opennlp.tools.tokenize.TokenizerFactory;
import opennlp.tools.tokenize.TokenizerME;
import opennlp.tools.tokenize.TokenizerModel;
import opennlp.tools.util.MarkableFileInputStreamFactory;
import opennlp.tools.util.ObjectStream;
import opennlp.tools.util.PlainTextByLineStream;
import opennlp.tools.util.Span;
import opennlp.tools.util.TrainingParameters;

import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Path;

/**
 * Trains the harness models with Apache OpenNLP, and runs the shared inference
 * inputs through a set of models regardless of which runtime produced them.
 *
 * <p>The mirror of {@code CompatModels.cs}. The training half is used only by
 * {@link TrainModels}; the inference half runs against both Java-trained and
 * NOpenNLP-trained models, which is what makes the comparison meaningful:
 * exactly the same code reads both, so a difference in the recorded output is a
 * difference in the models, not in how they were exercised.
 */
public final class CompatModels {

    private CompatModels() {
    }

    // ------------------------------------------------------------------
    // Training
    // ------------------------------------------------------------------

    /** Maxent parameters, shared by every tool except the name finder. */
    private static TrainingParameters maxentParameters() {
        TrainingParameters parameters = new TrainingParameters();
        parameters.put(TrainingParameters.ITERATIONS_PARAM, CompatCorpus.ITERATIONS);
        parameters.put(TrainingParameters.CUTOFF_PARAM, CompatCorpus.CUTOFF);
        return parameters;
    }

    private static ObjectStream<String> lines(String corpus) throws IOException {
        return new PlainTextByLineStream(
            new MarkableFileInputStreamFactory(CompatCorpus.corpus(corpus)), StandardCharsets.UTF_8);
    }

    /** Trains all five models and writes them into {@code outputDirectory}. */
    public static void trainAll(Path outputDirectory) throws IOException {
        trainSentenceDetector(outputDirectory.resolve(CompatCorpus.SENTENCE_MODEL).toFile());
        trainTokenizer(outputDirectory.resolve(CompatCorpus.TOKENIZER_MODEL).toFile());
        trainNameFinder(outputDirectory.resolve(CompatCorpus.NAME_FINDER_MODEL).toFile());
        trainPosTagger(outputDirectory.resolve(CompatCorpus.POS_TAGGER_MODEL).toFile());
        trainChunker(outputDirectory.resolve(CompatCorpus.CHUNKER_MODEL).toFile());
    }

    private static void trainSentenceDetector(File target) throws IOException {
        try (ObjectStream<String> lines = lines(CompatCorpus.SENTENCE_CORPUS)) {
            SentenceModel model = SentenceDetectorME.train(
                CompatCorpus.LANGUAGE,
                new SentenceSampleStream(lines),
                new SentenceDetectorFactory(CompatCorpus.LANGUAGE, true, null, null),
                maxentParameters());
            model.serialize(target);
        }
        System.out.println("  " + target.getName());
    }

    private static void trainTokenizer(File target) throws IOException {
        try (ObjectStream<String> lines = lines(CompatCorpus.TOKENIZER_CORPUS)) {
            TokenizerModel model = TokenizerME.train(
                new TokenSampleStream(lines),
                // No abbreviation dictionary and no alphanumeric optimization, so
                // the model alone decides every split and nothing is short-circuited
                // by a lookup table that would have to be identical on both sides.
                TokenizerFactory.create(null, CompatCorpus.LANGUAGE, null, false, null),
                maxentParameters());
            model.serialize(target);
        }
        System.out.println("  " + target.getName());
    }

    private static void trainNameFinder(File target) throws IOException {
        TrainingParameters parameters = new TrainingParameters();
        parameters.put(TrainingParameters.ITERATIONS_PARAM, CompatCorpus.NAME_FINDER_ITERATIONS);
        parameters.put(TrainingParameters.CUTOFF_PARAM, CompatCorpus.NAME_FINDER_CUTOFF);

        try (ObjectStream<String> lines = lines(CompatCorpus.NAME_FINDER_CORPUS)) {
            TokenNameFinderModel model = NameFinderME.train(
                CompatCorpus.LANGUAGE, "default",
                new NameSampleDataStream(lines),
                parameters,
                TokenNameFinderFactory.create(null, null, java.util.Collections.emptyMap(),
                    new opennlp.tools.namefind.BioCodec()));
            model.serialize(target);
        }
        System.out.println("  " + target.getName());
    }

    private static void trainPosTagger(File target) throws IOException {
        try (ObjectStream<String> lines = lines(CompatCorpus.POS_TAGGER_CORPUS)) {
            POSModel model = POSTaggerME.train(
                CompatCorpus.LANGUAGE,
                new WordTagSampleStream(lines),
                maxentParameters(),
                new POSTaggerFactory());
            model.serialize(target);
        }
        System.out.println("  " + target.getName());
    }

    private static void trainChunker(File target) throws IOException {
        try (ObjectStream<String> lines = lines(CompatCorpus.CHUNKER_CORPUS)) {
            ChunkerModel model = ChunkerME.train(
                CompatCorpus.LANGUAGE,
                new ChunkSampleStream(lines),
                maxentParameters(),
                new ChunkerFactory());
            model.serialize(target);
        }
        System.out.println("  " + target.getName());
    }

    // ------------------------------------------------------------------
    // Inference
    // ------------------------------------------------------------------

    /**
     * Loads every model in {@code modelDirectory}, runs the shared inputs
     * through it, and records what came back.
     *
     * <p>Each tool contributes both its discrete output (the sentences, tokens,
     * tags, spans it produced) and the probabilities behind it. The discrete
     * output is what a caller sees; the probabilities are what actually proves
     * the model was read correctly, because a model whose parameters were
     * misread will frequently still land on the same argmax for easy cases while
     * scoring it differently.
     */
    public static CompatResults analyze(Path modelDirectory) throws IOException {
        CompatResults results = new CompatResults();

        sentenceDetector(modelDirectory, results);
        tokenizer(modelDirectory, results);
        nameFinder(modelDirectory, results);
        posTagger(modelDirectory, results);
        chunker(modelDirectory, results);

        return results;
    }

    private static void sentenceDetector(Path directory, CompatResults results) throws IOException {
        SentenceModel model = new SentenceModel(
            directory.resolve(CompatCorpus.SENTENCE_MODEL).toFile());
        SentenceDetectorME detector = new SentenceDetectorME(model);

        String[] sentences = detector.sentDetect(CompatCorpus.SENTENCE_INPUT);
        results.put("sentence.language", model.getLanguage());
        results.put("sentence.count", Integer.toString(sentences.length));
        results.put("sentence.text", sentences);
        results.put("sentence.probabilities", detector.getSentenceProbabilities());
        results.put("sentence.spans", spans(detector.sentPosDetect(CompatCorpus.SENTENCE_INPUT)));
    }

    private static void tokenizer(Path directory, CompatResults results) throws IOException {
        TokenizerModel model = new TokenizerModel(
            directory.resolve(CompatCorpus.TOKENIZER_MODEL).toFile());
        TokenizerME tokenizer = new TokenizerME(model);

        String[] tokens = tokenizer.tokenize(CompatCorpus.TOKENIZER_INPUT);
        results.put("token.language", model.getLanguage());
        results.put("token.count", Integer.toString(tokens.length));
        results.put("token.text", tokens);
        results.put("token.probabilities", tokenizer.getTokenProbabilities());
        results.put("token.spans", spans(tokenizer.tokenizePos(CompatCorpus.TOKENIZER_INPUT)));
    }

    private static void nameFinder(Path directory, CompatResults results) throws IOException {
        TokenNameFinderModel model = new TokenNameFinderModel(
            directory.resolve(CompatCorpus.NAME_FINDER_MODEL).toFile());
        NameFinderME finder = new NameFinderME(model);

        Span[] names = finder.find(CompatCorpus.NAME_INPUT);
        results.put("ner.language", model.getLanguage());
        results.put("ner.count", Integer.toString(names.length));
        results.put("ner.spans", spans(names));
        results.put("ner.probabilities", finder.probs());

        // A second call without clearAdaptiveData(), so the adaptive feature
        // generators carry state across documents exactly as they would in a
        // caller that forgot to clear. That state is part of the runtime
        // behaviour a compatible model has to reproduce.
        Span[] repeat = finder.find(CompatCorpus.TOKEN_INPUT);
        results.put("ner.adaptive.count", Integer.toString(repeat.length));
        results.put("ner.adaptive.spans", spans(repeat));
    }

    private static void posTagger(Path directory, CompatResults results) throws IOException {
        POSModel model = new POSModel(directory.resolve(CompatCorpus.POS_TAGGER_MODEL).toFile());
        POSTaggerME tagger = new POSTaggerME(model);

        String[] tags = tagger.tag(CompatCorpus.TOKEN_INPUT);
        results.put("pos.language", model.getLanguage());
        results.put("pos.tags", tags);
        results.put("pos.probabilities", tagger.probs());

        // The n-best sequences exercise the beam search rather than just the
        // single best path through it, which is where a subtly misread model
        // shows up first.
        String[][] best = tagger.tag(3, CompatCorpus.TOKEN_INPUT);
        results.put("pos.nbest.count", Integer.toString(best.length));
        for (int i = 0; i < best.length; i++) {
            results.put("pos.nbest." + i, best[i]);
        }
    }

    private static void chunker(Path directory, CompatResults results) throws IOException {
        ChunkerModel model = new ChunkerModel(directory.resolve(CompatCorpus.CHUNKER_MODEL).toFile());
        ChunkerME chunker = new ChunkerME(model);

        // The chunker needs POS tags. They come from the harness's own POS model
        // rather than being hard-coded, so the chunker is fed whatever the
        // runtime under test actually produced. If the POS models disagree this
        // has already been caught above; feeding the chunker the real tags keeps
        // the two tools chained the way a caller would chain them.
        POSModel posModel = new POSModel(directory.resolve(CompatCorpus.POS_TAGGER_MODEL).toFile());
        String[] tags = new POSTaggerME(posModel).tag(CompatCorpus.TOKEN_INPUT);

        String[] chunks = chunker.chunk(CompatCorpus.TOKEN_INPUT, tags);
        results.put("chunk.language", model.getLanguage());
        results.put("chunk.tags", chunks);
        results.put("chunk.probabilities", chunker.probs());
        results.put("chunk.spans",
            spans(ChunkSample.phrasesAsSpanList(CompatCorpus.TOKEN_INPUT, tags, chunks)));
    }

    /** Renders spans as {@code start-end:type}, the same on both sides. */
    private static String[] spans(Span[] spans) {
        String[] result = new String[spans.length];
        for (int i = 0; i < spans.length; i++) {
            Span span = spans[i];
            result[i] = span.getStart() + "-" + span.getEnd() + ":"
                + (span.getType() == null ? "" : span.getType());
        }
        return result;
    }
}
