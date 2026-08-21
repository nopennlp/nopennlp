#!/usr/bin/env bash
#
# Compares the ported `nopennlp` CLI against the real Apache OpenNLP CLI by running the
# same invocation through both and diffing stdout and the exit code.
#
# This is a developer tool, not part of CI: it needs a JVM, the OpenNLP jar, a clone of
# the upstream source for its test corpora, and the downloaded SF models. Its value is at
# rebase time -- after pulling a new upstream release, run it to see what changed in the
# CLI's observable behaviour.
#
# Usage:
#   build/download-test-models.ps1                       # once, for the SF models
#   dotnet pack src/NOpenNLP.Cli/NOpenNLP.Cli.csproj -c Release
#   dotnet tool install --tool-path /tmp/nopennlp-tool \
#       --add-source src/NOpenNLP.Cli/bin/Release NOpenNLP.Cli --version <version>
#   build/regress.sh                                     # VERBOSE=1 to print each diff
#
# Override any of the paths below by exporting them first.
#
# Differences that are expected, and are normalized away rather than reported:
#   - the command and product name ("opennlp"/"OpenNLP" vs "nopennlp"/"NOpenNLP")
#   - timings ("Execution time: 0.193 seconds", "done (0.020s)", "Runtime: 1.2s")
#   - absolute paths, which differ per run because each side uses its own temp dir
#   - throughput lines, which depend on machine speed
#   - the order of a tool's options and of a format list. Java derives both from
#     reflection -- Class.getMethods() and HashMap iteration -- neither of which the JDK
#     specifies; the port uses declaration and registration order. See canon.py.
#
# Everything else is compared verbatim. Two classes of difference are known to remain and
# are reported as failures on purpose, so that a real regression is not hidden behind a
# blanket exemption:
#   - training logs differ in the last one or two digits of a loglikelihood, from
#     floating-point summation order. The per-iteration ACCURACY is bit-identical, which
#     is the number that matters; see the PR for the measurements.
#   - converter help lists formats in a different order (same set).

set -uo pipefail

JAR=${JAR:-$HOME/.m2/repository/org/apache/opennlp/opennlp-tools/1.9.4/opennlp-tools-1.9.4.jar}
NOPENNLP=${NOPENNLP:-/tmp/nopennlp-tool/nopennlp}
RES=${RES:-$HOME/git/opennlp/opennlp-tools/src/test/resources/opennlp/tools}
MODELS=${MODELS:-$(cd "$(dirname "$0")/.." && pwd)/testdata/models-sf}

for required in "$JAR" "$NOPENNLP" "$RES" "$MODELS"; do
  if [ ! -e "$required" ]; then
    echo "error: not found: $required" >&2
    echo "see the usage notes at the top of this script" >&2
    exit 2
  fi
done
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

PASS=0; FAIL=0; SKIP=0
declare -a FAILED_CASES=()

canonicalize() {
  # See canon.py: sorts the option list, the format alternation and the arguments
  # description, all of which Java derives from reflection orders the JDK does not specify.
  python3 "$(dirname "$0")/canon.py"
}

normalize() {
  sed -E \
    -e 's/NOpenNLP/OpenNLP/g' \
    -e 's/nopennlp/opennlp/g' \
    -e 's/Execution time: [0-9.]+ seconds/Execution time: T/' \
    -e 's/done \([0-9.]+s\)/done (Ts)/' \
    -e 's/Runtime: [0-9.]+s/Runtime: Ts/' \
    -e 's/Average: [0-9.]+ /Average: N /' \
    -e 's/current: [0-9.]+ .*$/current: N/' \
    -e 's#/[^ ]*/(java|cs)-[a-z0-9]+#PATH#g' \
    -e 's#/var/folders/[^ ]*#PATH#g' \
    -e 's#/tmp/[^ ]*#PATH#g' \
    -e 's#/private/var/folders/[^ ]*#PATH#g' \
    -e 's/[0-9]+\.[0-9]+ (sent|doc|token)s?\/s/N \1s\/s/g'
}

