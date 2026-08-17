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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Entitylinker;

/// <summary>
/// Stores a minimal tuple of information. Intended to be used with <see cref="LinkedSpan{T}"/>.
/// </summary>
public abstract class BaseLink
{
    protected BaseLink()
    {
    }

    protected BaseLink(string? itemParentID, string? itemID, string? itemName, string? itemType)
    {
        ItemParentID = itemParentID;
        ItemID = itemID;
        ItemName = itemName;
        ItemType = itemType;
    }

    /// <summary>
    /// Gets or sets any parent ID for the linked item.
    /// </summary>
    public string? ItemParentID { get; set; }

    /// <summary>
    /// Gets or sets the item id. This field can store, for example, the primary key of a
    /// row in an external/linked database.
    /// </summary>
    public string? ItemID { get; set; }

    /// <summary>
    /// Gets or sets the item name. An item name can be the native name (often a normalized
    /// version of something) from an external linked database.
    /// </summary>
    public string? ItemName { get; set; }

    /// <summary>
    /// Gets or sets the item type. An item type can be the native type from an external
    /// linked database. For instance, a product type or code.
    /// </summary>
    public string? ItemType { get; set; }

    /// <summary>
    /// Gets or sets the scores associated with this link.
    /// </summary>
    public IDictionary<string, double> ScoreMap { get; set; } = new JCG.Dictionary<string, double>();

    public override string ToString() =>
        $"\tBaseLink\n\titemParentID={ItemParentID}, \n\titemID={ItemID}, \n\titemName={ItemName}, \n\titemType={ItemType}, \n\tscoreMap={ScoreMap}\n";

    public override int GetHashCode() => HashCode.Combine(ItemParentID, ItemID, ItemName, ItemType);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        if (obj is BaseLink other)
        {
            return string.Equals(ItemParentID, other.ItemParentID, StringComparison.Ordinal)
                && string.Equals(ItemID, other.ItemID, StringComparison.Ordinal)
                && string.Equals(ItemName, other.ItemName, StringComparison.Ordinal)
                && string.Equals(ItemType, other.ItemType, StringComparison.Ordinal);
        }

        return false;
    }
}
