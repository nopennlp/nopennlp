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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using NOpenNLP.Tools.Support;
using System.Collections.Generic;
using System.IO;

namespace NOpenNLP.Tools.Util.Model;

public class DictionarySerializer : IArtifactSerializer<NOpenNLP.Tools.Dictionary.Dictionary>
{
    // NOpenNLP: upstream serializes opennlp.tools.dictionary.Dictionary, not a
    // java.util.Dictionary; the converter had mapped this to IDictionary/Hashtable.
    public virtual NOpenNLP.Tools.Dictionary.Dictionary Create(Stream @in)
    {
        return new NOpenNLP.Tools.Dictionary.Dictionary(@in);
    }

    // public virtual void Serialize(NOpenNLP.Tools.Dictionary.Dictionary dictionary, Stream @out)
    // {
    //     dictionary.Serialize(@out);
    // }

    public static void Register(IDictionary<string, IArtifactSerializer> factories)
    {
        factories.Put("dictionary", new DictionarySerializer());
    }

    // NOpenNLP: upstream relies on a default interface implementation to
    // bridge the non-generic IArtifactSerializer; DIMs are unavailable on
    // netstandard2.0/net462, so the bridge is explicit here.
    object IArtifactSerializer.Create(Stream @in) => Create(@in);
}
