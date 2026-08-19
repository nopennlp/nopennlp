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

namespace NOpenNLP.Tools.Ml.Maxent;

/// <summary>
/// An interface for objects which can deliver a stream of training data to be
/// supplied to an event stream. It is not necessary to use an <see cref="IDataStream"/>
/// in a maxent application, but it can be used to support a wider variety of formats
/// in which your training data can be held.
/// </summary>
public interface IDataStream
{
    /// <summary>
    /// Returns the next slice of data held in this data stream.
    /// </summary>
    /// <returns>the object representing the data which is next in this data stream</returns>
    object NextToken();

    /// <summary>
    /// Tests whether there are any events remaining in this data stream.
    /// </summary>
    /// <returns><c>true</c> if this data stream has more data tokens</returns>
    bool HasNext();
}
