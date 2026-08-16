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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Namefind;

public class BilouNameFinderSequenceValidator : ISequenceValidator<string>
{
    public virtual bool ValidSequence(int i, string[] inputSequence, string[] outcomesSequence, string outcome)
    {
        if (outcome.EndsWith(BilouCodec.CONTINUE, StringComparison.Ordinal)
            || outcome.EndsWith(BilouCodec.LAST, StringComparison.Ordinal))
        {
            int li = outcomesSequence.Length - 1;

            if (li == -1)
            {
                return false;
            }
            else if (outcomesSequence[li].EndsWith(BilouCodec.OTHER, StringComparison.Ordinal)
                || outcomesSequence[li].EndsWith(BilouCodec.UNIT, StringComparison.Ordinal))
            {
                return false;
            }
            else if (outcomesSequence[li].EndsWith(BilouCodec.LAST, StringComparison.Ordinal)
                && (outcome.EndsWith(BilouCodec.CONTINUE, StringComparison.Ordinal)
                    || outcome.EndsWith(BilouCodec.LAST, StringComparison.Ordinal)))
            {
                return false;
            }
            else if (outcomesSequence[li].EndsWith(BilouCodec.CONTINUE, StringComparison.Ordinal)
                || outcomesSequence[li].EndsWith(BilouCodec.START, StringComparison.Ordinal))
            {
                // if it is continue, we have to check if previous match was of the same type
                string? previousNameType = NameFinderME.ExtractNameType(outcomesSequence[li]);
                string? nameType = NameFinderME.ExtractNameType(outcome);
                if (previousNameType != null || nameType != null)
                {
                    if (nameType != null)
                    {
                        return nameType.Equals(previousNameType);
                    }

                    return false; // outcomes types are not equal
                }
            }
        }

        if (outcomesSequence.Length > 0)
        {
            if (outcome.EndsWith(BilouCodec.START, StringComparison.Ordinal)
                || outcome.EndsWith(BilouCodec.OTHER, StringComparison.Ordinal)
                || outcome.EndsWith(BilouCodec.UNIT, StringComparison.Ordinal))
            {
                return !outcomesSequence[^1].EndsWith(BilouCodec.START, StringComparison.Ordinal)
                    && !outcomesSequence[^1].EndsWith(BilouCodec.CONTINUE, StringComparison.Ordinal);
            }
        }

        return true;
    }
}
