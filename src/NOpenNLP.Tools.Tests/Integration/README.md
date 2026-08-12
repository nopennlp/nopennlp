# Integration tests

These tests run the ported tools against the pre-trained Apache OpenNLP 1.5
models, which is the only coverage in the suite that exercises real inference
end to end: the zip container, the model readers, the maxent and perceptron
arithmetic, the beam search and the context generators all have to be correct
for the expected output to come back.

## Getting the models

From a PowerShell prompt:

```powershell
./build/download-test-models.ps1
```

or from any other shell:

```
pwsh -File build/download-test-models.ps1
```

That fetches 12 models (~46 MB) from <https://opennlp.sourceforge.net/models-1.5/>
into `testdata/models-sf/`, verifying each against a pinned SHA-256. The
directory is gitignored and the script is idempotent, so re-running it is cheap
and only fetches what is missing or corrupt.

It needs PowerShell 7 or later (`pwsh`), which runs on Windows, macOS and Linux.
Windows PowerShell 5.1 is not enough: the retry parameters used on the download
were added in PowerShell 6.

Set `NOPENNLP_TEST_DATA_DIR` to use a directory elsewhere; otherwise the tests
walk up from the test assembly looking for `testdata/`.

**Without the models these tests report inconclusive, not failed.** A fresh
clone builds and tests green without downloading anything.

## What is and is not covered

Adapted from upstream's `SourceForgeModelEval`, with two deliberate differences:

- **No parser.** `en-parser-chunking.bin` is 34 MB — more than the other twelve
  models combined — and the parser is not ported, so nothing would exercise it.
- **No Leipzig corpus.** Upstream hashes each model's output over a 300K
  sentence news corpus (63 MB) and compares the digest to a constant. That needs
  the `ObjectStream` sample-stream stack, which is not ported. These tests check
  the same models against fixed sentences with the expected analysis stated
  inline instead. Weaker than a hash over 300K sentences, but it covers the same
  code paths and a failure is readable rather than a changed digest.

Restoring either is worthwhile once the parser and the sample-stream stack are
ported.

## CI

A separate `models` job populates the cache once per run; the nine test legs
restore from it. The cache key is a hash of `build/download-test-models.ps1`, so
adding or changing a model invalidates it, and there are no `restore-keys` that
could serve a stale set. Apache's servers see at most one fetch per model-set
change, not one per build.
