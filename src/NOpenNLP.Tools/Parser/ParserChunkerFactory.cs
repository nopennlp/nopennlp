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
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Parser;

public class ParserChunkerFactory : ChunkerFactory
{
    public override IChunkerContextGenerator ContextGenerator => new ChunkContextGenerator(ChunkerME.DEFAULT_BEAM_SIZE);

    public override ISequenceValidator<TokenTag> SequenceValidator
    {
        get
        {
            // NOpenNLP: artifactProvider is nullable here because a factory can be
            // constructed before Init runs; upstream would throw an NPE at this point.
            IMaxentModel model = artifactProvider?.GetArtifact<IMaxentModel>("chunker.model")
                ?? throw new InvalidOperationException("The chunker model is not available.");

            string[] outcomes = new string[model.NumOutcomes];
            for (int i = 0; i < outcomes.Length; i++)
            {
                outcomes[i] = model.GetOutcome(i);
            }

            return new ParserChunkerSequenceValidator(outcomes);
        }
    }
}
