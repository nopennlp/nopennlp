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
using System.Threading.Tasks;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Maxent;

/// <summary>
/// An implementation of Generalized Iterative Scaling. The reference paper
/// for this implementation was Adwait Ratnaparkhi's tech report at the
/// University of Pennsylvania's Institute for Research in Cognitive Science,
/// and is available at <c>ftp://ftp.cis.upenn.edu/pub/ircs/tr/97-08.ps.Z</c>.
/// <para/>
/// The slack parameter used in the above implementation has been removed by default
/// from the computation and a method for updating with Gaussian smoothing has been
/// added per Investigating GIS and Smoothing for Maximum Entropy Taggers, Clark and Curran (2002).
/// <c>http://acl.ldc.upenn.edu/E/E03/E03-1071.pdf</c>
/// Gaussian smoothing can be used by setting <c>useGaussianSmoothing</c> to true.
/// <para/>
/// A prior can be used to train models which converge to the distribution which minimizes the
/// relative entropy between the distribution specified by the empirical constraints of the training
/// data and the specified prior. By default, the uniform distribution is used as the prior.
/// </summary>
public class GISTrainer : AbstractEventTrainer
{
    [Obsolete("Use LOG_LIKELIHOOD_THRESHOLD_PARAM instead.")]
    public const string OLD_LL_THRESHOLD_PARAM = "llthreshold";

    public const string LOG_LIKELIHOOD_THRESHOLD_PARAM = "LLThreshold";
    public const double LOG_LIKELIHOOD_THRESHOLD_DEFAULT = 0.0001;

    public const string MAXENT_VALUE = "MAXENT";

    /// <summary>
    /// If we are using smoothing, this is used as the "number" of times we want
    /// the trainer to imagine that it saw a feature that it actually didn't see.
    /// Defaulted to 0.1.
    /// </summary>
    private const string SMOOTHING_PARAM = "Smoothing";
    private const bool SMOOTHING_DEFAULT = false;
    private const string SMOOTHING_OBSERVATION_PARAM = "SmoothingObservation";
    private const double SMOOTHING_OBSERVATION = 0.1;

    private const string GAUSSIAN_SMOOTHING_PARAM = "GaussianSmoothing";
    private const bool GAUSSIAN_SMOOTHING_DEFAULT = false;
    private const string GAUSSIAN_SMOOTHING_SIGMA_PARAM = "GaussianSmoothingSigma";
    private const double GAUSSIAN_SMOOTHING_SIGMA_DEFAULT = 2.0;

    private double llThreshold = 0.0001;

    /// <summary>
    /// Specifies whether unseen context/outcome pairs should be estimated as occurring very infrequently.
    /// </summary>
    private bool useSimpleSmoothing = false;

    /// <summary>
    /// Specifies whether parameter updates should prefer a distribution of parameters which
    /// is gaussian.
    /// </summary>
    private bool useGaussianSmoothing = false;
    private double sigma = 2.0;

    // If we are using smoothing, this is used as the "number" of
    // times we want the trainer to imagine that it saw a feature that it
    // actually didn't see. Defaulted to 0.1.
    private double _smoothingObservation = 0.1;

    /// <summary>
    /// Number of unique events which occurred in the event set.
    /// </summary>
    private int numUniqueEvents;

    /// <summary>
    /// Number of predicates.
    /// </summary>
    private int numPreds;

    /// <summary>
    /// Number of outcomes.
    /// </summary>
    private int numOutcomes;

    /// <summary>
    /// Records the array of predicates seen in each event.
    /// </summary>
    private int[][]? contexts;

    /// <summary>
    /// The value associated with each context. If null then context values are assumed to be 1.
    /// </summary>
    private float[][]? values;

    /// <summary>
    /// List of outcomes for each event i, in context[i].
    /// </summary>
    private int[] outcomeList = null!;

    /// <summary>
    /// Records the number of times an event has been seen for each event i, in context[i].
    /// </summary>
    private int[]? numTimesEventsSeen;

    /// <summary>
    /// Stores the string names of the outcomes. The GIS only tracks outcomes as
    /// ints, and so this array is needed to save the model to disk and thereby
    /// allow users to know what the outcome was in human understandable terms.
    /// </summary>
    private string[] outcomeLabels = null!;

