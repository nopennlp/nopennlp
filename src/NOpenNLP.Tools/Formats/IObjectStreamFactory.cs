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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// Creates an <see cref="IObjectStream{T}"/> of samples read from a particular corpus
/// format, and describes the command line parameters that format accepts.
/// </summary>
/// <typeparam name="T">the sample type this factory produces</typeparam>
// NOpenNLP: upstream is opennlp.tools.cmdline.ObjectStreamFactory, and declares
// `<P> Class<P> getParameters()` returning an annotated interface that ArgumentParser
// reflects over to build and validate the argument list. This port drives the command
// line with System.CommandLine instead, so a factory contributes its parameters as
// option descriptors and then reads their parsed values, and no reflection is involved.
// The interface also moves from cmdline into formats, next to the factories that
// implement it, so the library owns the format SPI rather than the CLI project.
public interface IObjectStreamFactory<out T>
{
    /// <summary>
    /// The parameters this format accepts, in the order they should be listed in help.
    /// </summary>
    IEnumerable<IFormatParameter> Parameters { get; }

    /// <summary>
    /// Creates the <see cref="IObjectStream{T}"/> over the corpus named by
    /// <paramref name="values"/>.
    /// </summary>
    /// <param name="values">the parsed values of <see cref="Parameters"/></param>
    /// <returns>a stream of samples</returns>
    IObjectStream<T> Create(IFormatParameterValues values);
}
