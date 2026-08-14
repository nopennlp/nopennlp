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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using NOpenNLP.Tools.Support;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ml.Model;

public abstract class AbstractModel : IMaxentModel
{
    /// <summary>
    /// Mapping between predicates/contexts and an integer representing them.
    /// </summary>
    /// <remarks>
    /// Backed by <see cref="JCG.OrderedDictionary{TKey, TValue}"/> so iteration order
    /// is insertion order, matching the <c>LinkedHashMap</c> upstream adopted in
    /// OPENNLP-1321. Declared as <see cref="IDictionary{TKey, TValue}"/> to keep the
    /// J2N type out of the exposed signature.
    /// </remarks>
    protected IDictionary<string, Context> pmap;

    /// <summary>
    /// The names of the outcomes.
    /// </summary>
    protected string[] outcomeNames;

    /// <summary>
    /// Parameters for the model.
    /// </summary>
    protected EvalParameters evalParams;

    /// <summary>
    /// Prior distribution for this model.
    /// </summary>
    protected IPrior? prior;

    public enum ModelType
    {
        Maxent,
        Perceptron,
        MaxentQn,
        NaiveBayes
    }

    /// <summary>
    /// The type of the model.
    /// </summary>
    protected ModelType modelType;

    protected AbstractModel(Context[] @params, string[] predLabels, IDictionary<string, Context> pmap, string[] outcomeNames)
    {
        this.pmap = pmap;
        this.outcomeNames = outcomeNames;
        this.evalParams = new EvalParameters(@params, outcomeNames.Length);
    }

    protected AbstractModel(Context[] @params, string[] predLabels, string[] outcomeNames)
    {
        Init(predLabels, @params, outcomeNames);
        this.evalParams = new EvalParameters(@params, outcomeNames.Length);
    }

    private void Init(string[] predLabels, Context[] @params, string[] outcomeNames)
    {
        this.pmap = new JCG.OrderedDictionary<string, Context>(predLabels.Length);
        for (int i = 0; i < predLabels.Length; i++)
        {
            pmap.Put(predLabels[i], @params[i]);
        }

        this.outcomeNames = outcomeNames;
    }

    // NOpenNLP: from IMaxentModel interface
    public abstract double[] Eval(string[] context);

    public abstract double[] Eval(string[] context, double[] probs);

    public abstract double[] Eval(string[] context, float[] values);

    /// <summary>
    /// Return the name of the outcome corresponding to the highest likelihood
    /// in the parameter ocs.
    /// </summary>
    /// <param name="ocs">A <see cref="double[]"/> as returned by the <see cref="Eval(string[])"/>
    ///            method.</param>
    /// <returns>   The name of the most likely outcome.</returns>
    public string GetBestOutcome(double[] ocs)
    {
        return outcomeNames[ArrayMath.Argmax(ocs)];
    }

    public virtual ModelType GetModelType()
    {
        return modelType;
    }

    /// <summary>
    /// Return a string matching all the outcome names with all the
    /// probabilities produced by the <see cref="Eval(string[])"/>
    /// method.
    /// </summary>
    /// <param name="ocs">A <see cref="double[]"/> as returned by the
    ///            <see cref="Eval(string[])"/>
    ///            method.</param>
    /// <returns>   String containing outcome names paired with the normalized
    ///            probability (contained in the <paramref name="ocs"/>)
    ///            for each one.</returns>
    public string GetAllOutcomes(double[] ocs)
    {
        if (ocs.Length != outcomeNames.Length)
        {
            return "The double array sent as a parameter to GISModel.getAllOutcomes() " + "must not have been produced by this model.";
        }
        else
        {
            //DecimalFormat df = new DecimalFormat("0.0000");
            StringBuilder sb = new StringBuilder(ocs.Length * 2);
            sb.Append(outcomeNames[0]).Append("[").Append(ocs[0].ToString("0.0000")).Append("]");
            for (int i = 1; i < ocs.Length; i++)
            {
                sb.Append("  ").Append(outcomeNames[i]).Append("[").Append(ocs[i].ToString("0.0000")).Append("]");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Return the name of an outcome corresponding to an int id.
    /// </summary>
    /// <param name="i">An outcome id.</param>
    /// <returns> The name of the outcome associated with that id.</returns>
    public string GetOutcome(int i)
    {
        return outcomeNames[i];
    }

    /// <summary>
    /// Gets the index associated with the String name of the given outcome.
    /// </summary>
    /// <param name="outcome">the String name of the outcome for which the
    ///          index is desired</param>
    /// <returns>the index if the given outcome label exists for this
    ///     model, -1 if it does not.</returns>
    public virtual int GetIndex(string outcome)
    {
        for (int i = 0; i < outcomeNames.Length; i++)
        {
            if (outcomeNames[i].Equals(outcome))
                return i;
        }

        return -1;
    }

    public virtual int NumOutcomes => evalParams.GetNumOutcomes();

    /// <summary>
    /// Provides the fundamental data structures which encode the maxent model
    /// information.  This method will usually only be needed by
    /// GISModelWriters.  The following values are held in the Object array
    /// which is returned by this method:
    /// <list type="bullet">
    /// <item><description>index 0: <see cref="Context"/>[] containing the model
    ///            parameters</description></item>
    /// <item><description>index 1: a dictionary containing the mapping of model predicates
    ///            to unique integers</description></item>
    /// <item><description>index 2: <see cref="string"/>[] containing the names of the outcomes,
    ///            stored in the index of the array which represents their
    ///            unique ids in the model.</description></item>
    /// </list>
    /// </summary>
    /// <returns>An Object[] with the values as described above.</returns>
    public object[] GetDataStructures()
    {
        object[] data = new object[3];
        data[0] = evalParams.GetParams();
        data[1] = pmap;
        data[2] = outcomeNames;
        return data;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(pmap, Arrays.GetHashCode(outcomeNames), evalParams, prior);
    }

    public override bool Equals(object? obj)
    {
        if (obj == this)
        {
            return true;
        }

        if (obj is AbstractModel model)
        {
            return pmap.Equals(model.pmap) && Arrays.Equals(outcomeNames, model.outcomeNames) && Equals(prior, model.prior); // NOpenNLP: using Arrays.Equals since this is just a string array
        }

        return false;
    }
}
