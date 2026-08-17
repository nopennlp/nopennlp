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

namespace NOpenNLP.Tools.Langdetect;

public class LanguageDetectorConfig
{
    public const int DEFAULT_MAX_LENGTH = 10000;

    public const int DEFAULT_CHUNK_SIZE = 200;

    public const int DEFAULT_MIN_CONSEC_IMPROVEMENTS = 2;

    public const double DEFAULT_MIN_DIFF = 0.20;

    public static readonly LanguageDetectorConfig DEFAULT_LANGUAGE_DETECTOR_CONFIG =
        new ImmutableLanguageDetectorConfig();

    private int maxLength = DEFAULT_MAX_LENGTH;
    private int chunkSize = DEFAULT_CHUNK_SIZE;
    private int minConsecImprovements = DEFAULT_MIN_CONSEC_IMPROVEMENTS;
    private double minDiff = DEFAULT_MIN_DIFF;

    /// <summary>
    /// Maximum length in codepoints of text to process.
    /// </summary>
    public virtual int MaxLength
    {
        get => maxLength;
        set => maxLength = value;
    }

    /// <summary>
    /// Size in codepoints of chunk to process at each
    /// step for the probing detection.
    /// <para/>
    /// After processing a chunk of this size, the probing
    /// detection will compute probabilities and determine
    /// if there is enough confidence to stop.
    /// </summary>
    public virtual int ChunkSize
    {
        get => chunkSize;
        set => chunkSize = value;
    }

    /// <summary>
    /// Minimum number of consecutive increased probabilities
    /// for the top language required in probing detection
    /// to stop early.
    /// <para/>
    /// If this value equals 0, probing detection will
    /// rely solely on <see cref="MinDiff"/>.
    /// </summary>
    public virtual int MinConsecImprovements
    {
        get => minConsecImprovements;
        set => minConsecImprovements = value;
    }

    /// <summary>
    /// Minimum difference in confidence between the top predicted
    /// language and the next most likely language.
    /// <para/>
    /// If this value equals 0, probing detection will
    /// rely solely on <see cref="MinConsecImprovements"/>.
    /// </summary>
    public virtual double MinDiff
    {
        get => minDiff;
        set => minDiff = value;
    }

    private sealed class ImmutableLanguageDetectorConfig : LanguageDetectorConfig
    {
        public override int MaxLength
        {
            get => base.MaxLength;
            set { /* no-op */ }
        }

        public override int ChunkSize
        {
            get => base.ChunkSize;
            set { /* no-op */ }
        }

        public override int MinConsecImprovements
        {
            get => base.MinConsecImprovements;
            set { /* no-op */ }
        }

        public override double MinDiff
        {
            get => base.MinDiff;
            set { /* no-op */ }
        }
    }
}
