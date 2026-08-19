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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using NOpenNLP.Tools.Ml.Model;
using JDouble = J2N.Numerics.Double;

namespace NOpenNLP.Tools.Ml.Naivebayes;

/// <summary>
/// Model writer that saves models in plain text format.
/// </summary>
public class PlainTextNaiveBayesModelWriter : NaiveBayesModelWriter
{
    private readonly TextWriter output; // NOpenNLP: made readonly

    /// <summary>
    /// Constructor which takes a <see cref="NaiveBayesModel"/> and a <see cref="FileInfo"/> and
    /// prepares itself to write the model to that file. Detects whether the file is
    /// gzipped or not based on whether the suffix contains ".gz".
    /// </summary>
    /// <param name="model">The <see cref="NaiveBayesModel"/> which is to be persisted.</param>
    /// <param name="f">The file in which the model is to be persisted.</param>
    public PlainTextNaiveBayesModelWriter(AbstractModel model, FileInfo f)
        : base(model)
    {
        // NOpenNLP: upstream opens a FileOutputStream, which truncates an existing
        // file. FileMode.Create restores that; FileInfo.OpenWrite would not.
        if (f.Name.EndsWith(".gz", StringComparison.Ordinal))
        {
            output = new StreamWriter(new GZipStream(
                new FileStream(f.FullName, FileMode.Create, FileAccess.Write), CompressionMode.Compress));
        }
        else
        {
            output = new StreamWriter(new FileStream(f.FullName, FileMode.Create, FileAccess.Write));
        }
    }

    /// <summary>
    /// Constructor which takes a <see cref="NaiveBayesModel"/> and a <see cref="TextWriter"/> and
    /// prepares itself to write the model to that writer.
    /// </summary>
    /// <param name="model">The <see cref="NaiveBayesModel"/> which is to be persisted.</param>
    /// <param name="bw">The writer which will be used to persist the model.</param>
    public PlainTextNaiveBayesModelWriter(AbstractModel model, TextWriter bw)
        : base(model)
    {
        output = bw;
    }

    public override void WriteUTF(string s)
    {
        output.Write(s);
        output.WriteLine();
    }

    public override void WriteInt32(int i)
    {
        // NOpenNLP: upstream uses Integer.toString, which is culture-invariant.
        output.Write(i.ToString(CultureInfo.InvariantCulture));
        output.WriteLine();
    }

    public override void WriteDouble(double d)
    {
        // NOpenNLP: upstream uses Double.toString. .NET's own formatting differs
        // from Java's in both shape and culture: 2.0 renders as "2", 1.0E7 as
        // "10000000", and a comma decimal separator appears under a locale such as
        // de-DE. J2N reproduces Java's algorithm, so the file stays byte-identical
        // to one Apache OpenNLP would have written and is readable by both.
        output.Write(JDouble.ToString(d, CultureInfo.InvariantCulture));
        output.WriteLine();
    }

    protected override void CloseCore()
    {
        output.Flush();
        output.Dispose();
    }
}
