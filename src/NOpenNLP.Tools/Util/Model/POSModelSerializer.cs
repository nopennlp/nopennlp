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
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Postag;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Util.Model;

public class POSModelSerializer : IArtifactSerializer<POSModel>
{
    public virtual POSModel Create(Stream @in)
    {
        POSModel posModel = new POSModel(new UncloseableInputStream(@in));

        // The 1.6.x models write the non-default beam size into the model itself.
        // In 1.5.x the parser configured the beam size when the model was loaded,
        // this is not possible anymore with the new APIs
        Version version = posModel.Version;
        if (version is { Major: 1, Minor: 5 })
        {
            if (posModel.GetManifestProperty(BeamSearch.BEAM_SIZE_PARAMETER) == null)
            {
                JCG.Dictionary<string, string> manifestInfoEntries = [];

                // The version in the model must be correct or otherwise version
                // dependent code branches in other places fail
                manifestInfoEntries["OpenNLP-Version"] = "1.5.0";

#pragma warning disable CS0618 // the deprecated accessor exists for exactly this 1.5.x path
                posModel = new POSModel(posModel.Language, posModel.PosModel!, 10,
                    manifestInfoEntries, posModel.Factory);
#pragma warning restore CS0618
            }
        }

        return posModel;
    }

    public virtual void Serialize(POSModel artifact, Stream @out)
        => artifact.Serialize(@out);

    // NOpenNLP: upstream relies on a default interface implementation to
    // bridge the non-generic IArtifactSerializer; DIMs are unavailable on
    // netstandard2.0/net462, so the bridge is explicit here.
    object IArtifactSerializer.Create(Stream @in)
        => Create(@in);

    void IArtifactSerializer.Serialize(object artifact, Stream @out)
        => Serialize((POSModel)artifact, @out);
}
