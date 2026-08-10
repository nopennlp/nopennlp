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
using System;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// The Unicode script values that <see cref="Util.Featuregen.StringPattern"/>
/// distinguishes. Java's <c>Character.UnicodeScript</c> defines a value per
/// script, but only these are behaviorally significant here; every other
/// script folds into <see cref="Other"/>.
/// </summary>
internal enum UnicodeScript
{
    Common,
    Latin,
    Han,
    Hiragana,
    Katakana,
    Other
}

/// <summary>
/// Resolves the Unicode script of a code point, matching the classification
/// used by Java's <c>Character.UnicodeScript.of(int)</c>.
/// </summary>
/// <remarks>
/// The range boundaries below are Unicode character-property data from the
/// Unicode Character Database (Scripts.txt, Unicode 8.0 — the version tracked
/// by Java 8, whose behavior this reproduces for model compatibility).
/// <para/>
/// Boundaries whose scripts are not significant to
/// <see cref="Util.Featuregen.StringPattern"/> are folded into
/// <see cref="UnicodeScript.Other"/> and adjacent runs are collapsed, so this
/// table is smaller than the full Unicode script table.
/// <para/>
/// Code points unassigned as of Unicode 8.0 are baked into the table as
/// <see cref="UnicodeScript.Other"/>, matching Java's <c>UNKNOWN</c>. This is
/// deliberately not a runtime <c>CharUnicodeInfo</c> check: .NET ships newer
/// Unicode data, so thousands of code points it considers assigned were not in
/// Unicode 8.0, and each would otherwise resolve to
/// <see cref="UnicodeScript.Common"/> — which
/// <see cref="Util.Featuregen.StringPattern"/> treats specially.
/// </remarks>
internal static class UnicodeScriptResolver
{
    /// <summary>Inclusive start code point of each range, ascending.</summary>
    private static readonly int[] ScriptStarts = new int[]
    {
        0x0000, 0x0041, 0x005B, 0x0061, 0x007B, 0x00AA, 0x00AB, 0x00BA, 0x00BB, 0x00C0, 0x00D7,
        0x00D8, 0x00F7, 0x00F8, 0x02B9, 0x02E0, 0x02E5, 0x02EA, 0x02EC, 0x0300, 0x0374, 0x0375,
        0x037E, 0x037F, 0x0385, 0x0386, 0x0387, 0x0388, 0x0589, 0x058A, 0x060C, 0x060D, 0x061B,
        0x061C, 0x061F, 0x0620, 0x0640, 0x0641, 0x0660, 0x066A, 0x06DD, 0x06DE, 0x0964, 0x0966,
        0x0E3F, 0x0E40, 0x0FD5, 0x0FD9, 0x10FB, 0x10FC, 0x16EB, 0x16EE, 0x1735, 0x1737, 0x1802,
        0x1804, 0x1805, 0x1806, 0x1CD3, 0x1CD4, 0x1CE1, 0x1CE2, 0x1CE9, 0x1CED, 0x1CEE, 0x1CF4,
        0x1CF5, 0x1CF7, 0x1D00, 0x1D26, 0x1D2C, 0x1D5D, 0x1D62, 0x1D66, 0x1D6B, 0x1D78, 0x1D79,
        0x1DBF, 0x1E00, 0x1F00, 0x2000, 0x200C, 0x200E, 0x2065, 0x206A, 0x2071, 0x2072, 0x2074,
        0x207F, 0x2080, 0x208F, 0x2090, 0x209D, 0x20A0, 0x20C0, 0x2100, 0x2126, 0x2127, 0x212A,
        0x212C, 0x2132, 0x2133, 0x214E, 0x214F, 0x2160, 0x2189, 0x218A, 0x2190, 0x23F4, 0x2400,
        0x2427, 0x2440, 0x244B, 0x2460, 0x2700, 0x2701, 0x2800, 0x2900, 0x2B4D, 0x2B50, 0x2B5A,
        0x2C60, 0x2C80, 0x2E00, 0x2E3C, 0x2E80, 0x2E9A, 0x2E9B, 0x2EF4, 0x2F00, 0x2FD6, 0x2FF0,
        0x2FFC, 0x3000, 0x3005, 0x3006, 0x3007, 0x3008, 0x3021, 0x302A, 0x3030, 0x3038, 0x303C,
        0x3040, 0x3041, 0x3097, 0x309B, 0x309D, 0x30A0, 0x30A1, 0x30FB, 0x30FD, 0x3100, 0x3190,
        0x31A0, 0x31C0, 0x31E4, 0x31F0, 0x3200, 0x3220, 0x3260, 0x327F, 0x32D0, 0x32FF, 0x3300,
        0x3358, 0x3400, 0x4DB6, 0x4DC0, 0x4E00, 0x9FF0, 0xA700, 0xA722, 0xA788, 0xA78B, 0xA78F,
        0xA790, 0xA794, 0xA7A0, 0xA7AB, 0xA7F8, 0xA800, 0xA830, 0xA83A, 0xF900, 0xFA6E, 0xFA70,
        0xFADA, 0xFB00, 0xFB07, 0xFD3E, 0xFD40, 0xFDFD, 0xFDFE, 0xFE10, 0xFE1A, 0xFE30, 0xFE53,
        0xFE54, 0xFE67, 0xFE68, 0xFE6C, 0xFEFF, 0xFF00, 0xFF01, 0xFF21, 0xFF3B, 0xFF41, 0xFF5B,
        0xFF66, 0xFF70, 0xFF71, 0xFF9E, 0xFFA0, 0xFFE0, 0xFFE7, 0xFFE8, 0xFFEF, 0xFFF9, 0xFFFE,
        0x10100, 0x10103, 0x10107, 0x10134, 0x10137, 0x10140, 0x10190, 0x1019C, 0x101D0,
        0x101FD, 0x1B000, 0x1B001, 0x1B002, 0x1D000, 0x1D0F6, 0x1D100, 0x1D127, 0x1D129,
        0x1D167, 0x1D16A, 0x1D17B, 0x1D183, 0x1D185, 0x1D18C, 0x1D1AA, 0x1D1AE, 0x1D1DE,
        0x1D300, 0x1D357, 0x1D360, 0x1D372, 0x1D400, 0x1D455, 0x1D456, 0x1D49D, 0x1D49E,
        0x1D4A0, 0x1D4A2, 0x1D4A3, 0x1D4A5, 0x1D4A7, 0x1D4A9, 0x1D4AD, 0x1D4AE, 0x1D4BA,
        0x1D4BB, 0x1D4BC, 0x1D4BD, 0x1D4C4, 0x1D4C5, 0x1D506, 0x1D507, 0x1D50B, 0x1D50D,
        0x1D515, 0x1D516, 0x1D51D, 0x1D51E, 0x1D53A, 0x1D53B, 0x1D53F, 0x1D540, 0x1D545,
        0x1D546, 0x1D547, 0x1D54A, 0x1D551, 0x1D552, 0x1D6A6, 0x1D6A8, 0x1D7CC, 0x1D7CE,
        0x1D800, 0x1F000, 0x1F02C, 0x1F030, 0x1F094, 0x1F0A0, 0x1F0AF, 0x1F0B1, 0x1F0BF,
        0x1F0C1, 0x1F0D0, 0x1F0D1, 0x1F0E0, 0x1F100, 0x1F10B, 0x1F110, 0x1F12F, 0x1F130,
        0x1F16C, 0x1F170, 0x1F19B, 0x1F1E6, 0x1F200, 0x1F201, 0x1F203, 0x1F210, 0x1F23B,
        0x1F240, 0x1F249, 0x1F250, 0x1F252, 0x1F300, 0x1F321, 0x1F330, 0x1F336, 0x1F337,
        0x1F37D, 0x1F380, 0x1F394, 0x1F3A0, 0x1F3C5, 0x1F3C6, 0x1F3CB, 0x1F3E0, 0x1F3F1,
        0x1F400, 0x1F43F, 0x1F440, 0x1F441, 0x1F442, 0x1F4F8, 0x1F4F9, 0x1F4FD, 0x1F500,
        0x1F53E, 0x1F540, 0x1F544, 0x1F550, 0x1F568, 0x1F5FB, 0x1F641, 0x1F645, 0x1F650,
        0x1F680, 0x1F6C6, 0x1F700, 0x1F774, 0x20000, 0x2A6D7, 0x2A700, 0x2B735, 0x2B740,
        0x2B81E, 0x2B820, 0x2CEA2, 0x2F800, 0x2FA1E, 0xE0001, 0xE0002, 0xE0020, 0xE0080
    };

