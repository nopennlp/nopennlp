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

namespace NOpenNLP.Tools.Parser;

public static class ParserFactory // NOpenNLP: made static
{
    public static IParser Create(ParserModel model, int beamSize, double advancePercentage)
    {
        if (ParserType.CHUNKING.Equals(model.ParserTypeValue))
        {
            return new Chunking.Parser(model, beamSize, advancePercentage);
        }
        else if (ParserType.TREEINSERT.Equals(model.ParserTypeValue))
        {
            return new Treeinsert.Parser(model, beamSize, advancePercentage);
        }
        else
        {
            // NOpenNLP: upstream calls getParserType().name(), which throws an NPE when
            // the manifest names no known type; interpolating the nullable value reports
            // the actual problem instead.
            throw new InvalidOperationException($"Unexpected ParserType: {model.ParserTypeValue}");
        }
    }

    public static IParser Create(ParserModel model) =>
        Create(model, AbstractBottomUpParser.defaultBeamSize, AbstractBottomUpParser.defaultAdvancePercentage);
}
