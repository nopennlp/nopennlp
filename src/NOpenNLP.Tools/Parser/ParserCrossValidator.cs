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
using System.IO;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Parser;

public class ParserCrossValidator
{
    private readonly string languageCode;

    private readonly TrainingParameters @params;

    private readonly IHeadRules rules;

    private readonly FMeasure fmeasure = new();

    private readonly ParserType parserType; // NOpenNLP: made readonly

    // NOpenNLP: upstream declares this field but never assigns it, so the monitors
    // passed to the constructor are silently dropped and the evaluator below always
    // receives null. That behaviour is preserved; see the constructor. CS0649 is
    // suppressed because the field being unassigned is the point, not an oversight.
#pragma warning disable CS0649
    private readonly IParserEvaluationMonitor?[]? monitors;
#pragma warning restore CS0649

    public ParserCrossValidator(string languageCode, TrainingParameters @params,
        IHeadRules rules, ParserType parserType, params IParserEvaluationMonitor?[]? monitors)
    {
        this.languageCode = languageCode;
        this.@params = @params;
        this.rules = rules;
        this.parserType = parserType;
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    public virtual void Evaluate(IObjectStream<Parse?> samples, int nFolds)
    {
        CrossValidationPartitioner<Parse> partitioner = new(samples, nFolds);

        while (partitioner.HasNext)
        {
            CrossValidationPartitioner<Parse>.TrainingSampleStream trainingSampleStream =
                partitioner.Next();

            ParserModel model;

            // NOpenNLP: upstream trains on `samples`, not on trainingSampleStream --
            // every fold sees the whole corpus, including its own test partition.
            // Preserved as-is so the port matches upstream behaviour.
            if (ParserType.CHUNKING.Equals(parserType))
            {
                model = Chunking.Parser.Train(languageCode, samples, rules, @params);
            }
            else if (ParserType.TREEINSERT.Equals(parserType))
            {
                model = Treeinsert.Parser.Train(languageCode, samples, rules, @params);
            }
            else
            {
                throw new InvalidOperationException("Unexpected parser type: " + parserType);
            }

            ParserEvaluator evaluator = new(ParserFactory.Create(model), monitors);

            evaluator.Evaluate(trainingSampleStream.GetTestSampleStream());

            fmeasure.MergeInto(evaluator.FMeasure);
        }
    }

    public virtual FMeasure FMeasure => fmeasure;
}