    /// <summary>Script of the range beginning at the same index in <see cref="ScriptStarts"/>.</summary>
    private static readonly UnicodeScript[] Scripts = new UnicodeScript[]
    {
        UnicodeScript.Common, UnicodeScript.Latin, UnicodeScript.Common, UnicodeScript.Latin,
        UnicodeScript.Common, UnicodeScript.Latin, UnicodeScript.Common, UnicodeScript.Latin,
        UnicodeScript.Common, UnicodeScript.Latin, UnicodeScript.Common, UnicodeScript.Latin,
        UnicodeScript.Common, UnicodeScript.Latin, UnicodeScript.Common, UnicodeScript.Latin,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Latin, UnicodeScript.Other, UnicodeScript.Latin, UnicodeScript.Other,
        UnicodeScript.Latin, UnicodeScript.Other, UnicodeScript.Latin, UnicodeScript.Other,
        UnicodeScript.Latin, UnicodeScript.Other, UnicodeScript.Latin, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Latin, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Latin, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Latin,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Latin, UnicodeScript.Common,
        UnicodeScript.Latin, UnicodeScript.Common, UnicodeScript.Latin, UnicodeScript.Common,
        UnicodeScript.Latin, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Latin, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Han, UnicodeScript.Other, UnicodeScript.Han,
        UnicodeScript.Other, UnicodeScript.Han, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Han, UnicodeScript.Common,
        UnicodeScript.Han, UnicodeScript.Common, UnicodeScript.Han, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Han, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Hiragana, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Hiragana, UnicodeScript.Common, UnicodeScript.Katakana,
        UnicodeScript.Common, UnicodeScript.Katakana, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Katakana,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Katakana, UnicodeScript.Common, UnicodeScript.Katakana,
        UnicodeScript.Common, UnicodeScript.Han, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Han, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Latin,
        UnicodeScript.Common, UnicodeScript.Latin, UnicodeScript.Other, UnicodeScript.Latin,
        UnicodeScript.Other, UnicodeScript.Latin, UnicodeScript.Other, UnicodeScript.Latin,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Han,
        UnicodeScript.Other, UnicodeScript.Han, UnicodeScript.Other, UnicodeScript.Latin,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common,
        UnicodeScript.Latin, UnicodeScript.Common, UnicodeScript.Latin, UnicodeScript.Common,
        UnicodeScript.Katakana, UnicodeScript.Common, UnicodeScript.Katakana,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Katakana,
        UnicodeScript.Hiragana, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Hiragana,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Han, UnicodeScript.Other, UnicodeScript.Han, UnicodeScript.Other,
        UnicodeScript.Han, UnicodeScript.Other, UnicodeScript.Han, UnicodeScript.Other,
        UnicodeScript.Han, UnicodeScript.Other, UnicodeScript.Common, UnicodeScript.Other,
        UnicodeScript.Common, UnicodeScript.Other
    };

    /// <summary>
    /// Returns the script of <paramref name="codePoint"/>.
    /// </summary>
    public static UnicodeScript Of(int codePoint)
    {
        if (codePoint < 0 || codePoint > 0x10FFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(codePoint));
        }

        int index = Array.BinarySearch(ScriptStarts, codePoint);
        if (index < 0)
        {
            // Not an exact range start; take the range it falls inside.
            index = -index - 2;
        }

        return Scripts[index];
    }
}