    /// <summary>
    /// Stores the string names of the predicates. The GIS only tracks predicates
    /// as ints, and so this array is needed to save the model to disk and thereby
    /// allow users to know what the outcome was in human understandable terms.
    /// </summary>
    private string[] predLabels = null!;

    /// <summary>
    /// Stores the observed expected values of the features based on training data.
    /// </summary>
    private MutableContext[]? observedExpects;

    /// <summary>
    /// Stores the estimated parameter value of each predicate during iteration.
    /// </summary>
    private MutableContext[] @params = null!;

    /// <summary>
    /// Stores the expected values of the features based on the current models.
    /// </summary>
    private MutableContext[][]? modelExpects;

    /// <summary>
    /// This is the prior distribution that the model uses for training.
    /// </summary>
    private IPrior prior = null!;

    /// <summary>
    /// Initial probability for all outcomes.
    /// </summary>
    private EvalParameters evalParams = null!;

    /// <summary>
    /// Creates a new <see cref="GISTrainer"/> instance which does not print
    /// progress messages about training to the console.
    /// </summary>
    public GISTrainer()
    {
        printMessages = false;
    }

    /// <summary>
    /// Creates a new <see cref="GISTrainer"/> instance.
    /// </summary>
    /// <param name="printMessages">
    /// Sends progress messages about training to the console when true;
    /// trains silently otherwise.
    /// </param>
    internal GISTrainer(bool printMessages)
    {
        this.printMessages = printMessages;
    }

    public override bool IsSortAndMerge => true;

    public override void Init(TrainingParameters trainingParameters, IDictionary<string, string>? reportMap)
    {
        base.Init(trainingParameters, reportMap);

        // Just in case someone is using "llthreshold" instead of LLThreshold...
        // this warning can be removed in a future version of OpenNLP.
#pragma warning disable CS0618 // the deprecated parameter name is read for backward compatibility
        if (trainingParameters.GetDoubleParameter(OLD_LL_THRESHOLD_PARAM, -1.0) > 0.0)
        {
            Display("WARNING: the training parameter: " + OLD_LL_THRESHOLD_PARAM +
                " has been deprecated.  Please use " +
                LOG_LIKELIHOOD_THRESHOLD_DEFAULT + " instead");
            // if they didn't supply a value for both llthreshold AND LLThreshold copy it over..
            if (trainingParameters.GetDoubleParameter(LOG_LIKELIHOOD_THRESHOLD_PARAM, -1.0) < 0.0)
            {
                trainingParameters.Put(LOG_LIKELIHOOD_THRESHOLD_PARAM,
                    trainingParameters.GetDoubleParameter(OLD_LL_THRESHOLD_PARAM,
                        LOG_LIKELIHOOD_THRESHOLD_DEFAULT));
            }
        }
#pragma warning restore CS0618

        llThreshold = trainingParameters.GetDoubleParameter(LOG_LIKELIHOOD_THRESHOLD_PARAM,
            LOG_LIKELIHOOD_THRESHOLD_DEFAULT);

        useSimpleSmoothing = trainingParameters.GetBooleanParameter(SMOOTHING_PARAM, SMOOTHING_DEFAULT);
        if (useSimpleSmoothing)
        {
            _smoothingObservation =
                trainingParameters.GetDoubleParameter(SMOOTHING_OBSERVATION_PARAM, SMOOTHING_OBSERVATION);
        }

        useGaussianSmoothing =
            trainingParameters.GetBooleanParameter(GAUSSIAN_SMOOTHING_PARAM, GAUSSIAN_SMOOTHING_DEFAULT);
        if (useGaussianSmoothing)
        {
            sigma = trainingParameters.GetDoubleParameter(
                GAUSSIAN_SMOOTHING_SIGMA_PARAM, GAUSSIAN_SMOOTHING_SIGMA_DEFAULT);
        }

        if (useSimpleSmoothing && useGaussianSmoothing)
        {
            throw new InvalidOperationException("Cannot set both Gaussian smoothing and Simple smoothing");
        }
    }

    public override IMaxentModel DoTrain(IDataIndexer indexer)
    {
        int iterations = Iterations;

        int threads = trainingParameters.GetIntParameter(TrainingParameters.THREADS_PARAM, 1);
        return TrainModel(iterations, indexer, threads);
    }

