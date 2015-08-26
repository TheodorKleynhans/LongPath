// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LongPathCommon.cs">
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
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    using LongPath.Native;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Contains common functions to work with files and directories using long paths.
    /// </summary>
    /// <remarks>
    /// This class contains methods taken from the <c>http://bcl.codeplex.com/</c> <c>LongPath</c> project.
    /// They were modified to support UNC paths.
    /// </remarks>
    public static class LongPathCommon
    {
        #region Fields

        /// <summary>
        /// Prefix for the long paths.
        /// </summary>
        private const string LongPathPrefix = @"\\?\";

        /// <summary>
        /// Prefix for the long UNC paths.
        /// </summary>
        private const string LongPathUncPrefix = @"\\?\UNC\";

        #endregion // Fields

        #region Public methods

        /// <summary>
        /// Normalizes path (can be longer than <c>MAX_PATH</c>) and adds <c>\\?\</c> long path prefix, if needed.
        /// UNC paths are also supported.
        /// </summary>
        /// <param name="path">Path to be normalized.</param>
        /// <returns>Normalized path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        public static string NormalizePath(string path)
        {
            // This will also validate the path.
            path = LongPathCommon.AddLongPathPrefix(path);

            // Don't forget about the null terminator.
            StringBuilder buffer = new StringBuilder(path.Length + 1);

            uint normalizedPathLength = NativeMethods.GetFullPathName(path, (uint)buffer.Capacity, buffer, IntPtr.Zero);

            // Length returned does not include null terminator.
            if (normalizedPathLength > buffer.Capacity - 1)
            {
                // Resulting path longer than our buffer, so increase it.
                buffer.Capacity = unchecked((int)normalizedPathLength) + 1;
                normalizedPathLength = NativeMethods.GetFullPathName(path, normalizedPathLength, buffer, IntPtr.Zero);
            }

            if (normalizedPathLength == 0)
            {
                throw LongPathCommon.GetExceptionForHr(Marshal.GetHRForLastWin32Error(), path);
            }

            if (normalizedPathLength > NativeMethods.MaxLongPath - 1)
            {
                throw LongPathCommon.GetExceptionForHr(NativeMethods.ErrorFilenameExcedRange, path);
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Adds the <c>\\?\</c> prefix to the <paramref name="path"/> given.
        /// UNC paths are also supported.
        /// </summary>
        /// <param name="path">Path to add prefix to.</param>
        /// <returns>Path with the long prefix added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        public static string AddLongPathPrefix(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Length == 0)
            {
                path = Directory.GetCurrentDirectory();
            }

            // If the prefix is already there, do nothing.
            if (path.StartsWith(LongPathCommon.LongPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            // Path points to a network share.
            if (path.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
            {
                return LongPathCommon.LongPathUncPrefix + path.Substring(2);
            }

            // If we have a relative path, expand it.
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), path);
            }

            return LongPathCommon.LongPathPrefix + path;
        }

        /// <summary>
        /// Removes the long path prefix from the <paramref name="normalizedPath"/> given.
        /// </summary>
        /// <param name="normalizedPath">Normalized path.</param>
        /// <returns>Path without the long path prefix.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="normalizedPath"/> is <see langword="null"/> or empty.</exception>
        public static string RemoveLongPathPrefix(string normalizedPath)
        {
            if (normalizedPath == null)
            {
                throw new ArgumentNullException(nameof(normalizedPath));
            }

            if (normalizedPath.Length == 0)
            {
                return Directory.GetCurrentDirectory();
            }

            // If the prefix is not there, do nothing.
            if (!normalizedPath.StartsWith(LongPathCommon.LongPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath;
            }

            if (normalizedPath.StartsWith(LongPathCommon.LongPathUncPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return @"\\" + normalizedPath.Substring(LongPathCommon.LongPathUncPrefix.Length);
            }

            return normalizedPath.Substring(LongPathCommon.LongPathPrefix.Length);
        }

        /// <summary>
        /// Returns the directory information for the specified path string.
        /// </summary>
        /// <param name="path">The path of a file or directory.</param>
        /// <returns>Directory information for path, or <see langword="null"/>, if path denotes a root directory or is <see langword="null"/>.</returns>
        public static string GetDirectoryName(string path)
        {
            // Get the full path without the long path prefix.
            path = LongPathCommon.RemoveLongPathPrefix(LongPathCommon.NormalizePath(path));

            int lastSeparatorIndex = path.LastIndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            if (lastSeparatorIndex == -1)
            {
                return null;
            }

            // Return null for root directories.
            if (path.Length == 3 && path.EndsWith(@":\", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (path.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase) && lastSeparatorIndex <= 2)
            {
                return null;
            }

            string result = path.Substring(0, lastSeparatorIndex);

            // Append directory separator for root directories.
            if (result.EndsWith(":", StringComparison.OrdinalIgnoreCase))
            {
                return result + Path.DirectorySeparatorChar;
            }

            return result;
        }

        /// <summary>
        /// Determines whether the specified <paramref name="path"/> exists.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><see langword="true"/>, if the <paramref name="path"/> given exists; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
        public static bool Exists(string path)
        {
            bool isDirectory;
            return LongPathCommon.Exists(path, out isDirectory);
        }

        /// <summary>
        /// Determines whether the specified <paramref name="path"/> exists.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">If set to <see langword="true"/>, the <paramref name="path"/> is a directory; otherwise it's a file.</param>
        /// <returns><see langword="true"/>, if the <paramref name="path"/> given exists; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#", Justification = "Out parameter may be helpful here.")]
        public static bool Exists(string path, out bool isDirectory)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            string normalizedPath;
            if (LongPathCommon.TryNormalizeLongPath(path, out normalizedPath))
            {
                EFileAttributes attributes;
                if (LongPathCommon.TryGetFileAttributes(normalizedPath, out attributes))
                {
                    isDirectory = LongPathCommon.IsDirectory(attributes);
                    return true;
                }
            }

            isDirectory = false;
            return false;
        }

        /// <summary>
        /// Sets the attributes for a file or directory.
        /// </summary>
        /// <param name="path">The name of the file whose attributes are to be set.</param>
        /// <param name="attributes">The file attributes to set for the file.</param>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is prefixed with, or contains only a colon character (<c>:</c>).
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> contains a colon character (<c>:</c>) that is not part of a drive label (<c>"C:\"</c>).</exception>
        public static void SetAttributes(string path, FileAttributes attributes)
        {
            string normalizedPath = LongPathCommon.NormalizePath(path);

            if (!NativeMethods.SetFileAttributes(normalizedPath, (EFileAttributes)attributes))
            {
                throw LongPathCommon.GetExceptionForHr(Marshal.GetHRForLastWin32Error(), path);
            }
        }

        /// <summary>
        /// Updates the <paramref name="path"/> timestamps with the values given.
        /// </summary>
        /// <param name="path">Symbolic link to be updated.</param>
        /// <param name="creationTime">Creation time.</param>
        /// <param name="lastAccessTime">Last access time.</param>
        /// <param name="lastWriteTime">Last write time.</param>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="path"/> is prefixed with, or contains only a colon character (<c>:</c>).
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> contains a colon character (<c>:</c>) that is not part of a drive label (<c>"C:\"</c>).</exception>
        /// <remarks>
        /// The major difference between this method and the <see cref="File.SetAttributes"/> is that this method supports
        /// setting attributes for symbolic links (both files and directories), while the <see cref="File.SetAttributes"/>
        /// will set them for a target file, not the link itself.
        /// </remarks>
        public static void SetTimestamps(string path, DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime)
        {
            string normalizedPath = LongPathCommon.NormalizePath(path);

            using (SafeFileHandle handle = NativeMethods.CreateFile(
                normalizedPath,
                EFileAccess.FileWriteAttributes,
                FileShare.ReadWrite,
                IntPtr.Zero,
                FileMode.Open,
                EFileAttributes.OpenReparsePoint | EFileAttributes.BackupSemantics,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    throw LongPathCommon.GetExceptionForHr(Marshal.GetHRForLastWin32Error(), path);
                }

                long creationFileTime   = creationTime.ToLocalTime().ToFileTime();
                long lastAccessFileTime = lastAccessTime.ToLocalTime().ToFileTime();
                long lastWriteFileTime  = lastWriteTime.ToLocalTime().ToFileTime();

                if (!NativeMethods.SetFileTime(handle, ref creationFileTime, ref lastAccessFileTime, ref lastWriteFileTime))
                {
                    throw LongPathCommon.GetExceptionForHr(Marshal.GetHRForLastWin32Error(), path);
                }
            }
        }

        #endregion // Public methods

        #region Internal methods

        /// <summary>
        /// Determines whether the specified attributes belong to a directory.
        /// </summary>
        /// <param name="findData">File or directory data object.</param>
        /// <returns><see langword="true"/> if the specified attributes belong to a directory; otherwise, <see langword="false"/>.
        /// </returns>
        internal static bool IsDirectory(Win32FindData findData)
        {
            return LongPathCommon.IsDirectory(findData.FileAttributes);
        }

        /// <summary>
        /// Determines whether the specified attributes belong to a directory.
        /// </summary>
        /// <param name="attributes">File or directory attributes.</param>
        /// <returns><see langword="true"/> if the specified attributes belong to a directory; otherwise, <see langword="false"/>.
        /// </returns>
        internal static bool IsDirectory(EFileAttributes attributes)
        {
            return attributes.HasFlag(EFileAttributes.Directory);
        }

        /// <summary>
        /// Converts the specified <paramref name="hr"/> to a corresponding the managed exception type.
        /// </summary>
        /// <param name="hr">Error code.</param>
        /// <param name="path">Path to a file or directory.</param>
        /// <returns>Managed exception for the <paramref name="hr"/> given.</returns>
        internal static Exception GetExceptionForHr(int hr, string path)
        {
            string pathWithoutPrefix = LongPathCommon.RemoveLongPathPrefix(path);

            Exception nativeException = Marshal.GetExceptionForHR(hr);

            switch (hr)
            {
                case NativeMethods.ErrorFileNotFound:
                    return new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, "File not found: {0}", pathWithoutPrefix), nativeException);
                case NativeMethods.ErrorPathNotFound:
                    return new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "Path not found: {0}", pathWithoutPrefix), nativeException);
                case NativeMethods.ErrorAccessDenied:
                    return new UnauthorizedAccessException(string.Format(CultureInfo.InvariantCulture, "Access denied to the path: {0}", pathWithoutPrefix), nativeException);
                case NativeMethods.ErrorFilenameExcedRange:
                    return new PathTooLongException(string.Format(CultureInfo.InvariantCulture, "Path is too long: {0}", pathWithoutPrefix), nativeException);
                case NativeMethods.ErrorInvalidDrive:
                    return new DriveNotFoundException(string.Format(CultureInfo.InvariantCulture, "Drive not found: {0}", pathWithoutPrefix), nativeException);
                case NativeMethods.ErrorOperationAborted:
                    return new OperationCanceledException(string.Format(CultureInfo.InvariantCulture, "Operation aborted: {0}", pathWithoutPrefix), nativeException);
                case NativeMethods.ErrorInvalidName:
                    return new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid name: {0}", pathWithoutPrefix), nativeException);
                case NativeMethods.ErrorDirNotEmpty:
                    return new IOException(string.Format(CultureInfo.InvariantCulture, "Directory is not empty: {0}", pathWithoutPrefix), hr);
                case NativeMethods.ErrorFileExists:
                    return new IOException(string.Format(CultureInfo.InvariantCulture, "File already exists: {0}", pathWithoutPrefix), hr);
            }

            // If we don't know about the error code, rely on the Marshal results.
            return nativeException;
        }

        #endregion // Internal methods

        #region Private methods

        /// <summary>
        /// Tries to normalize the <paramref name="path"/> to a long path.
        /// </summary>
        /// <param name="path">Path to be normalized.</param>
        /// <param name="result">Normalized path.</param>
        /// <returns><see langword="true"/>, if the <paramref name="path"/> was successfully normalized; otherwise, <see langword="false"/>.</returns>
        private static bool TryNormalizeLongPath(string path, out string result)
        {
            try
            {
                result = LongPathCommon.NormalizePath(path);
                return true;
            }
            catch (ArgumentException)
            {
                // Ignore.
            }
            catch (PathTooLongException)
            {
                // Ignore.
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Tries to get the file attributes.
        /// </summary>
        /// <param name="normalizedPath">Normalized path to file or directory.</param>
        /// <param name="attributes">Attributes found.</param>
        /// <returns><see langword="true"/>, if the file or directory attributes were successfully retrieved; otherwise, <see langword="false"/>.</returns>
        private static bool TryGetFileAttributes(string normalizedPath, out EFileAttributes attributes)
        {
            // NOTE: Don't be tempted to use FindFirstFile here, it does not work with root directories.
            attributes = NativeMethods.GetFileAttributes(normalizedPath);
            if (attributes == EFileAttributes.Invalid)
            {
                return false;
            }

            return true;
        }

        #endregion // Private methods
    }
}
