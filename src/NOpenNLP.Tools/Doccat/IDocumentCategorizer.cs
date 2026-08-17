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

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// Interface for classes which categorize documents.
/// </summary>
public interface IDocumentCategorizer
{
    /// <summary>
    /// Categorize the given text provided as tokens along with
    /// the provided extra information
    /// </summary>
    /// <param name="text">the tokens of text to categorize</param>
    /// <param name="extraInformation">extra information</param>
    /// <returns>per category probabilities</returns>
    double[] Categorize(string[] text, IDictionary<string, object> extraInformation);

    /// <summary>
    /// Categorizes the given text, provided in separate tokens.
    /// </summary>
    /// <param name="text">the tokens of text to categorize</param>
    /// <returns>per category probabilities</returns>
    double[] Categorize(string[] text);

    /// <summary>
    /// Gets the best category from previously generated outcome probabilities
    /// </summary>
    /// <param name="outcome">a vector of outcome probabilities</param>
    /// <returns>the best category string</returns>
    string GetBestCategory(double[] outcome);

    /// <summary>
    /// Gets the index of a certain category
    /// </summary>
    /// <param name="category">the category</param>
    /// <returns>an index</returns>
    int GetIndex(string category);

    /// <summary>
    /// Gets the category at a given index
    /// </summary>
    /// <param name="index">the index</param>
    /// <returns>a category</returns>
    string GetCategory(int index);

    /// <summary>
    /// Gets the number of categories
    /// </summary>
    int NumberOfCategories { get; }

    /// <summary>
    /// Gets the name of the category associated with the given probabilities
    /// </summary>
    /// <param name="results">the probabilities of each category</param>
    /// <returns>the name of the outcome</returns>
    string GetAllResults(double[] results);

    /// <summary>
    /// Returns a map in which the key is the category name and the value is the score
    /// </summary>
    /// <param name="text">the input text to classify</param>
    /// <returns>a map with the category as a key and the score as the value.</returns>
    IDictionary<string, double> ScoreMap(string[] text);

    /// <summary>
    /// Gets a map of the scores sorted in ascending order together with their associated categories.
    /// Many categories can have the same score, hence the set as value
    /// </summary>
    /// <param name="text">the input text to classify</param>
    /// <returns>a map with the score as a key. The value is a set of categories with the score.</returns>
    // NOpenNLP: Java's SortedMap is represented by SortedDictionary, which likewise
    // enumerates in ascending key order.
    SortedDictionary<double, ISet<string>> SortedScoreMap(string[] text);
}
