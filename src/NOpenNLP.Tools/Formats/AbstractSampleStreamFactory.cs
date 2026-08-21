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
using System.IO;
using System.Text;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// Base class for sample stream factories.
/// </summary>
public abstract class AbstractSampleStreamFactory<T> : IObjectStreamFactory<T>
{
    /// <inheritdoc/>
    public abstract IEnumerable<IFormatParameter> Parameters { get; }

    /// <inheritdoc/>
    public abstract IObjectStream<T> Create(IFormatParameterValues values);

    /// <summary>
    /// The language of the samples this factory produces.
    /// </summary>
    public virtual string Lang => "eng";

    /// <summary>
    /// Opens the <c>-data</c> file named by <paramref name="values"/> as a stream of
    /// lines, decoded with <c>-encoding</c>.
    /// </summary>
    // NOpenNLP: upstream's readData() lives on this base in later releases and is
    // inlined into each factory in 1.9.4. It is here because every text-based factory
    // repeats it, and CmdLineUtil -- where upstream keeps createInputStreamFactory --
    // is part of the CLI project.
    protected static IObjectStream<string?> ReadData(IFormatParameterValues values)
    {
        FileInfo data = values.Get<FileInfo>(FormatParameters.Data)!;
        Encoding encoding = FormatParameters.ResolveEncoding(
            values.Get<string>(FormatParameters.Encoding));

        CheckInputFile("Data", data);

        return new PlainTextByLineStream(CreateInputStreamFactory(data), encoding);
    }

    /// <summary>
    /// Checks that <paramref name="inFile"/> exists, is not a directory, and is readable,
    /// failing the way the command line tools do when it is not.
    /// </summary>
    /// <param name="name">
    /// the name used to refer to the file in the error message; it should start with a
    /// capital letter
    /// </param>
    // NOpenNLP: stands in for CmdLineUtil.checkInputFile, which lives in the CLI package
    // upstream while the factories that call it live here. The messages and the -1 exit
    // code are upstream's verbatim. CmdLineUtil has its own copy for the tools, which
    // reach it without going through a factory.
    protected internal static void CheckInputFile(string name, FileInfo inFile)
    {
        string? isFailure = null;

        // NOpenNLP: a java.io.File models either kind of path, so isDirectory() and
        // exists() are both meaningful on one object. A FileInfo pointing at a directory
        // reports Exists == false, so the directory case is tested separately to keep
        // upstream's more specific message.
        if (Directory.Exists(inFile.FullName))
        {
            isFailure = "The " + name + " file is a directory!";
        }
        else if (!inFile.Exists)
        {
            isFailure = "The " + name + " file does not exist!";
        }

        if (null != isFailure)
        {
            throw new TerminateToolException(-1, isFailure + " Path: " + inFile.FullName);
        }
    }

    /// <summary>
    /// Creates an <see cref="IInputStreamFactory"/> over <paramref name="file"/>,
    /// failing the way the command line tools do when it cannot be read.
    /// </summary>
    // NOpenNLP: stands in for CmdLineUtil.createInputStreamFactory, which lives in the
    // CLI package upstream. The exit code and message are upstream's verbatim, and
    // FileNotFoundException derives from IOException so this catches what upstream
    // catches. `file` renders as the path in both languages.
    protected internal static IInputStreamFactory CreateInputStreamFactory(FileInfo file)
    {
        try
        {
            return new MarkableFileInputStreamFactory(file);
        }
        catch (FileNotFoundException e)
        {
            throw new TerminateToolException(-1, "File '" + file + "' cannot be found", e);
        }
    }
}
