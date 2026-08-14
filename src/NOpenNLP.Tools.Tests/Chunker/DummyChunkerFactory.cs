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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// The custom factory named by the chunker170custom.bin and chunker180custom.bin
/// test models, which record it as "opennlp.tools.chunker.DummyChunkerFactory".
/// </summary>
public class DummyChunkerFactory : ChunkerFactory
{
    public DummyChunkerFactory()
    {
    }

    public override IChunkerContextGenerator ContextGenerator => new DummyContextGenerator();

    public override ISequenceValidator<TokenTag> SequenceValidator => new DummySequenceValidator();

    internal class DummyContextGenerator : DefaultChunkerContextGenerator
    {
        public override string[] GetContext(int i, string[] toks, string[] tags,
            string[] preds)
        {
            return base.GetContext(i, toks, tags, preds);
        }
    }

    internal class DummySequenceValidator : DefaultChunkerSequenceValidator
    {
        public override bool ValidSequence(int i, TokenTag[] sequence, string[] s,
            string outcome)
        {
            return base.ValidSequence(i, sequence, s, outcome);
        }
    }
}
