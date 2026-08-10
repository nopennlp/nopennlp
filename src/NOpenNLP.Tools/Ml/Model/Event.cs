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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using System;
using System.Text;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// The context of a decision point during training.  This includes
/// contextual predicates and an outcome.
/// </summary>
// NOpenNLP: used optional parameter instead of ctor overload
public class Event(string outcome, string[] context, float[]? values = null)
{
    private readonly string outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
    private readonly string[] context = context ?? throw new ArgumentNullException(nameof(context));

    public virtual string GetOutcome()
    {
        return outcome;
    }

    public virtual string[] GetContext()
    {
        return context;
    }

    public virtual float[]? GetValues()
    {
        return values;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(outcome).Append(" [");
        if (context.Length > 0)
        {
            sb.Append(context[0]);
            if (values != null)
            {
                sb.Append('=').Append(values[0]);
            }
        }

        for (int ci = 1; ci < context.Length; ci++)
        {
            sb.Append(' ').Append(context[ci]);
            if (values != null)
            {
                sb.Append('=').Append(values[ci]);
            }
        }

        sb.Append(']');
        return sb.ToString();
    }
}