# run_case <name> <stdin-file-or-empty> <args...>
run_case() {
  local name="$1"; shift
  local stdin_file="$1"; shift

  local jout="$WORK/j.out" jerr="$WORK/j.err" cout="$WORK/c.out" cerr="$WORK/c.err"

  if [ -n "$stdin_file" ]; then
    java -cp "$JAR" opennlp.tools.cmdline.CLI "$@" < "$stdin_file" > "$jout" 2> "$jerr"; local jrc=$?
    "$NOPENNLP" "$@" < "$stdin_file" > "$cout" 2> "$cerr"; local crc=$?
  else
    java -cp "$JAR" opennlp.tools.cmdline.CLI "$@" < /dev/null > "$jout" 2> "$jerr"; local jrc=$?
    "$NOPENNLP" "$@" < /dev/null > "$cout" 2> "$cerr"; local crc=$?
  fi

  local ok=1 detail=""

  if [ "$jrc" != "$crc" ]; then
    ok=0; detail="exit code: java=$jrc cs=$crc"
  fi

  if ! diff -q <(normalize < "$jout" | canonicalize) <(normalize < "$cout" | canonicalize) >/dev/null; then
    ok=0; detail="$detail stdout differs"
  fi

  if [ $ok -eq 1 ]; then
    PASS=$((PASS+1)); printf '  PASS  %s\n' "$name"
  else
    FAIL=$((FAIL+1)); FAILED_CASES+=("$name")
    printf '  FAIL  %s  (%s)\n' "$name" "$detail"
    if [ -n "${VERBOSE:-}" ]; then
      echo "    --- stdout diff (java | cs) ---"
      diff <(normalize < "$jout" | canonicalize) <(normalize < "$cout" | canonicalize) | head -20 | sed 's/^/    /'
    fi
  fi
}

echo "== usage and help =="
run_case "usage (no args)" ""
for tool in Doccat DoccatTrainer DoccatEvaluator DoccatCrossValidator DoccatConverter \
            LanguageDetector LanguageDetectorTrainer LanguageDetectorConverter \
            LanguageDetectorCrossValidator LanguageDetectorEvaluator \
            DictionaryBuilder SimpleTokenizer TokenizerME TokenizerTrainer \
            TokenizerMEEvaluator TokenizerCrossValidator TokenizerConverter \
            DictionaryDetokenizer SentenceDetector SentenceDetectorTrainer \
            SentenceDetectorEvaluator SentenceDetectorCrossValidator SentenceDetectorConverter \
            TokenNameFinder TokenNameFinderTrainer TokenNameFinderEvaluator \
            TokenNameFinderCrossValidator TokenNameFinderConverter CensusDictionaryCreator \
            POSTagger POSTaggerTrainer POSTaggerEvaluator POSTaggerCrossValidator \
            POSTaggerConverter LemmatizerME LemmatizerTrainerME LemmatizerEvaluator \
            ChunkerME ChunkerTrainerME ChunkerEvaluator ChunkerCrossValidator ChunkerConverter \
            Parser ParserTrainer ParserEvaluator ParserConverter \
            BuildModelUpdater CheckModelUpdater TaggerModelReplacer \
            EntityLinker NGramLanguageModel; do
  run_case "$tool help" "" "$tool" help
done

echo
echo "== error paths =="
run_case "unknown tool" "" NoSuchTool
run_case "unknown format" "" TokenizerTrainer.nosuchformat -model m.bin
run_case "format on a basic tool" "" SimpleTokenizer.conllu x

echo
echo "== inference over the official models =="
printf 'Mr. Smith went to Washington. He arrived on Jan. 3rd.\n' > "$WORK/sents.txt"
run_case "SimpleTokenizer" "$WORK/sents.txt" SimpleTokenizer
run_case "TokenizerME"     "$WORK/sents.txt" TokenizerME "$MODELS/en-token.bin"
run_case "SentenceDetector" "$WORK/sents.txt" SentenceDetector "$MODELS/en-sent.bin"

printf 'Mr. Smith went to Washington .\n' > "$WORK/tokens.txt"
run_case "POSTagger maxent"     "$WORK/tokens.txt" POSTagger "$MODELS/en-pos-maxent.bin"
run_case "POSTagger perceptron" "$WORK/tokens.txt" POSTagger "$MODELS/en-pos-perceptron.bin"
run_case "TokenNameFinder person" "$WORK/tokens.txt" TokenNameFinder "$MODELS/en-ner-person.bin"
run_case "TokenNameFinder multi"  "$WORK/tokens.txt" TokenNameFinder \
    "$MODELS/en-ner-person.bin" "$MODELS/en-ner-location.bin"

printf 'Mr._NNP Smith_NNP went_VBD to_TO Washington_NNP ._.\n' > "$WORK/postokens.txt"
run_case "ChunkerME" "$WORK/postokens.txt" ChunkerME "$MODELS/en-chunker.bin"

echo
echo "== converters =="
run_case "POSTaggerConverter conllu" "" POSTaggerConverter conllu \
    -data "$RES/formats/conllu/de-ud-train-sample.conllu" -encoding UTF-8
run_case "TokenizerConverter conllu" "" TokenizerConverter conllu \
    -data "$RES/formats/conllu/de-ud-train-sample.conllu" -encoding UTF-8
