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
using System.IO;
using System.IO.Compression;
using NOpenNLP.Tools.Ml.Maxent.Io;
using NOpenNLP.Tools.Ml.Naivebayes;
using NOpenNLP.Tools.Ml.Perceptron;

namespace NOpenNLP.Tools.Ml.Model;

public class GenericModelWriter : AbstractModelWriter
{
    private AbstractModelWriter delegateWriter = null!;

    public GenericModelWriter(AbstractModel model, FileInfo file)
    {
        string filename = file.Name;
        Stream os;

        // handle the zipped/not zipped distinction
        // NOpenNLP: upstream opens a FileOutputStream, which truncates an existing
        // file. FileMode.Create restores that; FileInfo.OpenWrite would not.
        if (filename.EndsWith(".gz", StringComparison.Ordinal))
        {
            os = new GZipStream(
                new FileStream(file.FullName, FileMode.Create, FileAccess.Write), CompressionMode.Compress);
            filename = filename.Substring(0, filename.Length - 3);
        }
        else
        {
            os = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
        }

        Init(model, os);
    }

    public GenericModelWriter(AbstractModel model, Stream dos)
    {
        Init(model, dos);
    }

    private void Init(AbstractModel model, Stream dos)
    {
        // NOpenNLP: upstream leaves delegateWriter null for a model type it does
        // not recognise, and every method then throws NullPointerException. The
        // model type is an enum with exactly these four values, so the port makes
        // the exhaustiveness explicit rather than deferring the failure.
        delegateWriter = model.GetModelType() switch
        {
            AbstractModel.ModelType.Perceptron => new BinaryPerceptronModelWriter(model, dos),
            AbstractModel.ModelType.Maxent => new BinaryGISModelWriter(model, dos),
            AbstractModel.ModelType.MaxentQn => new BinaryQNModelWriter(model, dos),
            AbstractModel.ModelType.NaiveBayes => new BinaryNaiveBayesModelWriter(model, dos),
            _ => throw new ArgumentException($"Unknown model type: {model.GetModelType()}", nameof(model)),
        };
    }

    protected override void CloseCore() => delegateWriter.Close();

    public override void Persist() => delegateWriter.Persist();

    public override void WriteDouble(double d) => delegateWriter.WriteDouble(d);

    public override void WriteInt32(int i) => delegateWriter.WriteInt32(i);

    public override void WriteUTF(string s) => delegateWriter.WriteUTF(s);
}
