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
using System.IO;

namespace NOpenNLP.Tools.Util.Eval;

/// <summary>
/// Provides access to training and test partitions for n-fold cross validation.
/// <para/>
/// Cross validation is used to evaluate the performance of a classifier when only
/// training data is available. The training set is split into n parts
/// and the training / evaluation is performed n times on these parts.
/// The training partition always consists of n -1 parts and one part is used for testing.
/// <para/>
/// To use the <see cref="CrossValidationPartitioner{E}"/> a client iterates over the n
/// <see cref="CrossValidationPartitioner{E}.TrainingSampleStream"/>s. Each
/// <see cref="CrossValidationPartitioner{E}.TrainingSampleStream"/> represents
/// one partition and is used first for training and afterwards for testing.
/// The <c>TestSampleStream</c> can be obtained from the
/// <see cref="CrossValidationPartitioner{E}.TrainingSampleStream"/>
/// with the <see cref="CrossValidationPartitioner{E}.TrainingSampleStream.GetTestSampleStream"/> method.
/// </summary>
public class CrossValidationPartitioner<E>
    where E : class
{
    /// <summary>
    /// The <see cref="TestSampleStream"/> iterates over all test elements.
    /// </summary>
    private sealed class TestSampleStream(IObjectStream<E?> sampleStream, int numberOfPartitions, int testIndex)
        : ObjectStreamBase<E?>
    {
        private readonly IObjectStream<E?> sampleStream = sampleStream;

        private readonly int numberOfPartitions = numberOfPartitions;

        private readonly int testIndex = testIndex;

        private int index;

        private bool isPoisened;

        /// <inheritdoc/>
        public override E? Read()
        {
            if (isPoisened)
            {
                throw new InvalidOperationException();
            }

            // skip training samples
            while (index % numberOfPartitions != testIndex)
            {
                sampleStream.Read();
                index++;
            }

            index++;

            return sampleStream.Read();
        }

        /// <summary>
        /// Throws <see cref="NotSupportedException"/>
        /// </summary>
        public override void Reset() => throw new NotSupportedException();

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            sampleStream.Dispose();
            isPoisened = true;
        }

        internal void Poison() => isPoisened = true;
    }

    /// <summary>
    /// The <see cref="TrainingSampleStream"/> which iterates over
    /// all training elements.
    /// <para/>
    /// Note:
    /// After the test sample stream was obtained
    /// the <see cref="TrainingSampleStream"/> must not be used
    /// anymore, otherwise an <see cref="InvalidOperationException"/>
    /// is thrown.
    /// <para/>
    /// The <see cref="IObjectStream{T}"/>s must not be used anymore after the
    /// <see cref="CrossValidationPartitioner{E}"/> was moved
    /// to one of next partitions. If they are called anyway
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    public class TrainingSampleStream(IObjectStream<E?> sampleStream, int numberOfPartitions, int testIndex)
        : ObjectStreamBase<E?>
    {
        private readonly IObjectStream<E?> sampleStream = sampleStream;

        private readonly int numberOfPartitions = numberOfPartitions;

        private readonly int testIndex = testIndex;

        private int index;

        private bool isPoisened;

        private TestSampleStream? testSampleStream;

        /// <inheritdoc/>
        public override E? Read()
        {
            if (testSampleStream != null || isPoisened)
            {
                throw new InvalidOperationException();
            }

            // If the test element is reached skip over it to not include it in
            // the training data
            if (index % numberOfPartitions == testIndex)
            {
                sampleStream.Read();
                index++;
            }

            index++;

            return sampleStream.Read();
        }

        /// <summary>
        /// Resets the training sample. Use this if you need to collect things before
        /// training, for example, to collect induced abbreviations or create a POS
        /// Dictionary.
        /// </summary>
        /// <exception cref="IOException">if there is an error during resetting the stream</exception>
        public override void Reset()
        {
            if (testSampleStream != null || isPoisened)
            {
                throw new InvalidOperationException();
            }

            this.index = 0;
            this.sampleStream.Reset();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            sampleStream.Dispose();
            Poison();
        }

        internal void Poison()
        {
            isPoisened = true;
            testSampleStream?.Poison();
        }

        /// <summary>
        /// Retrieves the <see cref="IObjectStream{T}"/> over the test/evaluations
        /// elements and poisons this <see cref="TrainingSampleStream"/>.
        /// From now on calls to the hasNext and next methods are forbidden
        /// and will raise an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <returns>the test sample stream</returns>
        /// <exception cref="IOException">if there is an error during resetting the stream</exception>
        public IObjectStream<E?> GetTestSampleStream()
        {
            if (isPoisened)
            {
                throw new InvalidOperationException();
            }

            if (testSampleStream == null)
            {
                sampleStream.Reset();
                testSampleStream = new TestSampleStream(sampleStream, numberOfPartitions, testIndex);
            }

            return testSampleStream;
        }
    }

    /// <summary>
    /// An <see cref="IObjectStream{T}"/> over the whole set of data samples which
    /// are used for the cross validation.
    /// </summary>
    // NOpenNLP: made readonly
    private readonly IObjectStream<E?> sampleStream;

    /// <summary>
    /// The number of parts the data is divided into.
    /// </summary>
    private readonly int numberOfPartitions;

    /// <summary>
    /// The index of test part.
    /// </summary>
    private int testIndex;

    /// <summary>
    /// The last handed out <c>TrainingIterator</c>. The reference
    /// is needed to poison the instance to fail fast if it is used
    /// despite the fact that it is forbidden!.
    /// </summary>
    private TrainingSampleStream? lastTrainingSampleStream;

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    public CrossValidationPartitioner(IObjectStream<E?> inElements, int numberOfPartitions)
    {
        this.sampleStream = inElements;
        this.numberOfPartitions = numberOfPartitions;
    }

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    public CrossValidationPartitioner(ICollection<E> elements, int numberOfPartitions)
        : this(new CollectionObjectStream<E>(elements), numberOfPartitions)
    {
    }

    /// <summary>
    /// Checks if there are more partitions available.
    /// </summary>
    public bool HasNext => testIndex < numberOfPartitions;

    /// <summary>
    /// Retrieves the next training and test partitions.
    /// </summary>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    public TrainingSampleStream Next()
    {
        if (HasNext)
        {
            lastTrainingSampleStream?.Poison();

            sampleStream.Reset();

            TrainingSampleStream trainingSampleStream =
                new(sampleStream, numberOfPartitions, testIndex);

            testIndex++;

            lastTrainingSampleStream = trainingSampleStream;

            return trainingSampleStream;
        }
        else
        {
            // NOpenNLP: upstream throws java.util.NoSuchElementException, whose closest
            // .NET counterpart for an exhausted sequence is InvalidOperationException.
            throw new InvalidOperationException();
        }
    }

    /// <inheritdoc/>
    public override string ToString() =>
        "At partition" + (testIndex + 1).ToString(CultureInfo.InvariantCulture) +
        " of " + numberOfPartitions.ToString(CultureInfo.InvariantCulture);
}