run_case "SentenceDetectorConverter conllu" "" SentenceDetectorConverter conllu \
    -data "$RES/formats/conllu/de-ud-train-sample.conllu" -encoding UTF-8
run_case "TokenNameFinderConverter conll02" "" TokenNameFinderConverter conll02 \
    -data "$RES/formats/conll2002-nl.sample" -lang nld -types per,loc,org,misc -encoding UTF-8
run_case "TokenNameFinderConverter ad" "" TokenNameFinderConverter ad \
    -data "$RES/formats/ad.sample" -lang por -encoding UTF-8
run_case "ChunkerConverter ad" "" ChunkerConverter ad \
    -data "$RES/formats/ad.sample" -lang por -encoding UTF-8
run_case "POSTaggerConverter ad" "" POSTaggerConverter ad \
    -data "$RES/formats/ad.sample" -lang por -encoding UTF-8
run_case "converter help (no format)" "" POSTaggerConverter
run_case "converter format help" "" POSTaggerConverter conllu help

echo
echo "== training =="
cp "$RES/tokenize/token.train" "$WORK/"
run_case "TokenizerTrainer" "" TokenizerTrainer \
    -model "$WORK/tok.bin" -lang eng -data "$WORK/token.train" -encoding UTF-8
cp "$RES/sentdetect/Sentences.txt" "$WORK/"
run_case "SentenceDetectorTrainer" "" SentenceDetectorTrainer \
    -model "$WORK/sent.bin" -lang eng -data "$WORK/Sentences.txt" -encoding UTF-8
cp "$RES/namefind/AnnotatedSentences.txt" "$WORK/"
run_case "TokenNameFinderTrainer" "" TokenNameFinderTrainer \
    -model "$WORK/ner.bin" -lang eng -data "$WORK/AnnotatedSentences.txt" -encoding ISO-8859-1
cp "$RES/postag/AnnotatedSentences.txt" "$WORK/pos.train"
run_case "POSTaggerTrainer" "" POSTaggerTrainer \
    -model "$WORK/pos.bin" -lang eng -data "$WORK/pos.train" -encoding UTF-8
cp "$RES/chunker/test.txt" "$WORK/"
run_case "ChunkerTrainerME" "" ChunkerTrainerME \
    -model "$WORK/chunk.bin" -lang eng -data "$WORK/test.txt" -encoding UTF-8

echo
echo "== evaluation =="
run_case "TokenizerMEEvaluator" "" TokenizerMEEvaluator \
    -model "$MODELS/en-token.bin" -data "$WORK/token.train" -encoding UTF-8
run_case "SentenceDetectorEvaluator" "" SentenceDetectorEvaluator \
    -model "$MODELS/en-sent.bin" -data "$WORK/Sentences.txt" -encoding UTF-8
run_case "POSTaggerEvaluator" "" POSTaggerEvaluator \
    -model "$MODELS/en-pos-maxent.bin" -data "$WORK/pos.train" -encoding UTF-8
run_case "ChunkerEvaluator" "" ChunkerEvaluator \
    -model "$MODELS/en-chunker.bin" -data "$WORK/test.txt" -encoding UTF-8
run_case "TokenNameFinderEvaluator" "" TokenNameFinderEvaluator \
    -model "$MODELS/en-ner-person.bin" -data "$WORK/AnnotatedSentences.txt" -encoding ISO-8859-1

echo
echo "== cross validation =="
run_case "TokenizerCrossValidator" "" TokenizerCrossValidator \
    -lang eng -data "$WORK/token.train" -encoding UTF-8 -folds 2
run_case "SentenceDetectorCrossValidator" "" SentenceDetectorCrossValidator \
    -lang eng -data "$WORK/Sentences.txt" -encoding UTF-8 -folds 2
run_case "POSTaggerCrossValidator" "" POSTaggerCrossValidator \
    -lang eng -data "$WORK/pos.train" -encoding UTF-8 -folds 2
run_case "ChunkerCrossValidator" "" ChunkerCrossValidator \
    -lang eng -data "$WORK/test.txt" -encoding UTF-8 -folds 2

echo
echo "== dictionary tools =="
printf 'foo\nbar\nbaz\n' > "$WORK/dict.txt"
run_case "DictionaryBuilder" "" DictionaryBuilder \
    -inputFile "$WORK/dict.txt" -outputFile "$WORK/dict.xml" -encoding UTF-8

echo
echo "======================================"
printf 'PASS: %d   FAIL: %d   SKIP: %d\n' "$PASS" "$FAIL" "$SKIP"
if [ ${#FAILED_CASES[@]} -gt 0 ]; then
  echo "failing cases:"
  printf '  - %s\n' "${FAILED_CASES[@]}"
fi
