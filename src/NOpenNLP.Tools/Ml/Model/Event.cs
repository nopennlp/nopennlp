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
using System.Text;
using JSingle = J2N.Numerics.Single;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// The context of a decision point during training.  This includes
/// contextual predicates and an outcome.
/// </summary>
public class Event
{
    private readonly string outcome;
    private readonly string[] context;
    private readonly float[]? values;

    // NOpenNLP: used optional parameter instead of ctor overload
    public Event(string outcome, string[] context, float[]? values = null)
    {
        // NOpenNLP: upstream uses Objects.requireNonNull; ArgumentNullException
        // is the .NET counterpart of the NullPointerException it throws.
        this.outcome = outcome ?? throw new ArgumentNullException(nameof(outcome), "outcome must not be null");
        this.context = context ?? throw new ArgumentNullException(nameof(context), "context must not be null");
        this.values = values;
    }

    public virtual string Outcome => outcome;

    public virtual string[] Context => context;

    public virtual float[]? Values => values;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(outcome).Append(" [");
        if (context.Length > 0)
        {
            sb.Append(context[0]);
            if (values != null)
            {
                // NOpenNLP: upstream appends the float with Java's Float.toString.
                // StringBuilder.Append(float) would use the current culture and
                // .NET's own shortest-round-trip format, so a value like 1.0E-5
                // would render as "1E-05", and as "1E-05" with a comma decimal
                // separator under a locale such as de-DE. HashSumEventStream
                // hashes this string and TwoPassDataIndexer compares the hash
                // across two passes, so J2N is used to reproduce Java exactly.
                sb.Append('=').Append(JSingle.ToString(values[0], CultureInfo.InvariantCulture));
            }
        }

        for (int ci = 1; ci < context.Length; ci++)
        {
            sb.Append(' ').Append(context[ci]);
            if (values != null)
            {
                sb.Append('=').Append(JSingle.ToString(values[ci], CultureInfo.InvariantCulture));
            }
        }

        sb.Append(']');
        return sb.ToString();
    }
}
