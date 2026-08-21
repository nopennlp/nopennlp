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
using NOpenNLP.Tools.Formats.Convert;
using NOpenNLP.Tools.Parser;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Ontonotes;

public class OntoNotesPOSSampleStreamFactory : AbstractSampleStreamFactory<POSSample?>
{
    private readonly OntoNotesParseSampleStreamFactory parseSampleStreamFactory =
        new OntoNotesParseSampleStreamFactory(); // NOpenNLP: made readonly

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [OntoNotesFormatParameters.OntoNotesDir];

    /// <inheritdoc/>
    public override IObjectStream<POSSample?> Create(IFormatParameterValues values)
    {
        IObjectStream<Parse?> parseSampleStream = parseSampleStreamFactory.Create(values);

        return new ParseToPOSSampleStream(parseSampleStream);
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<POSSample?>(
            "ontonotes", new OntoNotesPOSSampleStreamFactory());
}
