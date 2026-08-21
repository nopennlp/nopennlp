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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Loads a model and does all the error handling for the command line tools.
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public abstract class ModelLoader<T>
{
    private readonly string modelName;

    protected ModelLoader(string modelName)
    {
        this.modelName = modelName
            ?? throw new ArgumentNullException(nameof(modelName), "modelName must not be null!");
    }

    /// <exception cref="IOException">if the model cannot be read</exception>
    protected abstract T LoadModel(Stream modelIn);

    public T Load(FileInfo modelFile)
    {
        var stopwatch = Stopwatch.StartNew();

        CmdLineUtil.CheckInputFile(modelName + " model", modelFile);

        Console.Error.Write("Loading " + modelName + " model ... ");

        T model;
        try
        {
            using Stream modelIn = new BufferedStream(
                CmdLineUtil.OpenInFile(modelFile), CmdLineUtil.IoBufferSize);
            model = LoadModel(modelIn);
        }
        catch (InvalidFormatException e)
        {
            Console.Error.WriteLine("failed");
            throw new TerminateToolException(-1, "Model has invalid format", e);
        }
        catch (IOException e)
        {
            Console.Error.WriteLine("failed");
            throw new TerminateToolException(-1,
                "IO error while loading model file '" + modelFile + "'", e);
        }

        stopwatch.Stop();

        // NOpenNLP: upstream uses printf("done (%.3fs)\n"), which is always LF and uses
        // the JVM's default locale for the decimal separator. The invariant culture
        // keeps it a period, which is what the English-only messages around it assume.
        Console.Error.Write(string.Format(CultureInfo.InvariantCulture,
            "done ({0:F3}s)\n", stopwatch.Elapsed.TotalSeconds));

        return model;
    }
}
