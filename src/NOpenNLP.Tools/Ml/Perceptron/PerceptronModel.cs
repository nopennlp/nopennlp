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

using J2N;
using NOpenNLP.Tools.Ml.Model;
using System;

namespace NOpenNLP.Tools.Ml.Perceptron;

public class PerceptronModel : AbstractModel
{
    public PerceptronModel(Context[] @params, string[] predLabels, string[] outcomeNames)
        : base(@params, predLabels, outcomeNames)
    {
        modelType = ModelType.Perceptron;
    }

    public override double[] Eval(string[] context)
    {
        return Eval(context, new double[evalParams.GetNumOutcomes()]);
    }

    public override double[] Eval(string[] context, float[] values)
    {
        return Eval(context, values, new double[evalParams.GetNumOutcomes()]);
    }

    public override double[] Eval(string[] context, double[] probs)
    {
        return Eval(context, null, probs);
    }

    public virtual double[] Eval(string[] context, float[]? values, double[] outsums)
    {
        Context[] scontexts = new Context[context.Length];
        outsums.Fill(0);
        for (int i = 0; i < context.Length; i++)
        {
            // NOpenNLP: Java's Map.get() returns null for an absent key; the .NET indexer throws.
            pmap.TryGetValue(context[i], out Context ctx);
            scontexts[i] = ctx;
        }

        return Eval(scontexts, values, outsums, evalParams, true);
    }

    public static double[] Eval(int[] context, double[] prior, EvalParameters model)
    {
        return Eval(context, null, prior, model, true);
    }

    internal static double[] Eval(int[] context, float[]? values, double[] prior, EvalParameters model, bool normalize)
    {
        Context[] scontexts = new Context[context.Length];
        for (int i = 0; i < context.Length; i++)
        {
            scontexts[i] = model.GetParams()[context[i]];
        }

        return Eval(scontexts, values, prior, model, normalize);
    }

    internal static double[] Eval(Context[] context, float[]? values, double[] prior, EvalParameters model, bool normalize)
    {
        ArrayMath.SumFeatures(context, values, prior);
        if (normalize)
        {
            int numOutcomes = model.GetNumOutcomes();
            double maxPrior = 1;
            for (int oid = 0; oid < numOutcomes; oid++)
            {
                if (maxPrior < Math.Abs(prior[oid]))
                    maxPrior = Math.Abs(prior[oid]);
            }

            double normal = 0;
            for (int oid = 0; oid < numOutcomes; oid++)
            {
                prior[oid] = Math.Exp(prior[oid] / maxPrior);
                normal += prior[oid];
            }

            for (int oid = 0; oid < numOutcomes; oid++)
            {
                prior[oid] /= normal;
            }
        }

        return prior;
    }
}
