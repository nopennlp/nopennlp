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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Brat;

public class BratDocument
{
    private readonly AnnotationConfiguration config;
    private readonly string id;
    private readonly string text;
    private readonly IDictionary<string, BratAnnotation> annotationMap;

    public BratDocument(AnnotationConfiguration config, string id, string text,
        ICollection<BratAnnotation> annotations)
    {
        this.config = config;
        this.id = id;
        this.text = text;

        JCG.Dictionary<string, BratAnnotation> annMap = [];
        JCG.List<AnnotatorNoteAnnotation> noteList = [];
        foreach (var annotation in annotations)
        {
            if (annotation is AnnotatorNoteAnnotation noteAnnotation)
            {
                noteList.Add(noteAnnotation);
            }
            else
            {
                // NOpenNLP: the indexer overwrites a duplicate id, as Java's Map.put does;
                // Add would throw instead.
                annMap[annotation.Id] = annotation;
            }
        }

        // attach AnnotatorNote to the appropriate Annotation.
        // the note should ALWAYS have an appropriate id in the map,
        // but just to be safe, check for null.
        foreach (var note in noteList)
        {
            // NOpenNLP: upstream reads Map.get, which yields null for an unknown attached
            // id; TryGetValue preserves that rather than throwing the way the C# indexer
            // would, keeping the "just to be safe" check above meaningful.
            if (annMap.TryGetValue(note.AttachedId, out var annotation))
            {
                annotation.Note = note.Note;
            }
        }

        annotationMap = annMap.AsReadOnly();
    }

    public AnnotationConfiguration Config => config;

    public string Id => id;

    public string Text => text;

    /// <summary>
    /// Gets the annotation registered under <paramref name="id"/>.
    /// </summary>
    /// <param name="id">the annotation id to look up</param>
    /// <returns>the annotation, or <c>null</c> if no annotation has that id</returns>
    // NOpenNLP: upstream returns Map.get, which yields null for an unknown id; the C#
    // indexer would throw, so this uses TryGetValue and keeps the null.
    public BratAnnotation? GetAnnotation(string id) =>
        annotationMap.TryGetValue(id, out var annotation) ? annotation : null;

    public ICollection<BratAnnotation> Annotations => annotationMap.Values;

    /// <summary>
    /// Parses a brat document from its text and annotation streams.
    /// </summary>
    /// <param name="config">the annotation configuration to parse against</param>
    /// <param name="id">the document id</param>
    /// <param name="txtIn">the stream to read the document text from</param>
    /// <param name="annIn">the stream to read the .ann annotations from</param>
    /// <returns>the parsed <see cref="BratDocument"/></returns>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public static BratDocument ParseDocument(AnnotationConfiguration config, string id,
        Stream txtIn, Stream annIn)
    {
        // NOpenNLP: leaveOpen keeps the reader from closing the caller's stream, matching
        // upstream, which never closes the InputStreamReader it wraps around txtIn.
        using var txtReader = new StreamReader(txtIn, Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

        var text = new StringBuilder();

        char[] cbuf = new char[1024];

        int len;
        while ((len = txtReader.Read(cbuf, 0, cbuf.Length)) > 0)
        {
            text.Append(cbuf, 0, len);
        }

        JCG.List<BratAnnotation> annotations = [];
        IObjectStream<BratAnnotation?> annStream = new BratAnnotationStream(config, id, annIn);
        while (annStream.Read() is { } ann)
        {
            annotations.Add(ann);
        }
        annStream.Dispose();

        return new BratDocument(config, id, text.ToString(), annotations);
    }
}
