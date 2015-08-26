// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LongPathFile.cs">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;

    using LongPath.Native;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Provides static methods for creating, copying, deleting, moving, and opening of files
    /// with long paths, that is, paths that exceed 259 characters.
    /// </summary>
    /// <remarks>
    /// This class contains methods taken from the <c>http://bcl.codeplex.com/</c> <c>LongPath</c> project.
    /// They were modified to support UNC paths.
    /// </remarks>
    public static class LongPathFile
    {
        #region Fields

        /// <summary>
        /// Default stream buffer size.
        /// </summary>
        private const int DefaultBufferSize = 10 * 1024;

        #endregion // Fields

        #region Public methods

        /// <summary>
        /// Appends lines to a file, and then closes the file. If the specified file does not exist, this method creates a file, writes the specified lines to the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to append the lines to. The file is created if it doesn't already exist.</param>
        /// <param name="contents">The lines to append to the file.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException">Either <paramref name="path"/> or <paramref name="contents"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file specified by <paramref name="path"/> was not found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void AppendAllLines(string path, IEnumerable<string> contents)
        {
            LongPathFile.AppendAllLines(path, contents, Encoding.Default);
        }

        /// <summary>
        /// Appends lines to a file, and then closes the file.
        /// If the specified file does not exist, this method creates a file, writes the specified lines to the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to append the lines to. The file is created if it doesn't already exist.</param>
        /// <param name="contents">The lines to append to the file.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException">Either <paramref name="path"/>, <paramref name="contents"/>, or <paramref name="encoding"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file specified by <paramref name="path"/> was not found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream will not throw on the second Dispose().")]
        public static void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            using (Stream stream       = LongPathFile.Open(path, FileMode.Append, FileAccess.Write, FileShare.None, LongPathFile.DefaultBufferSize, FileOptions.None))
            using (StreamWriter writer = new StreamWriter(stream, encoding))
            {
                foreach (string line in contents)
                {
                    writer.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Appends the specified string to the file, creating the file if it does not already exist.
        /// </summary>
        /// <param name="path">The file to append the specified string to.</param>
        /// <param name="contents">The string to append to the file.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException">Either <paramref name="path"/> or <paramref name="contents"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file specified by <paramref name="path"/> was not found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void AppendAllText(string path, string contents)
        {
            LongPathFile.AppendAllText(path, contents, Encoding.Default);
        }

        /// <summary>
        /// Appends the specified string to the file, creating the file if it does not already exist.
        /// </summary>
        /// <param name="path">The file to append the specified string to.</param>
        /// <param name="contents">The string to append to the file.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException">Either <paramref name="path"/>, <paramref name="contents"/>, or <paramref name="encoding"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file specified by <paramref name="path"/> was not found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream will not throw on the second Dispose().")]
        public static void AppendAllText(string path, string contents, Encoding encoding)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            using (Stream stream       = LongPathFile.Open(path, FileMode.Append, FileAccess.Write, FileShare.None, LongPathFile.DefaultBufferSize, FileOptions.None))
            using (StreamWriter writer = new StreamWriter(stream, encoding))
            {
                writer.Write(contents);
            }
        }

        /// <summary>
        /// Creates a <see cref="StreamWriter"/> that appends UTF-8 encoded text to an existing file, or to a new file if the specified file does not exist.
        /// </summary>
        /// <param name="path">The path to the file to append to.</param>
        /// <returns>A stream writer that appends UTF-8 encoded text to the specified file or to a new file.</returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file specified by <paramref name="path"/> was not found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static StreamWriter AppendText(string path)
        {
            FileStream stream = null;
            try
            {
                stream = LongPathFile.Open(path, FileMode.Append, FileAccess.Write, FileShare.None, LongPathFile.DefaultBufferSize, FileOptions.None);
                return new StreamWriter(stream, Encoding.UTF8);
            }
            catch
            {
                stream?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Copies an existing file to a new file. Overwriting a file of the same name is allowed.
        /// </summary>
        /// <param name="sourceFileName">The file to copy.</param>
        /// <param name="destFileName">The name of the destination file. This cannot be a directory.</param>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="destFileName"/>is read-only.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> specifies a directory.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="sourceFileName"/> or <paramref name="destFileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException"><paramref name="sourceFileName"/> was not found.</exception>
        /// <exception cref="IOException">
        /// <paramref name="destFileName"/> exists.
        ///     <para>-or-</para>
        /// An I/O error has occurred.
        /// </exception>
        /// <exception cref="NotSupportedException"><paramref name="sourceFileName"/> or <paramref name="destFileName"/> is in an invalid format.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest", Justification = "Name was taken from the official API.")]
        public static void Copy(string sourceFileName, string destFileName)
        {
            LongPathFile.Copy(sourceFileName, destFileName, false);
        }

        /// <summary>
        /// Copies an existing file to a new file. Overwriting a file of the same name is allowed.
        /// </summary>
        /// <param name="sourceFileName">The file to copy.</param>
        /// <param name="destFileName">The name of the destination file. This cannot be a directory.</param>
        /// <param name="overwrite"><see langword="true"/> if the destination file can be overwritten; otherwise, <see langword="false"/>.</param>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="destFileName"/>is read-only.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> specifies a directory.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="sourceFileName"/> or <paramref name="destFileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException"><paramref name="sourceFileName"/> was not found.</exception>
        /// <exception cref="IOException">
        /// <paramref name="destFileName"/> exists and overwrite is <see langword="false"/>.
        ///     <para>-or-</para>
        /// An I/O error has occurred.
        /// </exception>
        /// <exception cref="NotSupportedException"><paramref name="sourceFileName"/> or <paramref name="destFileName"/> is in an invalid format.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest", Justification = "Name was taken from the official API.")]
        public static void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            if (sourceFileName == null)
            {
                throw new ArgumentNullException(nameof(sourceFileName));
            }

            if (destFileName == null)
            {
                throw new ArgumentNullException(nameof(destFileName));
            }

            string normalizedSourcePath      = LongPathCommon.NormalizePath(sourceFileName);
            string normalizedDestinationPath = LongPathCommon.NormalizePath(destFileName);

            if (!NativeMethods.CopyFile(normalizedSourcePath, normalizedDestinationPath, !overwrite))
            {
                throw LongPathCommon.GetExceptionForHr(Marshal.GetHRForLastWin32Error(), sourceFileName);
            }
        }

        /// <summary>
        /// Creates or overwrites a file in the specified path.
        /// </summary>
        /// <param name="path">The name of the file.</param>
        /// <returns>A <see cref="FileStream"/> that provides read/write access to the file specified in <paramref name="path"/>.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static FileStream Create(string path)
        {
            return LongPathFile.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, LongPathFile.DefaultBufferSize, FileOptions.None);
        }

        /// <summary>
        /// Creates or overwrites the specified file.
        /// </summary>
        /// <param name="path">The name of the file.</param>
        /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
        /// <returns>A <see cref="FileStream"/> with the specified buffer size that provides read/write access to the file specified in <paramref name="path"/>.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static FileStream Create(string path, int bufferSize)
        {
            return LongPathFile.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, FileOptions.None);
        }

        /// <summary>
        /// Creates or overwrites the specified file, specifying a buffer size and a <see cref="FileOptions"/> value that describes how to create or overwrite the file.
        /// </summary>
        /// <param name="path">The name of the file.</param>
        /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
        /// <param name="options">One of the <see cref="FileOptions"/> values that describes how to create or overwrite the file.</param>
        /// <returns>A new file with the specified buffer size.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static FileStream Create(string path, int bufferSize, FileOptions options)
        {
            return LongPathFile.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, options);
        }

        /// <summary>
        /// Creates or opens a file for writing UTF-8 encoded text.
        /// </summary>
        /// <param name="path">The file to be opened for writing.</param>
        /// <returns>A <see cref="StreamWriter"/> that writes to the specified file using UTF-8 encoding.</returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static StreamWriter CreateText(string path)
        {
            FileStream stream = null;
            try
            {
                stream = LongPathFile.Open(path, FileMode.Create, FileAccess.Write, FileShare.None, LongPathFile.DefaultBufferSize, FileOptions.None);
                return new StreamWriter(stream, Encoding.UTF8);
            }
            catch
            {
                stream?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">The name of the file to be deleted. Wildcard characters are not supported.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="IOException">
        /// The specified file is in use.
        ///     <para>-or-</para>
        /// There is an open handle on the file, and the operating system is Windows XP or earlier.
        /// </exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void Delete(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            string normalizedPath = LongPathCommon.NormalizePath(path);
            if (!NativeMethods.DeleteFile(normalizedPath) && LongPathFile.Exists(path))
            {
                throw LongPathCommon.GetExceptionForHr(Marshal.GetHRForLastWin32Error(), path);
            }
        }

        /// <summary>
        /// Determines whether the specified file exists.
        /// </summary>
        /// <param name="path">The file to check.</param>
        /// <returns><see langword="true"/> if the caller has the required permissions and <paramref name="path"/> contains the name of an existing file; otherwise, <see langword="false"/>.</returns>
        public static bool Exists(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            bool isDirectory;
            if (LongPathCommon.Exists(path, out isDirectory))
            {
                return !isDirectory;
            }

            return false;
        }

        /// <summary>
        /// Gets the <see cref="FileAttributes"/> of the file on the path.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The <see cref="FileAttributes"/> of the file on the path.</returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty, contains only white spaces, or contains invalid characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> represents a file and is invalid, such as being on an unmapped drive, or the file cannot be found.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">This file is being used by another process.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static FileAttributes GetAttributes(string path)
        {
            return new LongPathFileInfo(path).Attributes;
        }

        /// <summary>
        /// Returns the creation date and time of the specified file or directory.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain creation date and time information.</param>
        /// <returns>A <see cref="DateTime"/> structure set to the creation date and time for the specified file or directory. This value is expressed in local time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty, contains only white spaces, or contains invalid characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static DateTime GetCreationTime(string path)
        {
            return new LongPathFileInfo(path).CreationTime;
        }

        /// <summary>
        /// Returns the creation date and time, in coordinated universal time (UTC), of the specified file or directory.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain creation date and time information.</param>
        /// <returns>A <see cref="DateTime"/> structure set to the creation date and time for the specified file or directory. This value is expressed in UTC time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty, contains only white spaces, or contains invalid characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static DateTime GetCreationTimeUtc(string path)
        {
            return new LongPathFileInfo(path).CreationTimeUtc;
        }

        /// <summary>
        /// Returns the date and time the specified file or directory was last accessed.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain access date and time information.</param>
        /// <returns>A <see cref="DateTime"/> structure set to the date and time that the specified file or directory was last accessed. This value is expressed in local time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty, contains only white spaces, or contains invalid characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static DateTime GetLastAccessTime(string path)
        {
            return new LongPathFileInfo(path).LastAccessTime;
        }

        /// <summary>
        /// Returns the date and time, in coordinated universal time (UTC), that the specified file or directory was last accessed.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain access date and time information.</param>
        /// <returns>A <see cref="DateTime"/> structure set to the date and time that the specified file or directory was last accessed. This value is expressed in UTC time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty, contains only white spaces, or contains invalid characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static DateTime GetLastAccessTimeUtc(string path)
        {
            return new LongPathFileInfo(path).LastAccessTimeUtc;
        }

        /// <summary>
        /// Returns the date and time the specified file or directory was last written to.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain write date and time information.</param>
        /// <returns>A <see cref="DateTime"/> structure set to the date and time that the specified file or directory was last written to. This value is expressed in local time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty, contains only white spaces, or contains invalid characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static DateTime GetLastWriteTime(string path)
        {
            return new LongPathFileInfo(path).LastWriteTime;
        }

        /// <summary>
        /// Returns the date and time, in coordinated universal time (UTC), that the specified file or directory was last written to.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain write date and time information.</param>
        /// <returns>A <see cref="DateTime"/> structure set to the date and time that the specified file or directory was last written to. This value is expressed in UTC time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty, contains only white spaces, or contains invalid characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static DateTime GetLastWriteTimeUtc(string path)
        {
            return new LongPathFileInfo(path).LastWriteTimeUtc;
        }

        /// <summary>
        /// Moves a specified file to a new location, providing the option to specify a new file name.
        /// </summary>
        /// <param name="sourceFileName">The name of the file to move.</param>
        /// <param name="destFileName">The new path for the file.</param>
        /// <exception cref="IOException">
        /// The destination file already exists.
        ///     <para>-or-</para>
        /// <paramref name="sourceFileName"/> was not found.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="sourceFileName"/> or <paramref name="destFileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="sourceFileName"/> or <paramref name="destFileName"/> is a zero-length string, contains only white space, or contains invalid characters as defined in <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in sourceFileName or destFileName is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="NotSupportedException"><paramref name="sourceFileName"/> or <paramref name="destFileName"/> is in an invalid format.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest", Justification = "Name was taken from the official API.")]
        public static void Move(string sourceFileName, string destFileName)
        {
            if (sourceFileName == null)
            {
                throw new ArgumentNullException(nameof(sourceFileName));
            }

            if (destFileName == null)
            {
                throw new ArgumentNullException(nameof(destFileName));
            }
            
            string normalizedSourcePath      = LongPathCommon.NormalizePath(sourceFileName);
            string normalizedDestinationPath = LongPathCommon.NormalizePath(destFileName);

            if (!NativeMethods.MoveFile(normalizedSourcePath, normalizedDestinationPath))
            {
                throw LongPathCommon.GetExceptionForHr(Marshal.GetHRForLastWin32Error(), sourceFileName);
            }
        }

        /// <summary>
        /// Opens a <see cref="FileStream"/> on the specified path with read/write access.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
        /// <returns>A <see cref="FileStream"/> opened in the specified mode and path, with read/write access and not shared.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static FileStream Open(string path, FileMode mode)
        {
            return LongPathFile.Open(path, mode, FileAccess.ReadWrite, FileShare.None, LongPathFile.DefaultBufferSize, FileOptions.None);
        }

        /// <summary>
        /// Opens a <see cref="FileStream"/> on the specified path, with the specified mode and access.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
        /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
        /// <returns>An unshared <see cref="FileStream"/> that provides access to the specified file, with the specified mode and access.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static FileStream Open(string path, FileMode mode, FileAccess access)
        {
            return LongPathFile.Open(path, mode, access, FileShare.None, LongPathFile.DefaultBufferSize, FileOptions.None);
        }

        /// <summary>
        /// Opens a <see cref="Open(string,System.IO.FileMode,System.IO.FileAccess)"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
        /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
        /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
        /// <returns>A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return LongPathFile.Open(path, mode, access, share, LongPathFile.DefaultBufferSize, FileOptions.None);
        }

        /// <summary>
        /// Opens an existing file for reading.
        /// </summary>
        /// <param name="path">The file to be opened for reading.</param>
        /// <returns>A read-only <see cref="FileStream"/> on the specified path.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static FileStream OpenRead(string path)
        {
            return LongPathFile.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read, LongPathFile.DefaultBufferSize, FileOptions.None);
        }

        /// <summary>
        /// Opens an existing UTF-8 encoded text file for reading.
        /// </summary>
        /// <param name="path">The file to be opened for reading.</param>
        /// <returns>A <see cref="StreamReader"/> on the specified path.</returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static StreamReader OpenText(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            FileStream stream = null;
            try
            {
                stream = LongPathFile.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read, LongPathFile.DefaultBufferSize, FileOptions.None);
                return new StreamReader(stream, Encoding.UTF8);
            }
            catch
            {
                stream?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Opens an existing file or creates a new file for writing.
        /// </summary>
        /// <param name="path">The file to be opened for writing.</param>
        /// <returns>An unshared <see cref="FileStream"/> object on the specified path with <see cref="FileAccess.Write"/> access.</returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static FileStream OpenWrite(string path)
        {
            return LongPathFile.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, LongPathFile.DefaultBufferSize, FileOptions.None);
        }

        /// <summary>
        /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>A byte array containing the contents of the file.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static byte[] ReadAllBytes(string path)
        {
            byte[] buffer;

            using (FileStream stream = LongPathFile.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read, LongPathFile.DefaultBufferSize, FileOptions.None))
            {
                int length = (int)stream.Length;
                buffer = new byte[length];

                int bytesRead;
                int offset = 0;

                while ((bytesRead = stream.Read(buffer, offset, length - offset)) > 0)
                {
                    offset += bytesRead;
                }
            }

            return buffer;
        }

        /// <summary>
        /// Opens a text file, reads all lines of the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>A string array containing all lines of the file.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static string[] ReadAllLines(string path)
        {
            return LongPathFile.ReadAllLines(path, Encoding.Default);
        }

        /// <summary>
        /// Opens a text file, reads all lines of the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <param name="encoding">The encoding applied to the contents of the file.</param>
        /// <returns>A string array containing all lines of the file.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream will not throw on the second Dispose().")]
        public static string[] ReadAllLines(string path, Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            List<string> lines = new List<string>();

            using (FileStream stream   = LongPathFile.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read, LongPathFile.DefaultBufferSize, FileOptions.None))
            using (StreamReader reader = new StreamReader(stream, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Opens a file, reads all lines of the file with the specified encoding, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>A string containing all lines of the file.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static string ReadAllText(string path)
        {
            return LongPathFile.ReadAllText(path, Encoding.Default);
        }

        /// <summary>
        /// Opens a file, reads all lines of the file with the specified encoding, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <param name="encoding">The encoding applied to the contents of the file.</param>
        /// <returns>A string containing all lines of the file.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream will not throw on the second Dispose().")]
        public static string ReadAllText(string path, Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            using (FileStream stream   = LongPathFile.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read, LongPathFile.DefaultBufferSize, FileOptions.None))
            using (StreamReader reader = new StreamReader(stream, encoding))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Reads the lines of a file.
        /// </summary>
        /// <param name="path">The file to read.</param>
        /// <returns>All the lines of the file, or the lines that are the result of a query.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        public static IEnumerable<string> ReadLines(string path)
        {
            return LongPathFile.ReadLines(path, Encoding.Default);
        }

        /// <summary>
        /// Read the lines of a file that has a specified encoding.
        /// </summary>
        /// <param name="path">The file to read.</param>
        /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
        /// <returns>All the lines of the file, or the lines that are the result of a query.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream will not throw on the second Dispose().")]
        public static IEnumerable<string> ReadLines(string path, Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            using (FileStream stream   = LongPathFile.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read, LongPathFile.DefaultBufferSize, FileOptions.None))
            using (StreamReader reader = new StreamReader(stream, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        /// <summary>
        /// Sets the specified <see cref="FileAttributes"/> of the file on the specified path.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="fileAttributes">A bitwise combination of the enumeration values.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void SetAttributes(string path, FileAttributes fileAttributes)
        {
            new LongPathFileInfo(path).Attributes = fileAttributes;
        }

        /// <summary>
        /// Sets the date and time the file was created.
        /// </summary>
        /// <param name="path">The file for which to set the creation date and time information.</param>
        /// <param name="creationTime">A <see cref="DateTime"/> containing the value to set for the creation date and time of <paramref name="path"/>. This value is expressed in local time.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void SetCreationTime(string path, DateTime creationTime)
        {
            new LongPathFileInfo(path).CreationTime = creationTime;
        }

        /// <summary>
        /// Sets the date and time, in coordinated universal time (UTC), that the file was created.
        /// </summary>
        /// <param name="path">The file for which to set the creation date and time information.</param>
        /// <param name="creationTimeUtc">A <see cref="DateTime"/> containing the value to set for the creation date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
        {
            new LongPathFileInfo(path).CreationTimeUtc = creationTimeUtc;
        }

        /// <summary>
        /// Sets the date and time the specified file was last accessed.
        /// </summary>
        /// <param name="path">The file for which to set the access date and time information.</param>
        /// <param name="lastAccessTime">A <see cref="DateTime"/> containing the value to set for the last access date and time of <paramref name="path"/>. This value is expressed in local time.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void SetLastAccessTime(string path, DateTime lastAccessTime)
        {
            new LongPathFileInfo(path).LastAccessTime = lastAccessTime;
        }

        /// <summary>
        /// Sets the date and time, in coordinated universal time (UTC), that the specified file was last accessed.
        /// </summary>
        /// <param name="path">The file for which to set the access date and time information.</param>
        /// <param name="lastAccessTimeUtc">A <see cref="DateTime"/> containing the value to set for the last access date and time of <paramref name="path"/>. This value is expressed in local time.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        {
            new LongPathFileInfo(path).LastAccessTimeUtc = lastAccessTimeUtc;
        }

        /// <summary>
        /// Sets the date and time that the specified file was last written to.
        /// </summary>
        /// <param name="path">The file for which to set the date and time information.</param>
        /// <param name="lastWriteTime">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in local time.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void SetLastWriteTime(string path, DateTime lastWriteTime)
        {
            new LongPathFileInfo(path).LastWriteTime = lastWriteTime;
        }

        /// <summary>
        /// Sets the date and time, in coordinated universal time (UTC), that the specified file was last written to.
        /// </summary>
        /// <param name="path">The file for which to set the date and time information.</param>
        /// <param name="lastWriteTimeUtc">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        {
            new LongPathFileInfo(path).LastWriteTimeUtc = lastWriteTimeUtc;
        }

        /// <summary>
        /// Creates a new file, writes the specified byte array to the file, and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="bytes">The bytes to write to the file.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/> or the byte array is empty.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "bytes", Justification = "Name was taken from the official API.")]
        public static void WriteAllBytes(string path, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            using (FileStream stream = LongPathFile.Open(path, FileMode.Create, FileAccess.Write, FileShare.None, LongPathFile.DefaultBufferSize, FileOptions.None))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// Creates a new file, writes a collection of strings to the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The lines to write to the file.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException">Either <paramref name="path"/> or <paramref name="contents"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public static void WriteAllLines(string path, IEnumerable<string> contents)
        {
            LongPathFile.WriteAllLines(path, contents, Encoding.Default);
        }

        /// <summary>
        /// Creates a new file by using the specified encoding, writes a collection of strings to the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The lines to write to the file.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one more invalid characters defined by the <see cref="Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="ArgumentNullException">Either <paramref name="path"/>, <paramref name="contents"/>, or <paramref name="encoding"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> specifies a file that is read-only.
        ///     <para>-or-</para>
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is a directory.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream will not throw on the second Dispose().")]
        public static void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            using (FileStream stream   = LongPathFile.Open(path, FileMode.Create, FileAccess.Write, FileShare.None, LongPathFile.DefaultBufferSize, FileOptions.None))
            using (StreamWriter writer = new StreamWriter(stream, encoding))
            {
                foreach (string line in contents)
                {
                    writer.WriteLine(line);
                }
            }
        }

        #endregion // Public methods

        #region Private methods

        /// <summary>
        /// Opens a <see cref="Open(string,System.IO.FileMode,System.IO.FileAccess)"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
        /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
        /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
        /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
        /// <param name="options">One of the <see cref="FileOptions"/> values that describes how to create or overwrite the file.</param>
        /// <returns>A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// The caller does not have the required permission.
        ///     <para>-or-</para>
        /// <paramref name="path"/> specified a file that is read-only.
        ///     <para>-or-</para>
        /// <see cref="FileOptions.Encrypted"/> is specified for options and file encryption is not supported on the current platform.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> is in an invalid format.</exception>
        private static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (bufferSize <= 0)
            {
                bufferSize = LongPathFile.DefaultBufferSize;
            }

            string normalizedPath = LongPathCommon.NormalizePath(path);

            SafeFileHandle handle = LongPathFile.GetFileHandle(normalizedPath, mode, access, share, options);
            return new FileStream(handle, access, bufferSize, options.HasFlag(FileOptions.Asynchronous));
        }

        /// <summary>
        /// Gets the native file handle for the <paramref name="normalizedPath"/>.
        /// </summary>
        /// <param name="normalizedPath">Normalized path to the file.</param>
        /// <param name="mode">
        /// Whether a file is created if one does not exist, and determines whether the contents of existing files are
        /// retained or overwritten.
        /// </param>
        /// <param name="access">Operations that can be performed on the file.</param>
        /// <param name="share">Type of access other threads have to the file.</param>
        /// <param name="options">Additional options.</param>
        /// <returns>Handle to the opened file.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "File handle should be disposed by the caller.")]
        private static SafeFileHandle GetFileHandle(string normalizedPath, FileMode mode, FileAccess access, FileShare share, FileOptions options)
        {
            EFileAccess underlyingAccess = LongPathFile.GetUnderlyingAccess(access);

            SafeFileHandle handle = NativeMethods.CreateFile(normalizedPath, underlyingAccess, share, IntPtr.Zero, mode, (EFileAttributes)options, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                throw LongPathCommon.GetExceptionForHr(Marshal.GetHRForLastWin32Error(), normalizedPath);
            }

            return handle;
        }

        /// <summary>
        /// Converts the <see cref="FileAccess"/> value into the <see cref="EFileAccess"/>.
        /// </summary>
        /// <param name="access">File access value to convert.</param>
        /// <returns>The according <see cref="EFileAccess"/> value.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="access"/> is unknown.</exception>
        private static EFileAccess GetUnderlyingAccess(FileAccess access)
        {
            switch (access)
            {
                case FileAccess.Read:
                    return EFileAccess.GenericRead;
                case FileAccess.Write:
                    return EFileAccess.GenericWrite;
                case FileAccess.ReadWrite:
                    return EFileAccess.GenericRead | EFileAccess.GenericWrite;
                default:
                    throw new ArgumentOutOfRangeException(nameof(access));
            }
        }

        #endregion // Private methods
    }
}
