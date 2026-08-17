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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using J2N.Globalization;
using NOpenNLP.Tools.Support;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Util;

public class TrainingParameters
{
    // TODO: are them duplicated?
    public const string ALGORITHM_PARAM = "Algorithm";
    public const string TRAINER_TYPE_PARAM = "TrainerType";

    public const string ITERATIONS_PARAM = "Iterations";
    public const string CUTOFF_PARAM = "Cutoff";
    public const string THREADS_PARAM = "Threads";

    // NOpenNLP: made readonly
    private readonly JCG.Dictionary<string, object> parameters = new();

    public TrainingParameters()
    {
    }

    public TrainingParameters(TrainingParameters trainingParameters)
    {
        foreach (var entry in trainingParameters.parameters)
        {
            parameters[entry.Key] = entry.Value;
        }
    }

    /// <summary>
    /// Initializes the parameters from a string map, inferring each value's type.
    /// </summary>
    [Obsolete("Use one of the other constructors instead.")]
    public TrainingParameters(IDictionary<string, string> map)
    {
        // try to respect their original type...
        foreach (string key in map.Keys)
        {
            string value = map[key];
            // NOpenNLP: Java parses with Integer.parseInt/Double.parseDouble, which
            // are culture-invariant; int.TryParse/double.TryParse default to the
            // current culture, so InvariantCulture is specified to match. Without it
            // a value such as "1.5" parses differently under a comma-decimal locale.
            //
            // The accept-sets matter here, not just the outcome: which branch claims a
            // value decides the type stored in the map, and that type decides both how
            // GetStringValue renders it into a model manifest and whether
            // GetIntParameter/GetDoubleParameter cast successfully. Integer.parseInt
            // does NOT skip surrounding whitespace, so " 100 " falls through to the
            // double branch in Java and must here too -- NumberStyles.Integer would
            // allow it. Double.parseDouble in turn accepts more than NumberStyles.Float
            // does (a "d"/"f" type suffix and hex-float notation), which
            // ParseJavaDouble handles.
            if (int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
                    out int intValue))
            {
                parameters[key] = intValue;
            }
            else if (ParseJavaDouble(value, out double doubleValue))
            {
                parameters[key] = doubleValue;
            }
            else
            {
                // Because Boolean.parseBoolean() doesn't throw NFE, it just checks the value is either
                // true or yes. So let's see their letters here.
                if (value.ToLowerInvariant() == "true" || value.ToLowerInvariant() == "false")
                {
                    parameters[key] = bool.Parse(value);
                }
                else
                {
                    parameters[key] = value;
                }
            }
        }
    }

    /// <exception cref="IOException">if the parameters cannot be read</exception>
    public TrainingParameters(Stream @in)
    {
        Properties properties = new();
        properties.Load(@in);

        foreach (var entry in properties)
        {
            parameters[(string)entry.Key] = entry.Value;
        }
    }

    /// <summary>
    /// Retrieves the training algorithm name for a given name space.
    /// </summary>
    /// <returns>the name or <c>null</c> if not set.</returns>
    public string? Algorithm(string? namespace_) =>
        parameters.TryGetValue(GetKey(namespace_, ALGORITHM_PARAM), out object? value) ? (string?)value : null;

    /// <summary>
    /// Retrieves the training algorithm name.
    /// </summary>
    /// <returns>the name or <c>null</c> if not set.</returns>
    public string? Algorithm() =>
        parameters.TryGetValue(ALGORITHM_PARAM, out object? value) ? (string?)value : null;

    /// <summary>
    /// Retrieves a map with the training parameters which have the passed name space.
    /// </summary>
    /// <param name="namespace_">the name space, or <c>null</c> for the parameters without one</param>
    /// <returns>a parameter map which can be passed to the train and validate methods.</returns>
    [Obsolete("Use GetObjectSettings(string) instead.")]
    public IDictionary<string, string> GetSettings(string? namespace_)
    {
        JCG.Dictionary<string, string> trainingParams = new();
        string prefix = namespace_ + ".";

        foreach (var entry in parameters)
        {
            string key = entry.Key;

            if (namespace_ != null)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    trainingParams[key[prefix.Length..]] = GetStringValue(entry.Value);
                }
            }
            else
            {
                if (!key.Contains("."))
                {
                    trainingParams[key] = GetStringValue(entry.Value);
                }
            }
        }

        return new ReadOnlyDictionary<string, string>(trainingParams);
    }

    // NOpenNLP: reproduces the accept-set of Java's Double.parseDouble, which is
    // wider than NumberStyles.Float: it takes a trailing "d"/"f" type suffix
    // (J2N's AllowTypeSpecifier) and C99 hex-float notation such as "0x1.8p1"
    // (J2N's HexFloat). HexFloat and Float are mutually exclusive in J2N -- passing
    // both reads every value as hex, turning "100" into 512 -- so the prefix picks
    // which one applies, exactly as Java's grammar does.
    private static bool ParseJavaDouble(string value, out double result)
    {
        string trimmed = value.Trim();
        bool isHex =
            trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("-0x", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("+0x", StringComparison.OrdinalIgnoreCase);

        NumberStyle style = isHex
            ? NumberStyle.HexFloat | NumberStyle.AllowTypeSpecifier
            : NumberStyle.Float | NumberStyle.AllowTypeSpecifier;

        return J2N.Numerics.Double.TryParse(value, style, CultureInfo.InvariantCulture, out result);
    }

    private static string GetStringValue(object value)
    {
        // NOpenNLP: Java's Integer/Double/Boolean.toString are culture-invariant and
        // render booleans lower-case; .NET's ToString() is culture-sensitive and
        // renders booleans as "True"/"False", so both are pinned to match upstream.
        // These strings round-trip back through the deprecated string-map constructor
        // and are written into model manifests, so the exact rendering matters.
        // J2N's "J" format reproduces Java's Double.toString, which differs from
        // .NET's "R": Java renders 1.0 as "1.0" and 1e-5 as "1.0E-5", where "R"
        // gives "1" and "1E-05".
        return value switch
        {
            int i => i.ToString(CultureInfo.InvariantCulture),
            double d => J2N.Numerics.Double.ToString(d, "J", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => (string)value
        };
    }

    /// <summary>
    /// Retrieves all parameters without a name space.
    /// </summary>
    /// <returns>the settings map</returns>
    [Obsolete("Use GetObjectSettings() instead.")]
    public IDictionary<string, string> GetSettings() => GetSettings(null);

    /// <summary>
    /// Retrieves a map with the training parameters which have the passed name space.
    /// </summary>
    /// <param name="namespace_">the name space, or <c>null</c> for the parameters without one</param>
    /// <returns>a parameter map which can be passed to the train and validate methods.</returns>
    public IDictionary<string, object> GetObjectSettings(string? namespace_)
    {
        JCG.Dictionary<string, object> trainingParams = new();
        string prefix = namespace_ + ".";

        foreach (var entry in parameters)
        {
            string key = entry.Key;

            if (namespace_ != null)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    trainingParams[key[prefix.Length..]] = entry.Value;
                }
            }
            else
            {
                if (!key.Contains("."))
                {
                    trainingParams[key] = entry.Value;
                }
            }
        }

        return new ReadOnlyDictionary<string, object>(trainingParams);
    }

    /// <summary>
    /// Retrieves all parameters without a name space.
    /// </summary>
    /// <returns>the settings map</returns>
    public IDictionary<string, object> GetObjectSettings() => GetObjectSettings(null);

    // reduces the params to contain only the params in the name space
    public TrainingParameters GetParameters(string? namespace_)
    {
        TrainingParameters params_ = new();
        IDictionary<string, object> settings = GetObjectSettings(namespace_);

        foreach (var entry in settings)
        {
            string key = entry.Key;
            object value = entry.Value;
            switch (value)
            {
                case int i:
                    params_.Put(key, i);
                    break;
                case double d:
                    params_.Put(key, d);
                    break;
                case bool b:
                    params_.Put(key, b);
                    break;
                default:
                    params_.Put(key, (string)value);
                    break;
            }
        }

        return params_;
    }

    public void PutIfAbsent(string? namespace_, string key, string value) =>
        PutIfAbsentInternal(GetKey(namespace_, key), value);

    public void PutIfAbsent(string key, string value) => PutIfAbsent(null, key, value);

    public void PutIfAbsent(string? namespace_, string key, int value) =>
        PutIfAbsentInternal(GetKey(namespace_, key), value);

    public void PutIfAbsent(string key, int value) => PutIfAbsent(null, key, value);

    public void PutIfAbsent(string? namespace_, string key, double value) =>
        PutIfAbsentInternal(GetKey(namespace_, key), value);

    public void PutIfAbsent(string key, double value) => PutIfAbsent(null, key, value);

    public void PutIfAbsent(string? namespace_, string key, bool value) =>
        PutIfAbsentInternal(GetKey(namespace_, key), value);

    public void PutIfAbsent(string key, bool value) => PutIfAbsent(null, key, value);

    // NOpenNLP: Java's Map.putIfAbsent stores the value when the key is absent or
    // currently mapped to null. This map never holds null values, so testing for
    // an absent key alone matches. J2N's Dictionary has no putIfAbsent equivalent.
    private void PutIfAbsentInternal(string key, object value)
    {
        parameters.TryAdd(key, value);
    }

    public void Put(string? namespace_, string key, string value) =>
        parameters[GetKey(namespace_, key)] = value;

    public void Put(string key, string value) => Put(null, key, value);

    public void Put(string? namespace_, string key, int value) =>
        parameters[GetKey(namespace_, key)] = value;

    public void Put(string key, int value) => Put(null, key, value);

    public void Put(string? namespace_, string key, double value) =>
        parameters[GetKey(namespace_, key)] = value;

    public void Put(string key, double value) => Put(null, key, value);

    public void Put(string? namespace_, string key, bool value) =>
        parameters[GetKey(namespace_, key)] = value;

    public void Put(string key, bool value) => Put(null, key, value);

    /// <exception cref="IOException">if the parameters cannot be written</exception>
    public void Serialize(Stream @out)
    {
        Properties properties = new();

        foreach (var entry in parameters)
        {
            // NOpenNLP-specific: upstream puts the raw Integer/Double/Boolean objects
            // into the Properties, and java.util.Properties.store casts every value to
            // String -- so serializing anything with a non-string value, including
            // defaultParams(), throws ClassCastException on the JVM. Converting here
            // makes those cases round-trip instead of failing, using the same invariant
            // rendering the deprecated string-map constructor reads back. Deliberate
            // deviation: it turns an upstream hard failure into a success, which is
            // what a caller writing a manifest wants.
            properties[entry.Key] = GetStringValue(entry.Value);
        }

        properties.Store(@out, null);
    }

    /// <summary>
    /// Gets a string parameter.
    /// <para/>
    /// An <see cref="InvalidCastException"/> can be thrown if the value is not a <c>string</c>.
    /// </summary>
    public string? GetStringParameter(string key, string? defaultValue) =>
        GetStringParameter(null, key, defaultValue);

    /// <summary>
    /// Gets a string parameter in the specified namespace.
    /// <para/>
    /// An <see cref="InvalidCastException"/> can be thrown if the value is not a <c>string</c>.
    /// </summary>
    public string? GetStringParameter(string? namespace_, string key, string? defaultValue)
    {
        if (!parameters.TryGetValue(GetKey(namespace_, key), out object? value))
        {
            return defaultValue;
        }

        return (string?)value;
    }

    /// <summary>
    /// Gets an integer parameter.
    /// </summary>
    public int GetIntParameter(string key, int defaultValue) =>
        GetIntParameter(null, key, defaultValue);

    /// <summary>
    /// Gets an integer parameter in the specified namespace.
    /// </summary>
    public int GetIntParameter(string? namespace_, string key, int defaultValue)
    {
        if (!parameters.TryGetValue(GetKey(namespace_, key), out object? value))
        {
            return defaultValue;
        }

        // TODO: We have this try-catch for back-compat reason. After removing deprecated flag,
        // we can remove try-catch block and just return (int)value;
        // NOpenNLP: upstream catches ClassCastException from the (Integer) cast; a
        // type test expresses the same fallback without relying on an exception.
        return value is int i ? i : int.Parse((string)value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets a double parameter.
    /// </summary>
    public double GetDoubleParameter(string key, double defaultValue) =>
        GetDoubleParameter(null, key, defaultValue);

    /// <summary>
    /// Gets a double parameter in the specified namespace.
    /// </summary>
    public double GetDoubleParameter(string? namespace_, string key, double defaultValue)
    {
        if (!parameters.TryGetValue(GetKey(namespace_, key), out object? value))
        {
            return defaultValue;
        }

        // TODO: We have this try-catch for back-compat reason. After removing deprecated flag,
        // we can remove try-catch block and just return (double)value;
        // NOpenNLP: the string fallback goes through ParseJavaDouble so it accepts
        // what Java's Double.parseDouble does, and throws otherwise as Java would.
        if (value is double d)
        {
            return d;
        }

        string text = (string)value;
        return ParseJavaDouble(text, out double parsed)
            ? parsed
            : throw new FormatException($"'{text}' is not a valid double value.");
    }

    /// <summary>
    /// Gets a boolean parameter.
    /// </summary>
    public bool GetBooleanParameter(string key, bool defaultValue) =>
        GetBooleanParameter(null, key, defaultValue);

    /// <summary>
    /// Gets a boolean parameter in the specified namespace.
    /// </summary>
    public bool GetBooleanParameter(string? namespace_, string key, bool defaultValue)
    {
        if (!parameters.TryGetValue(GetKey(namespace_, key), out object? value))
        {
            return defaultValue;
        }

        // TODO: We have this try-catch for back-compat reason. After removing deprecated flag,
        // we can remove try-catch block and just return (bool)value;
        // NOpenNLP: Java's Boolean.parseBoolean is case-insensitive and returns false
        // for anything that is not "true", where bool.Parse throws on unrecognized
        // input and rejects surrounding whitespace, so the comparison is done here.
        return value is bool b
            ? b
            : string.Equals((string)value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static TrainingParameters DefaultParams()
    {
        TrainingParameters mlParams = new();
        mlParams.Put(ALGORITHM_PARAM, "MAXENT");
        // NOpenNLP: upstream references EventTrainer.EVENT_VALUE, which is part of the
        // trainer API and is not ported yet. The literal is inlined here and should be
        // replaced with the constant once opennlp.tools.ml.EventTrainer is ported.
        mlParams.Put(TRAINER_TYPE_PARAM, "Event");
        mlParams.Put(ITERATIONS_PARAM, 100);
        mlParams.Put(CUTOFF_PARAM, 5);

        return mlParams;
    }

    internal static string GetKey(string? namespace_, string key) =>
        namespace_ == null ? key : namespace_ + "." + key;
}