    /// <summary>
    /// Sets whether this trainer will use smoothing while training the model.
    /// This can improve model accuracy, though training will potentially take
    /// longer and use more memory. Model size will also be larger.
    /// </summary>
    public virtual bool Smoothing
    {
        set => useSimpleSmoothing = value;
    }

    /// <summary>
    /// Sets the "number" of times we want the trainer to imagine it saw a
    /// feature that it actually didn't see.
    /// </summary>
    public virtual double SmoothingObservation
    {
        set => _smoothingObservation = value;
    }

    /// <summary>
    /// Turns on Gaussian smoothing and sets its sigma value.
    /// </summary>
    public virtual double GaussianSigma
    {
        set
        {
            useGaussianSmoothing = true;
            sigma = value;
        }
    }

    /// <summary>
    /// Trains a model using the GIS algorithm, assuming 100 iterations and no cutoff.
    /// </summary>
    /// <param name="eventStream">
    /// The event stream holding the data on which this model will be trained.
    /// </param>
    /// <returns>
    /// The newly trained model, which can be used immediately or saved to disk
    /// using a <see cref="Io.GISModelWriter"/>.
    /// </returns>
    public virtual GISModel TrainModel(IObjectStream<Event?> eventStream) =>
        TrainModel(eventStream, 100, 0);

    /// <summary>
    /// Trains a GIS model on the events in the specified event stream, using the specified
    /// number of iterations and the specified count cutoff.
    /// </summary>
    /// <param name="eventStream">A stream of all events.</param>
    /// <param name="iterations">The number of iterations to use for GIS.</param>
    /// <param name="cutoff">The number of times a feature must occur to be included.</param>
    /// <returns>A GIS model trained with the specified parameters.</returns>
    public virtual GISModel TrainModel(IObjectStream<Event?> eventStream, int iterations, int cutoff)
    {
        IDataIndexer indexer = new OnePassDataIndexer();
        TrainingParameters indexingParameters = new();
        indexingParameters.Put(CUTOFF_PARAM, cutoff);
        indexingParameters.Put(ITERATIONS_PARAM, iterations);
        Dictionary<string, string> reportMap = [];
        indexer.Init(indexingParameters, reportMap);
        indexer.Index(eventStream);
        return TrainModel(iterations, indexer);
    }

    /// <summary>
    /// Trains a model using the GIS algorithm.
    /// </summary>
    /// <param name="iterations">The number of GIS iterations to perform.</param>
    /// <param name="di">The data indexer used to compress events in memory.</param>
    public virtual GISModel TrainModel(int iterations, IDataIndexer di) =>
        TrainModel(iterations, di, new UniformPrior(), 1);

    /// <summary>
    /// Trains a model using the GIS algorithm.
    /// </summary>
    /// <param name="iterations">The number of GIS iterations to perform.</param>
    /// <param name="di">The data indexer used to compress events in memory.</param>
    /// <param name="threads">The number of threads to compute model expectations with.</param>
    public virtual GISModel TrainModel(int iterations, IDataIndexer di, int threads) =>
        TrainModel(iterations, di, new UniformPrior(), threads);

