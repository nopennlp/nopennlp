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
using System.Collections.Generic;
using System.IO;
using System.Text;
using J2N.Text;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// Class for using a file of events as an event stream.  The format of the file is one event per line with
/// each line consisting of outcome followed by contexts (space delimited).
/// </summary>
public class FileEventStream : ObjectStreamBase<Event?>
{
    protected readonly TextReader reader;

    /// <summary>
    /// Creates a new file event stream from the specified file name.
    /// </summary>
    /// <param name="fileName">the name of the file containing the events.</param>
    /// <param name="encoding">the encoding of the file, or <c>null</c> for the default.</param>
    /// <exception cref="IOException">When the specified file can not be read.</exception>
    public FileEventStream(string fileName, Encoding? encoding = null)
        // NOpenNLP: upstream's null encoding selects FileReader, which uses the
        // platform default charset. StreamReader's default is UTF-8, which is also
        // the modern JVM default and what the File overload below asks for.
        : this(encoding == null
            ? new StreamReader(fileName)
            : new StreamReader(fileName, encoding))
    {
    }

    public FileEventStream(TextReader reader)
    {
        this.reader = reader;
    }

    /// <summary>
    /// Creates a new file event stream from the specified file.
    /// </summary>
    /// <param name="file">the file containing the events.</param>
    /// <exception cref="IOException">When the specified file can not be read.</exception>
    public FileEventStream(FileInfo file)
    {
        reader = new StreamReader(file.FullName, Encoding.UTF8);
    }

    public override Event? Read()
    {
        string? line;
        if ((line = reader.ReadLine()) != null)
        {
            // NOpenNLP: Java's StringTokenizer with no delimiters splits on
            // whitespace and skips runs of it. J2N's exposes only the enumerator
            // API, and there is no countTokens(), so the tokens are collected and
            // the first is taken as the outcome, which is what upstream computes.
            using var st = new StringTokenizer(line);
            List<string> tokens = [];
            while (st.MoveNext())
            {
                tokens.Add(st.Current);
            }

            if (tokens.Count == 0)
            {
                // Upstream's st.nextToken() throws NoSuchElementException on a
                // blank line; InvalidOperationException is the .NET counterpart.
                throw new InvalidOperationException("No tokens remain.");
            }

            string outcome = tokens[0];
            int count = tokens.Count - 1;
            string[] context = new string[count];
            for (int ci = 0; ci < count; ci++)
            {
                context[ci] = tokens[ci + 1];
            }

            return new Event(outcome, context);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a string representing the specified event.
    /// </summary>
    /// <param name="event">The event for which a string representation is needed.</param>
    /// <returns>A string representing the specified event.</returns>
    public static string ToLine(Event @event)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(@event.Outcome);
        string[] context = @event.Context;
        for (int ci = 0, cl = context.Length; ci < cl; ci++)
        {
            sb.Append(' ').Append(context[ci]);
        }

        // NOpenNLP: Environment.NewLine is the .NET equivalent of reading the
        // line.separator system property.
        sb.Append(Environment.NewLine);
        return sb.ToString();
    }

    public override void Reset() => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            reader.Dispose();
        }
    }
}
