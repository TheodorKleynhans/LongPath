// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Converter.cs">
//   The MIT License (MIT)
//   Copyright (c) 2015 Aleksey Kabanov
// </copyright>
// <summary>
//   Permission is hereby granted, free of charge, to any person obtaining a copy
//   of this software and associated documentation files (the "Software"), to deal
//   in the Software without restriction, including without limitation the rights
//   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//   copies of the Software, and to permit persons to whom the Software is
//   furnished to do so, subject to the following conditions:
//
//   The above copyright notice and this permission notice shall be included in
//   all copies or substantial portions of the Software.
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//   THE SOFTWARE.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LongPath
{
    using System;
    using System.Runtime.InteropServices.ComTypes;

    /// <summary>
    /// Contains helper data conversion methods.
    /// </summary>
    internal static class Converter
    {
        /// <summary>
        /// Converts two <see cref="int"/> to a single <see cref="long"/>.
        /// </summary>
        /// <param name="high">High part.</param>
        /// <param name="low">Low part.</param>
        /// <returns>Value converted.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "int", Justification = "Int identifier is desired here.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "long", Justification = "Long identifier is desired here.")]
        public static long DoubleIntToLong(int high, int low)
        {
            long b = high;

            b = b << 32;
            b = b | unchecked((uint)low);

            return b;
        }

        /// <summary>
        /// Converts the <see cref="FILETIME"/> structure to the <see cref="DateTime"/>.
        /// </summary>
        /// <param name="fileTime">The <see cref="FILETIME"/> structure.</param>
        /// <returns><see cref="DateTime"/> structure converted.</returns>
        public static DateTime FileTimeToDateTime(FILETIME fileTime)
        {
            return DateTime.FromFileTime(Converter.DoubleIntToLong(fileTime.dwHighDateTime, fileTime.dwLowDateTime));
        }
    }
}
