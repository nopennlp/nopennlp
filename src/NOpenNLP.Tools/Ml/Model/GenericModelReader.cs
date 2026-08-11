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
using NOpenNLP.Tools.Ml.Maxent.Io;
using NOpenNLP.Tools.Ml.Naivebayes;
using NOpenNLP.Tools.Ml.Perceptron;
using System;
using System.IO;

namespace NOpenNLP.Tools.Ml.Model;

public class GenericModelReader : AbstractModelReader
{
    private AbstractModelReader? delegateModelReader;

    public GenericModelReader(FileInfo f) : base(f)
    {
    }

    public GenericModelReader(IDataReader dataReader) : base(dataReader)
    {
    }

    public override void CheckModelType()
    {
        string modelType = ReadUTF();

        delegateModelReader = modelType switch
        {
            "Perceptron" => new PerceptronModelReader(dataReader),
            "GIS" => new GISModelReader(dataReader),
            "QN" => new QNModelReader(dataReader),
            "NaiveBayes" => new NaiveBayesModelReader(dataReader),
            _ => throw new InvalidOperationException("Unknown model format: " + modelType)
        };
    }

    public override AbstractModel ConstructModel()
    {
        // NOpenNLP: check to make sure CheckModelType was called
        if (delegateModelReader is null)
        {
            throw new InvalidOperationException($"You must call {nameof(CheckModelType)} before calling {nameof(ConstructModel)}.");
        }

        return delegateModelReader.ConstructModel();
    }
}
