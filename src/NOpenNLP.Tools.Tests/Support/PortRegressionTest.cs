/*
 * Copyright 2026 NOpenNLP Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using NOpenNLP.Tools.Ml.Naivebayes;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Version = NOpenNLP.Tools.Util.Version;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Regression tests for defects specific to the .NET port, which the upstream
/// Apache OpenNLP test suite does not cover.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. Each test here
/// fails against the pre-fix code, unlike the ported upstream tests, which pass
/// either way.
/// </remarks>
public class PortRegressionTest
{
    /// <summary>
    /// The embedded opennlp.version resource separates its key and value with
    /// ':'. A loader that only split on '=' skipped the entry silently and
    /// reported the fallback development version.
    /// </summary>
    /// <remarks>
    /// Upstream's VersionTest only round-trips CurrentVersion() through Parse(),
    /// so it passes even when the resource fails to load. This pins the value.
    /// </remarks>
    [Test]
    public void TestCurrentVersionIsReadFromEmbeddedResource()
    {
        Version current = Version.CurrentVersion();

        ClassicAssert.AreEqual(1, current.Major);
        ClassicAssert.AreEqual(9, current.Minor);
        ClassicAssert.AreEqual(1, current.Revision);
        ClassicAssert.IsFalse(current.IsSnapshot);
        ClassicAssert.AreEqual("1.9.1", current.ToString());
    }

    /// <summary>
    /// Properties.Load must accept '=', ':' and whitespace as separators, and
    /// treat both '#' and '!' as comment markers, as java.util.Properties does.
    /// </summary>
    [Test]
    public void TestPropertiesAcceptsJavaSeparators()
    {
        var properties = new Properties();
        using (var stream = new System.IO.MemoryStream([.. "# a comment\n! another comment\nequals=1\ncolon: 2\nspace 3\n"u8]))
        {
            properties.Load(stream);
        }

        ClassicAssert.AreEqual("1", properties.GetProperty("equals"));
        ClassicAssert.AreEqual("2", properties.GetProperty("colon"));
        ClassicAssert.AreEqual("3", properties.GetProperty("space"));
        ClassicAssert.AreEqual(3, properties.Count);
    }

    /// <summary>
    /// Sequence declared GetHashCode/Equals as new virtual members rather than
    /// overrides, so it fell back to reference equality and its list fields were
    /// compared by reference.
    /// </summary>
    [Test]
    public void TestSequenceUsesValueEquality()
    {
        Sequence first = new Sequence();
        first.Add("a", 0.5);
        first.Add("b", 0.25);

        Sequence second = new Sequence();
        second.Add("a", 0.5);
        second.Add("b", 0.25);

        ClassicAssert.AreEqual(first, second);
        ClassicAssert.AreEqual(first.GetHashCode(), second.GetHashCode());

        // Reached through an object reference, which is what the missing
        // override actually broke.
        object boxed = second;
        ClassicAssert.IsTrue(first.Equals(boxed));
    }

    /// <summary>
    /// LogProbabilities declared its members as new virtual methods rather than
    /// overrides. Calls through a Probabilities-typed reference — which is how
    /// NaiveBayesModel holds it — therefore ran the base implementation's
    /// linear-space arithmetic instead of the log-space overrides.
    /// </summary>
    [Test]
    public void TestLogProbabilitiesDispatchesThroughBaseReference()
    {
        // Declared as the base type, exactly as NaiveBayesModel declares it.
        Probabilities<string> probabilities = new LogProbabilities<string>();
        probabilities.Set("a", 0.5d);

        // The log-space override stores the logarithm of the value it is given.
        // The base implementation would store 0.5 and return log(0.5) only after
        // taking a logarithm of its own, so this pins which one ran.
        ClassicAssert.AreEqual(System.Math.Log(0.5d), probabilities.GetLog("a"), 1e-12);

        // An absent key is negative infinity in log space, not an exception and
        // not the base class's behavior.
        ClassicAssert.AreEqual(double.NegativeInfinity, probabilities.GetLog("missing"), 0d);

        // Two labels, so normalization is meaningful: log space must still yield
        // a correctly normalized linear probability through the base reference.
        Probabilities<string> pair = new LogProbabilities<string>();
        pair.Set("x", 0.25d);
        pair.Set("y", 0.75d);
        ClassicAssert.AreEqual(0.25d, pair.Get("x").Value, 1e-12);
        ClassicAssert.AreEqual(0.75d, pair.Get("y").Value, 1e-12);
    }
}
