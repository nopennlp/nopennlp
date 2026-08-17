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
using System.Linq;
using NOpenNLP.Tools.Ml.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Tests for the <see cref="AbstractEventStream{T}"/> class.
/// </summary>
public class AbstractEventStreamTest
{
    /// <summary>
    /// Checks if the <see cref="AbstractEventStream{T}"/> behavior is correctly
    /// if the <see cref="AbstractEventStream{T}.CreateEvents"/> method
    /// return iterators with events and empty iterators.
    /// </summary>
    [Test]
    public void TestStandardCase()
    {
        List<Result> samples =
        [
            Result.Events,
            Result.Empty,
            Result.Events
        ];

        using TestEventStream eventStream = new(new CollectionObjectStream<Result>(samples));
        int eventCounter = 0;

        while (eventStream.Read() != null)
        {
            eventCounter++;
        }

        ClassicAssert.AreEqual(2, eventCounter);
    }

    /// <summary>
    /// Checks if the <see cref="AbstractEventStream{T}"/> behavior is correctly
    /// if the <see cref="AbstractEventStream{T}.CreateEvents"/> method
    /// only returns empty iterators.
    /// </summary>
    [Test]
    public void TestEmtpyEventStream()
    {
        List<Result> samples = [Result.Empty];

        using (TestEventStream eventStream = new(new CollectionObjectStream<Result>(samples)))
        {
            ClassicAssert.IsNull(eventStream.Read());

            // now check if it can handle multiple empty event iterators
            samples.Add(Result.Empty);
            samples.Add(Result.Empty);
        }

        using (TestEventStream eventStream = new(new CollectionObjectStream<Result>(samples)))
        {
            ClassicAssert.IsNull(eventStream.Read());
        }
    }

    // NOpenNLP: upstream uses an enum here, but CollectionObjectStream requires a
    // reference type because Read() returns null to signal the end of the stream.
    // A sealed class with two singleton instances keeps the same two-valued shape.
    private sealed class Result
    {
        public static readonly Result Events = new();

        public static readonly Result Empty = new();

        private Result()
        {
        }
    }

    /// <summary>
    /// This class extends the <see cref="AbstractEventStream{T}"/> to help
    /// testing the <see cref="AbstractEventStream{T}.Read"/> method.
    /// </summary>
    private sealed class TestEventStream(IObjectStream<Result?> samples)
        : AbstractEventStream<Result>(samples)
    {
        /// <summary>
        /// Creates enumerators for testing.
        /// </summary>
        /// <param name="sample">parameter to specify the output</param>
        /// <returns>it returns an enumerator which contains one
        /// <see cref="Event"/> object if the sample parameter equals
        /// <c>Result.Events</c> or an empty enumerator if the sample
        /// parameter equals <c>Result.Empty</c>.</returns>
        protected override IEnumerator<Event> CreateEvents(Result sample)
        {
            if (Result.Events.Equals(sample))
            {
                List<Event> events = [new Event("test", ["f1", "f2"])];

                return events.GetEnumerator();
            }
            else if (Result.Empty.Equals(sample))
            {
                return Enumerable.Empty<Event>().GetEnumerator();
            }
            else
            {
                // throws runtime exception, execution stops here
                Assert.Fail();

                return null!;
            }
        }
    }
}
