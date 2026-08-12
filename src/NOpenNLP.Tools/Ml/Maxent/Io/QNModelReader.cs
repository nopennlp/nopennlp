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

using NOpenNLP.Tools.Ml.Model;
using System;
using System.IO;
using NOpenNLP.Tools.Ml.Maxent.Quasinewton;

namespace NOpenNLP.Tools.Ml.Maxent.Io;

public class QNModelReader : GISModelReader
{
    public QNModelReader(IDataReader dataReader)
        : base(dataReader)
    {
    }

    public QNModelReader(FileInfo file)
        : base(file)
    {
    }

    public override void CheckModelType()
    {
        string modelType = ReadUTF();
        if (!modelType.Equals("QN"))
            Console.WriteLine($"Error: attempting to load a {modelType} model as a MAXENT_QN model. You should expect problems.");
    }

    // NOpenNLP: upstream returns the covariant QNModel, but covariant return
    // types require net5.0+, so the base AbstractModel type is used here. The
    // returned instance is still a QNModel.
    public override AbstractModel ConstructModel()
    {
        string[] outcomeLabels = Outcomes;
        int[][] outcomePatterns = OutcomePatterns;
        string[] predLabels = Predicates;
        Context[] @params = GetParameters(outcomePatterns);
        return new QNModel(@params, predLabels, outcomeLabels);
    }
}
