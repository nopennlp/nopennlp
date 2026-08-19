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
using System.Globalization;
using J2N.Globalization;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using JSingle = J2N.Numerics.Single;

namespace NOpenNLP.Tools.Ml.Model;

public class SimpleEventStreamBuilder
{
    private readonly List<Event> eventList = []; // NOpenNLP: made readonly
    private int pos = 0;

    /*
     * the format of event should look like:
     * without values) other/w=he n1w=belongs n2w=to po=other pow=other,He powf=other,ic
     * with values) other/w=he;0.5 n1w=belongs;0.4 n2w=to;0.3 po=other;0.5 pow=other,He;0.25 powf=other,ic;0.5
     */
    public SimpleEventStreamBuilder Add(string @event)
    {
        string[] ss = @event.Split('/');
        if (ss.Length != 2)
        {
            throw new RuntimeException(string.Format(CultureInfo.InvariantCulture,
                "format error of the event \"{0}\"", @event));
        }

        // look for context (and values)
        string[] cvPairs = ss[1].Split([' ', '\t', '\n', '\r', '\f'], StringSplitOptions.RemoveEmptyEntries);
        if (cvPairs[0].Contains(";")) // has values?
        {
            string[] context = new string[cvPairs.Length];
            float[] values = new float[cvPairs.Length];
            for (int i = 0; i < cvPairs.Length; i++)
            {
                string[] pair = cvPairs[i].Split(';');
                if (pair.Length != 2)
                {
                    throw new RuntimeException(string.Format(CultureInfo.InvariantCulture,
                        "format error of the event \"{0}\". \"[{1}]\" doesn't have value",
                        @event, string.Join(", ", pair)));
                }

                context[i] = pair[0];
                // NOpenNLP: upstream uses Float.parseFloat, which is culture-invariant.
                values[i] = JSingle.Parse(pair[1], NumberStyle.Float, CultureInfo.InvariantCulture);
            }

            eventList.Add(new Event(ss[0], context, values));
        }
        else
        {
            eventList.Add(new Event(ss[0], cvPairs));
        }

        return this;
    }

    public IObjectStream<Event?> Build() => new SimpleEventStream(this);

    // NOpenNLP: upstream returns an anonymous ObjectStream; C# has no anonymous
    // classes, so this is a named private type reading the enclosing builder's list.
    private sealed class SimpleEventStream(SimpleEventStreamBuilder owner) : ObjectStreamBase<Event?>
    {
        public override Event? Read()
        {
            if (owner.eventList.Count <= owner.pos)
            {
                return null;
            }

            return owner.eventList[owner.pos++];
        }
    }
}