    /// <summary>
    /// Trains a model using the GIS algorithm.
    /// </summary>
    /// <param name="iterations">The number of GIS iterations to perform.</param>
    /// <param name="di">The data indexer used to compress events in memory.</param>
    /// <param name="modelPrior">The prior distribution used to train this model.</param>
    /// <param name="threads">The number of threads to compute model expectations with.</param>
    public virtual GISModel TrainModel(int iterations, IDataIndexer di, IPrior modelPrior, int threads)
    {
        if (threads <= 0)
        {
            throw new ArgumentException(
                "threads must be at least one or greater but is " + threads + "!", nameof(threads));
        }

        modelExpects = new MutableContext[threads][];

        // Incorporate all of the needed info
        Display("Incorporating indexed data for training...  \n");
        contexts = di.Contexts;
        values = di.Values;

        // The number of times a predicate occurred in the training data.
        int[] predicateCounts = di.PredCounts;
        numTimesEventsSeen = di.NumTimesEventsSeen;
        numUniqueEvents = contexts.Length;
        prior = modelPrior;

        // determine the correction constant and its inverse
        double correctionConstant = 0;
        for (int ci = 0; ci < contexts.Length; ci++)
        {
            if (values == null || values[ci] == null)
            {
                if (contexts[ci].Length > correctionConstant)
                {
                    correctionConstant = contexts[ci].Length;
                }
            }
            else
            {
                float cl = values[ci][0];
                for (int vi = 1; vi < values[ci].Length; vi++)
                {
                    cl += values[ci][vi];
                }

                if (cl > correctionConstant)
                {
                    correctionConstant = cl;
                }
            }
        }

        Display("done.\n");

        outcomeLabels = di.OutcomeLabels;
        outcomeList = di.OutcomeList;
        numOutcomes = outcomeLabels.Length;

        predLabels = di.PredLabels;
        prior.SetLabels(outcomeLabels, predLabels);
        numPreds = predLabels.Length;

        Display("\tNumber of Event Tokens: " + numUniqueEvents + "\n");
        Display("\t    Number of Outcomes: " + numOutcomes + "\n");
        Display("\t  Number of Predicates: " + numPreds + "\n");

        // set up feature arrays
        float[][] predCount = new float[numPreds][];
        for (int i = 0; i < numPreds; i++)
        {
            predCount[i] = new float[numOutcomes];
        }

        for (int ti = 0; ti < numUniqueEvents; ti++)
        {
            for (int j = 0; j < contexts[ti].Length; j++)
            {
                if (values != null && values[ti] != null)
                {
                    predCount[contexts[ti][j]][outcomeList[ti]] += numTimesEventsSeen[ti] * values[ti][j];
                }
                else
                {
                    predCount[contexts[ti][j]][outcomeList[ti]] += numTimesEventsSeen[ti];
                }
            }
        }

        // A fake "observation" to cover features which are not detected in
        // the data. The default is to assume that we observed "1/10th" of a
        // feature during training.
        double smoothingObservation = _smoothingObservation;

        // Get the observed expectations of the features. Strictly speaking,
        // we should divide the counts by the number of Tokens, but because of
        // the way the model's expectations are approximated in the
        // implementation, this is cancelled out when we compute the next
        // iteration of a parameter, making the extra divisions wasteful.
        @params = new MutableContext[numPreds];
        for (int i = 0; i < modelExpects.Length; i++)
        {
            modelExpects[i] = new MutableContext[numPreds];
        }

        observedExpects = new MutableContext[numPreds];

        // The model does need the correction constant and the correction feature. The correction
        // constant is only needed during training, and the correction feature is not necessary.
        // For compatibility reasons the model contains from now on a correction constant of 1,
        // and a correction param 0.
        evalParams = new EvalParameters(@params, numOutcomes);
        int[] activeOutcomes = new int[numOutcomes];
        int[] outcomePattern;
        int[] allOutcomesPattern = new int[numOutcomes];
        for (int oi = 0; oi < numOutcomes; oi++)
        {
            allOutcomesPattern[oi] = oi;
        }

        int numActiveOutcomes;
        for (int pi = 0; pi < numPreds; pi++)
        {
            numActiveOutcomes = 0;
            if (useSimpleSmoothing)
            {
                numActiveOutcomes = numOutcomes;
                outcomePattern = allOutcomesPattern;
            }
            else
            {
                // determine active outcomes
                for (int oi = 0; oi < numOutcomes; oi++)
                {
                    if (predCount[pi][oi] > 0)
                    {
                        activeOutcomes[numActiveOutcomes] = oi;
                        numActiveOutcomes++;
                    }
                }

                if (numActiveOutcomes == numOutcomes)
                {
                    outcomePattern = allOutcomesPattern;
                }
                else
                {
                    outcomePattern = new int[numActiveOutcomes];
                    Array.Copy(activeOutcomes, 0, outcomePattern, 0, numActiveOutcomes);
                }
            }

            @params[pi] = new MutableContext(outcomePattern, new double[numActiveOutcomes]);
            for (int i = 0; i < modelExpects.Length; i++)
            {
                modelExpects[i][pi] = new MutableContext(outcomePattern, new double[numActiveOutcomes]);
            }

            observedExpects[pi] = new MutableContext(outcomePattern, new double[numActiveOutcomes]);
            for (int aoi = 0; aoi < numActiveOutcomes; aoi++)
            {
                int oi = outcomePattern[aoi];
                @params[pi].SetParameter(aoi, 0.0);
                foreach (MutableContext[] modelExpect in modelExpects)
                {
                    modelExpect[pi].SetParameter(aoi, 0.0);
                }

                if (predCount[pi][oi] > 0)
                {
                    observedExpects[pi].SetParameter(aoi, predCount[pi][oi]);
                }
                else if (useSimpleSmoothing)
                {
                    observedExpects[pi].SetParameter(aoi, smoothingObservation);
                }
            }
        }

        Display("...done.\n");

        // Find the parameters
        if (threads == 1)
        {
            Display("Computing model parameters ...\n");
        }
        else
        {
            Display("Computing model parameters in " + threads + " threads...\n");
        }

        FindParameters(iterations, correctionConstant);

        return new GISModel(@params, predLabels, outcomeLabels);
    }

