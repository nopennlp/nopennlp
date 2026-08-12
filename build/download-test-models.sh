#!/usr/bin/env bash
#
# Downloads the pre-trained Apache OpenNLP 1.5 models used by the integration
# tests into testdata/models-sf, verifying each against a known SHA-256.
#
# The download is idempotent: a file that already exists and matches its
# checksum is left alone, so this is cheap to re-run and safe to point at a
# restored CI cache. Nothing here is committed; testdata/ is gitignored.
#
# Usage: build/download-test-models.sh [target-directory]
#
set -euo pipefail

BASE_URL="https://opennlp.sourceforge.net/models-1.5"
TARGET_DIR="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/testdata/models-sf}"

# Model name and expected SHA-256. The parser model is deliberately absent: it
# is 34 MB, and the parser is not ported, so nothing would exercise it.
MODELS=(
  "en-sent.bin bd6adffc85d66ccffd09ad1545ab798248193672c4da5c6669150e6a3b35e5b1"
  "en-token.bin 2d0dd64ffb3d084382d7bdb65e7bd004c5001ba5503c36413d97c3e46321437c"
  "en-chunker.bin 7861a0c2f134d9c12a022a1ba501e88bc7039f6db72b4140e1bafd1fb5ef76cc"
  "en-pos-maxent.bin 645a094f45a866687a617385233fd23ae8b0f5fa8b1b76996781a50c17bdcf3d"
  "en-pos-perceptron.bin 0b49b7d9bdb9f888aed85e9f41fbcfd6cab607805ba9cd2370e1e5af4e540db8"
  "en-ner-person.bin 687a9263d96b37fced707c9f2ac0560f9edaf54658856395555901924f64dbe4"
  "en-ner-location.bin 8fe39e48633f4a86c4132d9c54b396a2d8e0460c1d71e3562dacf976984f447b"
  "en-ner-organization.bin 0136c12afe1ac357142260c39bb879b7c9d121e41024114db5a6455b4fd5ba00"
  "en-ner-date.bin 1207030923852e1c244919d8f15d9e78c217323728fcf909029abd1703967855"
  "en-ner-money.bin b80d577d7d319038457e19f814438965aee9ef5cd1f4f175418d4aece8e504b8"
  "en-ner-percentage.bin dbc57162ba9784ae7a851393584aa7193aa2eee6ce2ec962fa937c9fa5e08137"
  "en-ner-time.bin 8a815e6e6d353ee4c478f85dc19b201361e955a9820487f2cf3a2f43c9c78274"
)

sha256_of() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | awk '{print $1}'
  else
    shasum -a 256 "$1" | awk '{print $1}'
  fi
}

mkdir -p "$TARGET_DIR"

downloaded=0
cached=0

for entry in "${MODELS[@]}"; do
  name="${entry%% *}"
  expected="${entry##* }"
  path="$TARGET_DIR/$name"

  if [ -f "$path" ] && [ "$(sha256_of "$path")" = "$expected" ]; then
    cached=$((cached + 1))
    continue
  fi

  echo "Downloading $name"
  # --fail so an HTML error page is never mistaken for a model.
  curl --fail --location --silent --show-error --max-time 300 \
       --retry 3 --retry-delay 2 \
       -o "$path.tmp" "$BASE_URL/$name"

  actual="$(sha256_of "$path.tmp")"
  if [ "$actual" != "$expected" ]; then
    rm -f "$path.tmp"
    echo "ERROR: checksum mismatch for $name" >&2
    echo "  expected $expected" >&2
    echo "  actual   $actual" >&2
    exit 1
  fi

  mv "$path.tmp" "$path"
  downloaded=$((downloaded + 1))
done

echo "Models ready in $TARGET_DIR ($downloaded downloaded, $cached already cached)."
