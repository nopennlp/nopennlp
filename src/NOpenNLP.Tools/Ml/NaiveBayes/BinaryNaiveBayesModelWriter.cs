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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Support;

namespace NOpenNLP.Tools.Ml.Naivebayes;

/// <summary>
/// Model writer that saves models in binary format.
/// </summary>
public class BinaryNaiveBayesModelWriter : NaiveBayesModelWriter
{
    private readonly Stream output; // NOpenNLP: made readonly

    /// <summary>
    /// Constructor which takes a <see cref="NaiveBayesModel"/> and a <see cref="FileInfo"/> and
    /// prepares itself to write the model to that file. Detects whether the file is
    /// gzipped or not based on whether the suffix contains ".gz".
    /// </summary>
    /// <param name="model">The <see cref="NaiveBayesModel"/> which is to be persisted.</param>
    /// <param name="f">The file in which the model is to be persisted.</param>
    public BinaryNaiveBayesModelWriter(AbstractModel model, FileInfo f)
        : base(model)
    {
        // NOpenNLP: upstream opens a FileOutputStream, which truncates an existing
        // file. FileMode.Create restores that; FileInfo.OpenWrite would not.
        if (f.Name.EndsWith(".gz", StringComparison.Ordinal))
        {
            output = new GZipStream(
                new FileStream(f.FullName, FileMode.Create, FileAccess.Write), CompressionMode.Compress);
        }
        else
        {
            output = new FileStream(f.FullName, FileMode.Create, FileAccess.Write);
        }
    }

    /// <summary>
    /// Constructor which takes a <see cref="NaiveBayesModel"/> and a <see cref="Stream"/> and
    /// prepares itself to write the model to that stream.
    /// </summary>
    /// <param name="model">The <see cref="NaiveBayesModel"/> which is to be persisted.</param>
    /// <param name="dos">The stream which will be used to persist the model.</param>
    public BinaryNaiveBayesModelWriter(AbstractModel model, Stream dos)
        : base(model)
    {
        output = dos;
    }

    public override void WriteUTF(string s) => output.WriteJavaUTF(s);

    public override void WriteInt32(int i) => output.WriteJavaInt32(i);

    public override void WriteDouble(double d) => output.WriteJavaDouble(d);

    protected override void CloseCore()
    {
        output.Flush();
        output.Dispose();
    }
}