    /// <summary>
    /// Estimates the model parameters.
    /// </summary>
    private void FindParameters(int iterations, double correctionConstant)
    {
        double prevLL = 0.0;
        double currLL;
        Display("Performing " + iterations + " iterations.\n");
        for (int i = 1; i <= iterations; i++)
        {
            if (i < 10)
            {
                Display("  " + i + ":  ");
            }
            else if (i < 100)
            {
                Display(" " + i + ":  ");
            }
            else
            {
                Display(i + ":  ");
            }

            currLL = NextIteration(correctionConstant);
            if (i > 1)
            {
                if (prevLL > currLL)
                {
                    Console.Error.WriteLine("Model Diverging: loglikelihood decreased");
                    break;
                }

                if (currLL - prevLL < llThreshold)
                {
                    break;
                }
            }

            prevLL = currLL;
        }

        // kill a bunch of these big objects now that we don't need them
        observedExpects = null;
        modelExpects = null;
        numTimesEventsSeen = null;
        contexts = null;
    }

    // modeled on implementation in Zhang Le's maxent kit
    private double GaussianUpdate(int predicate, int oid, double correctionConstant)
    {
        double param = @params[predicate].Parameters[oid];
        double x0 = 0.0;
        double modelValue = modelExpects![0][predicate].Parameters[oid];
        double observedValue = observedExpects![predicate].Parameters[oid];
        for (int i = 0; i < 50; i++)
        {
            double tmp = modelValue * Math.Exp(correctionConstant * x0);
            double f = tmp + (param + x0) / sigma - observedValue;
            double fp = tmp * correctionConstant + 1 / sigma;
            if (fp == 0)
            {
                break;
            }

            double x = x0 - f / fp;
            if (Math.Abs(x - x0) < 0.000001)
            {
                x0 = x;
                break;
            }

            x0 = x;
        }

        return x0;
    }

    /// <summary>
    /// Computes one iteration of GIS and returns the log-likelihood.
    /// </summary>
    private double NextIteration(double correctionConstant)
    {
        // compute contribution of p(a|b_i) for each feature and the new
        // correction parameter
        double loglikelihood = 0.0;
        int numEvents = 0;
        int numCorrect = 0;

        // Each thread gets an equal number of tasks; if the number of tasks
        // is not divisible by the number of threads, the first "leftOver"
        // threads have one extra task.
        int numberOfThreads = modelExpects!.Length;
        int taskSize = numUniqueEvents / numberOfThreads;
        int leftOver = numUniqueEvents % numberOfThreads;

        // NOpenNLP: upstream submits the tasks to an ExecutorCompletionService and
        // takes the results as they finish. The results are only summed, so running
        // them with Parallel.For and reading the per-thread results afterwards is
        // equivalent and avoids managing an executor's lifetime.
        ModelExpectationComputeTask[] tasks = new ModelExpectationComputeTask[numberOfThreads];
        for (int i = 0; i < numberOfThreads; i++)
        {
            tasks[i] = i < leftOver
                ? new ModelExpectationComputeTask(this, i, i * taskSize + i, taskSize + 1)
                : new ModelExpectationComputeTask(this, i, i * taskSize + leftOver, taskSize);
        }

        if (numberOfThreads == 1)
        {
            tasks[0].Run();
        }
        else
        {
            Parallel.For(0, numberOfThreads, i => tasks[i].Run());
        }

        foreach (ModelExpectationComputeTask task in tasks)
        {
            numEvents += task.NumEvents;
            numCorrect += task.NumCorrect;
            loglikelihood += task.Loglikelihood;
        }

        Display(".");

        // merge the results of the computations
        for (int pi = 0; pi < numPreds; pi++)
        {
            int[] activeOutcomes = @params[pi].Outcomes;

            for (int aoi = 0; aoi < activeOutcomes.Length; aoi++)
            {
                for (int i = 1; i < modelExpects.Length; i++)
                {
                    modelExpects[0][pi].UpdateParameter(aoi, modelExpects[i][pi].Parameters[aoi]);
                }
            }
        }

        Display(".");

        // compute the new parameter values
        for (int pi = 0; pi < numPreds; pi++)
        {
            double[] observed = observedExpects![pi].Parameters;
            double[] model = modelExpects[0][pi].Parameters;
            int[] activeOutcomes = @params[pi].Outcomes;
            for (int aoi = 0; aoi < activeOutcomes.Length; aoi++)
            {
                if (useGaussianSmoothing)
                {
                    @params[pi].UpdateParameter(aoi, GaussianUpdate(pi, aoi, correctionConstant));
                }
                else
                {
                    if (model[aoi] == 0)
                    {
                        Console.Error.WriteLine("Model expects == 0 for " + predLabels[pi] + " "
                            + outcomeLabels[aoi]);
                    }

                    @params[pi].UpdateParameter(aoi,
                        (Math.Log(observed[aoi]) - Math.Log(model[aoi])) / correctionConstant);
                }

                foreach (MutableContext[] modelExpect in modelExpects)
                {
                    modelExpect[pi].SetParameter(aoi, 0.0); // re-initialize to 0.0's
                }
            }
        }

        Display(". loglikelihood=" + loglikelihood + "\t" + ((double)numCorrect / numEvents) + "\n");

        return loglikelihood;
    }

    private sealed class ModelExpectationComputeTask(GISTrainer trainer, int threadIndex, int startIndex, int length)
    {
        public int NumEvents { get; private set; }

        public int NumCorrect { get; private set; }

        public double Loglikelihood { get; private set; }

        public void Run()
        {
            double[] modelDistribution = new double[trainer.numOutcomes];

            int[][] contexts = trainer.contexts!;
            float[][]? values = trainer.values;
            int[] numTimesEventsSeen = trainer.numTimesEventsSeen!;
            MutableContext[][] modelExpects = trainer.modelExpects!;

            for (int ei = startIndex; ei < startIndex + length; ei++)
            {
                if (values != null)
                {
                    trainer.prior.LogPrior(modelDistribution, contexts[ei], values[ei]);
                    GISModel.Eval(contexts[ei], values[ei], modelDistribution, trainer.evalParams);
                }
                else
                {
                    trainer.prior.LogPrior(modelDistribution, contexts[ei]);
                    GISModel.Eval(contexts[ei], null, modelDistribution, trainer.evalParams);
                }

                for (int j = 0; j < contexts[ei].Length; j++)
                {
                    int pi = contexts[ei][j];
                    int[] activeOutcomes = modelExpects[threadIndex][pi].Outcomes;
                    for (int aoi = 0; aoi < activeOutcomes.Length; aoi++)
                    {
                        int oi = activeOutcomes[aoi];

                        if (values != null && values[ei] != null)
                        {
                            modelExpects[threadIndex][pi].UpdateParameter(aoi,
                                modelDistribution[oi] * values[ei][j] * numTimesEventsSeen[ei]);
                        }
                        else
                        {
                            modelExpects[threadIndex][pi].UpdateParameter(aoi,
                                modelDistribution[oi] * numTimesEventsSeen[ei]);
                        }
                    }
                }

                Loglikelihood += Math.Log(modelDistribution[trainer.outcomeList[ei]]) * numTimesEventsSeen[ei];

                NumEvents += numTimesEventsSeen[ei];
                if (trainer.printMessages)
                {
                    int max = ArrayMath.Argmax(modelDistribution);
                    if (max == trainer.outcomeList[ei])
                    {
                        NumCorrect += numTimesEventsSeen[ei];
                    }
                }
            }
        }
    }
}
