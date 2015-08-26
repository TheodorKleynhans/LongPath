// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LongPathDirectory.cs">
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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security;

    using LongPath.Native;

    /// <summary>
    /// Provides methods for creating, deleting, moving and enumerating directories and
    /// subdirectories with long paths (paths that exceed 259 characters).
    /// </summary>
    /// <remarks>
    /// This class contains methods taken from the <c>http://bcl.codeplex.com/</c> <c>LongPath</c> project.
    /// They were modified to support UNC paths.
    /// </remarks>
    public static class LongPathDirectory
    {
        #region Public methods

        /// <summary>
        /// Creates all directories and subdirectories in the specified path.
        /// </summary>
        /// <param name="path">The directory path to create.</param>
        /// <returns>An object that represents the directory for the specified path.</returns>
        /// <exception cref="IOException">
        /// The directory specified by <paramref name="path"/> is a file.
        ///     <para>-or-</para>
        /// The network name is not known.
        /// </exception>
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
        public static LongPathDirectoryInfo CreateDirectory(string path)
        {
            string normalizedPath = LongPathCommon.NormalizePath(path);

            LongPathDirectoryInfo directoryInfo = new LongPathDirectoryInfo(normalizedPath);
            if (LongPathDirectory.Exists(normalizedPath))
            {
                return directoryInfo;
            }

            // Create list of parent directories not yet created.
            LinkedList<string> directoriesToCreate = new LinkedList<string>();
            directoriesToCreate.AddLast(normalizedPath);

            string parentDirectory = normalizedPath;
            while (!string.IsNullOrEmpty(parentDirectory = LongPathCommon.GetDirectoryName(parentDirectory)))
            {
                string normalizedParent = LongPathCommon.NormalizePath(parentDirectory);
                if (LongPathDirectory.Exists(normalizedParent))
                {
                    break;
                }

                // If we're in the infinite loop.
                if (string.Equals(normalizedParent, directoriesToCreate.First.Value, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unable to get parent for {0}", normalizedParent));
                }

                directoriesToCreate.AddFirst(normalizedParent);
            }

            foreach (string directoryToCreate in directoriesToCreate)
            {
                if (!NativeMethods.CreateDirectory(directoryToCreate, IntPtr.Zero))
                {
                    // To mimic the Directory.CreateDirectory, we don't throw if the directory (not a file) already exists.
                    int hr = Marshal.GetHRForLastWin32Error();
                    if (hr != NativeMethods.ErrorAlreadyExists && !LongPathDirectory.Exists(path))
                    {
                        throw LongPathCommon.GetExceptionForHr(hr, directoryToCreate);
                    }
                }
            }

            return directoryInfo;
        }

        /// <summary>
        /// Deletes an empty directory from a specified path.
        /// </summary>
        /// <param name="path">The name of the empty directory to remove. This directory must be writable or empty.</param>
        /// <exception cref="IOException">
        /// A file with the same name and location specified by <paramref name="path"/> exists.
        ///     <para>-or-</para>
        /// The directory is the application's current working directory.
        ///     <para>-or-</para>
        /// The directory specified by <paramref name="path"/> is not empty.
        ///     <para>-or-</para>
        /// The directory is read-only or contains a read-only file.
        ///     <para>-or-</para>
        /// The directory is being used by another process.
        ///     <para>-or-</para>
        /// There is an open handle on the directory, and the operating system is Windows XP or earlier.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">
        /// <paramref name="path"/> does not exist or could not be found.
        ///     <para>-or-</para>
        /// <paramref name="path"/> refers to a file instead of a directory.
        ///     <para>-or-</para>
        /// The specified path is invalid (for example, it is on an unmapped drive).
        /// </exception>
        public static void Delete(string path)
        {
            string normalizedPath = LongPathCommon.NormalizePath(path);
            if (!NativeMethods.RemoveDirectory(normalizedPath))
            {
                throw LongPathCommon.GetExceptionForHr(Marshal.GetHRForLastWin32Error(), path);
            }
        }

        /// <summary>
        /// Deletes the specified directory and, if indicated, any subdirectories and files in the directory.
        /// </summary>
        /// <param name="path">The name of the directory to remove.</param>
        /// <param name="recursive"><see langword="true"/> to remove directories, subdirectories, and files in path; otherwise, <see langword="false"/>.</param>
        /// <exception cref="IOException">
        /// A file with the same name and location specified by <paramref name="path"/> exists.
        ///     <para>-or-</para>
        /// The directory specified by <paramref name="path"/> is read-only, or <paramref name="recursive"/> is <see langword="false"/> and <paramref name="path"/> is not an empty directory.
        ///     <para>-or-</para>
        /// The directory is the application's current working directory.
        ///     <para>-or-</para>
        /// The directory contains a read-only file.
        ///     <para>-or-</para>
        /// The directory is being used by another process.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">
        /// <paramref name="path"/> does not exist or could not be found.
        ///     <para>-or-</para>
        /// <paramref name="path"/> refers to a file instead of a directory.
        ///     <para>-or-</para>
        /// The specified path is invalid (for example, it is on an unmapped drive).
        /// </exception>
        public static void Delete(string path, bool recursive)
        {
            if (!recursive)
            {
                LongPathDirectory.Delete(path);
                return;
            }

            Stack<string> foldersToDelete = new Stack<string>();
            foldersToDelete.Push(path);

            while (foldersToDelete.Count > 0)
            {
                string dir = foldersToDelete.Peek();
                string[] subDirs = null;

                try
                {
                    subDirs = LongPathDirectory.GetDirectories(dir);
                }
                catch (IOException)
                {
                    // Ignore.
                }

                if (subDirs == null || subDirs.Length == 0)
                {
                    try
                    {
                        foreach (string file in LongPathDirectory.EnumerateFiles(dir))
                        {
                            LongPathFile.Delete(file);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // Ignore.
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Ignore.
                    }

                    LongPathDirectory.Delete(dir);
                    foldersToDelete.Pop();
                }
                else
                {
                    foreach (string subDir in subDirs)
                    {
                        foldersToDelete.Push(subDir);
                    }
                }
            }
        }

        /// <summary>
        /// Returns an enumerable collection of directory names in a specified path.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static IEnumerable<string> EnumerateDirectories(string path)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly, includeDirectories: true, includeFiles: false).Select(e => LongPathCommon.RemoveLongPathPrefix(e.FullName));
        }

        /// <summary>
        /// Returns an enumerable collection of directory names that match a search pattern in a specified path.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/> and that match the specified search pattern.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly, includeDirectories: true, includeFiles: false).Select(e => LongPathCommon.RemoveLongPathPrefix(e.FullName));
        }

        /// <summary>
        /// Returns an enumerable collection of directory names that match a search pattern in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <param name="searchOption">
        /// One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.<br/>
        /// The default value is <see cref="SearchOption.TopDirectoryOnly"/>.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names (including paths) for the directories in the directory specified by path and that match the specified search pattern and option.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchPattern"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, searchPattern, searchOption, includeDirectories: true, includeFiles: false).Select(e => LongPathCommon.RemoveLongPathPrefix(e.FullName));
        }

        /// <summary>
        /// Returns an enumerable collection of file names in a specified path.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <returns>An enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static IEnumerable<string> EnumerateFiles(string path)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly, includeDirectories: false, includeFiles: true).Select(e => LongPathCommon.RemoveLongPathPrefix(e.FullName));
        }

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern in a specified path.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <returns>
        /// An enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/> and that match the specified search pattern.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly, includeDirectories: false, includeFiles: true).Select(e => LongPathCommon.RemoveLongPathPrefix(e.FullName));
        }

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <param name="searchOption">
        /// One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.<br/>
        /// The default value is <see cref="SearchOption.TopDirectoryOnly"/>.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/> and that match the specified search pattern and option.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchPattern"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, searchPattern, searchOption, includeDirectories: false, includeFiles: true).Select(e => LongPathCommon.RemoveLongPathPrefix(e.FullName));
        }

        /// <summary>
        /// Returns an enumerable collection of file-system entries in a specified path.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <returns>An enumerable collection of file-system entries in the directory specified by <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static IEnumerable<string> EnumerateFileSystemEntries(string path)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly, includeDirectories: true, includeFiles: true).Select(e => LongPathCommon.RemoveLongPathPrefix(e.FullName));
        }

        /// <summary>
        /// Returns an enumerable collection of file-system entries that match a search pattern in a specified path.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <returns>
        /// An enumerable collection of file-system entries in the directory specified by <paramref name="path"/> and that match the specified search pattern.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly, includeDirectories: true, includeFiles: true).Select(e => LongPathCommon.RemoveLongPathPrefix(e.FullName));
        }

        /// <summary>
        /// Returns an enumerable collection of file names and directory names that match a search pattern in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <param name="searchOption">
        /// One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.<br/>
        /// The default value is <see cref="SearchOption.TopDirectoryOnly"/>.
        /// </param>
        /// <returns>
        /// An enumerable collection of file-system entries in the directory specified by <paramref name="path"/> and that match the specified search pattern and option.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchPattern"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, searchPattern, searchOption, includeDirectories: true, includeFiles: true).Select(e => LongPathCommon.RemoveLongPathPrefix(e.FullName));
        }

        /// <summary>
        /// Determines whether the given path refers to an existing directory on disk.
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <returns>
        /// <see langword="true"/> if path refers to an existing directory; otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>
        /// Note that this method will return false if any error occurs while trying to determine
        /// if the specified directory exists. This includes situations that would normally result in
        /// thrown exceptions including (but not limited to) passing in a directory name with invalid
        /// or too many characters, an I/O error such as a failing or missing disk, or if the caller
        /// does not have Windows or Code Access Security (CAS) permissions to to read the directory.
        /// </remarks>
        public static bool Exists(string path)
        {
            bool isDirectory;
            if (LongPathCommon.Exists(path, out isDirectory))
            {
                return isDirectory;
            }

            return false;
        }

        /// <summary>
        /// Gets the creation date and time of a directory.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <returns>A structure that is set to the creation date and time for the specified directory. This value is expressed in local time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        public static DateTime GetCreationTime(string path)
        {
            return new LongPathDirectoryInfo(path).CreationTime;
        }

        /// <summary>
        /// Gets the creation date and time, in Coordinated Universal Time (UTC) format, of a directory.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <returns>A structure that is set to the creation date and time for the specified directory. This value is expressed in UTC time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        public static DateTime GetCreationTimeUtc(string path)
        {
            return new LongPathDirectoryInfo(path).CreationTimeUtc;
        }

        /// <summary>
        /// Gets the current working directory of the application.
        /// </summary>
        /// <returns>A string that contains the path of the current working directory, and does not end with a backslash (<c>\</c>).</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Mimicking System.IO.Directory method.")]
        public static string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Gets the names of subdirectories (including their paths) in the specified directory.
        /// </summary>
        /// <param name="path">The path for which an array of subdirectory names is returned.</param>
        /// <returns>An array of the full names (including paths) of subdirectories in the specified path.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static string[] GetDirectories(string path)
        {
            return LongPathDirectory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly).ToArray();
        }

        /// <summary>
        /// Gets the names of subdirectories (including their paths) that match the specified search pattern in the current directory.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <returns>An array of the full names (including paths) of the subdirectories that match the search pattern.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static string[] GetDirectories(string path, string searchPattern)
        {
            return LongPathDirectory.EnumerateDirectories(path, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
        }

        /// <summary>
        /// Gets the names of the subdirectories (including their paths) that match the specified search pattern in the current directory, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <param name="searchOption">
        /// One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.<br/>
        /// The default value is <see cref="SearchOption.TopDirectoryOnly"/>.
        /// </param>
        /// <returns>An array of the full names (including paths) of the subdirectories that match the search pattern.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchPattern"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            return LongPathDirectory.EnumerateDirectories(path, searchPattern, searchOption).ToArray();
        }

        /// <summary>
        /// Returns the volume information, root information, or both for the specified path.
        /// </summary>
        /// <param name="path">The path of a file or directory.</param>
        /// <returns>A string that contains the volume information, root information, or both for the specified path.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        public static string GetDirectoryRoot(string path)
        {
            path = LongPathCommon.RemoveLongPathPrefix(LongPathCommon.NormalizePath(path));

            // Try to existing method first.
            try
            {
                return Directory.GetDirectoryRoot(path);
            }
            catch (PathTooLongException)
            {
                // Ignore.
            }

            // Root directory for UNC paths is its parent directory.
            if (path.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
            {
                // Path has separators (validated by the Directory.GetDirectoryRoot).
                int idx = path.TrimEnd(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).LastIndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                return path.Substring(0, idx);
            }

            // Shorten path, so the existing API will work.
            return Directory.GetDirectoryRoot(path.Substring(0, 200));
        }

        /// <summary>
        /// Returns the names of files (including their paths) in the specified directory.
        /// </summary>
        /// <param name="path">The directory from which to retrieve the files.</param>
        /// <returns>An array of the full names (including paths) for the files in the specified directory.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static string[] GetFiles(string path)
        {
            return LongPathDirectory.EnumerateFiles(path).ToArray();
        }

        /// <summary>
        /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <returns>An array of the full names (including paths) for the files in the specified directory that match the specified search pattern.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static string[] GetFiles(string path, string searchPattern)
        {
            return LongPathDirectory.EnumerateFiles(path, searchPattern).ToArray();
        }

        /// <summary>
        /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory, using a value to determine whether to search subdirectories.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <param name="searchOption">
        /// One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.<br/>
        /// The default value is <see cref="SearchOption.TopDirectoryOnly"/>.
        /// </param>
        /// <returns>An array of the full names (including paths) for the files in the specified directory that match the specified search pattern and option.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchPattern"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return LongPathDirectory.EnumerateFiles(path, searchPattern, searchOption).ToArray();
        }

        /// <summary>
        /// Returns the names of all files and subdirectories in the specified directory.
        /// </summary>
        /// <param name="path">The directory for which file and subdirectory names are returned.</param>
        /// <returns>An array of the names of files and subdirectories in the specified directory.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static string[] GetFileSystemEntries(string path)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path).ToArray();
        }

        /// <summary>
        /// Returns an array of file system entries that match the specified search criteria.
        /// </summary>
        /// <param name="path">The path to be searched.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <returns>An array of file system entries that match the specified search criteria.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static string[] GetFileSystemEntries(string path, string searchPattern)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, searchPattern).ToArray();
        }

        /// <summary>
        /// Gets an array of all the file names and directory names that match a search pattern in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
        /// <param name="searchOption">
        /// One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.<br/>
        /// The default value is <see cref="SearchOption.TopDirectoryOnly"/>.
        /// </param>
        /// <returns>An array of file system entries that match the specified search criteria.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> does not contain a valid pattern.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        ///     <para>-or-</para>
        /// <paramref name="searchPattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchPattern"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(path, searchPattern, searchOption).ToArray();
        }

        /// <summary>
        /// Returns the date and time the specified file or directory was last accessed.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain access date and time information.</param>
        /// <returns>A structure that is set to the date and time the specified file or directory was last accessed. This value is expressed in local time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        public static DateTime GetLastAccessTime(string path)
        {
            return new LongPathDirectoryInfo(path).LastAccessTime;
        }

        /// <summary>
        /// Returns the date and time, in Coordinated Universal Time (UTC) format, that the specified file or directory was last accessed.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain access date and time information.</param>
        /// <returns>A structure that is set to the date and time the specified file or directory was last accessed. This value is expressed in UTC time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        public static DateTime GetLastAccessTimeUtc(string path)
        {
            return new LongPathDirectoryInfo(path).LastAccessTimeUtc;
        }

        /// <summary>
        /// Returns the date and time the specified file or directory was last written to.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain modification date and time information.</param>
        /// <returns>A structure that is set to the date and time the specified file or directory was last written to. This value is expressed in local time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        public static DateTime GetLastWriteTime(string path)
        {
            return new LongPathDirectoryInfo(path).LastWriteTime;
        }

        /// <summary>
        /// Returns the date and time, in Coordinated Universal Time (UTC) format, that the specified file or directory was last written to.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain modification date and time information.</param>
        /// <returns>A structure that is set to the date and time the specified file or directory was last written to. This value is expressed in UTC time.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        public static DateTime GetLastWriteTimeUtc(string path)
        {
            return new LongPathDirectoryInfo(path).LastWriteTimeUtc;
        }

        /// <summary>
        /// Retrieves the names of the logical drives on this computer in the form "<c>&lt;drive letter&gt;:\</c>".
        /// </summary>
        /// <returns>The logical drives on this computer.</returns>
        /// <exception cref="IOException">An I/O error occurred (for example, a disk error).</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public static string[] GetLogicalDrives()
        {
            return Directory.GetLogicalDrives();
        }

        /// <summary>
        /// Retrieves the parent directory of the specified path, including both absolute and relative paths.
        /// </summary>
        /// <param name="path">The path for which to retrieve the parent directory.</param>
        /// <returns>The parent directory, or <see langword="null"/> if <paramref name="path"/> is the root directory, including the root of a UNC server or share name.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        public static LongPathDirectoryInfo GetParent(string path)
        {
            path = LongPathCommon.RemoveLongPathPrefix(LongPathCommon.NormalizePath(path)).TrimEnd(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return new LongPathDirectoryInfo(LongPathCommon.GetDirectoryName(path));
        }

        /// <summary>
        /// Sets the creation date and time for the specified file or directory.
        /// </summary>
        /// <param name="path">The file or directory for which to set the creation date and time information.</param>
        /// <param name="creationTime">An object that contains the value to set for the creation date and time of <paramref name="path"/>. This value is expressed in local time.</param>
        /// <exception cref="FileNotFoundException">The specified path was not found.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="creationTime"/> specifies a value outside the range of dates or times permitted for this operation.</exception>
        public static void SetCreationTime(string path, DateTime creationTime)
        {
            new LongPathDirectoryInfo(path).CreationTime = creationTime;
        }

        /// <summary>
        /// Sets the creation date and time, in Coordinated Universal Time (UTC) format, for the specified file or directory.
        /// </summary>
        /// <param name="path">The file or directory for which to set the creation date and time information.</param>
        /// <param name="creationTimeUtc">An object that contains the value to set for the creation date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
        /// <exception cref="FileNotFoundException">The specified path was not found.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="creationTimeUtc"/> specifies a value outside the range of dates or times permitted for this operation.</exception>
        public static void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
        {
            new LongPathDirectoryInfo(path).CreationTimeUtc = creationTimeUtc;
        }

        /// <summary>
        /// Sets the application's current working directory to the specified directory.
        /// </summary>
        /// <param name="path">The path to which the current working directory is set.</param>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="FileNotFoundException">The specified path was not found.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified directory was not found.</exception>
        public static void SetCurrentDirectory(string path)
        {
            Directory.SetCurrentDirectory(path);
        }

        /// <summary>
        /// Sets the date and time the specified file or directory was last accessed.
        /// </summary>
        /// <param name="path">The file or directory for which to set the access date and time information.</param>
        /// <param name="lastAccessTime">An object that contains the value to set for the access date and time of <paramref name="path"/>. This value is expressed in local time.</param>
        /// <exception cref="FileNotFoundException">The specified path was not found.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lastAccessTime"/> specifies a value outside the range of dates or times permitted for this operation.</exception>
        public static void SetLastAccessTime(string path, DateTime lastAccessTime)
        {
            new LongPathDirectoryInfo(path).LastAccessTime = lastAccessTime;
        }

        /// <summary>
        /// Sets the date and time, in Coordinated Universal Time (UTC) format, that the specified file or directory was last accessed.
        /// </summary>
        /// <param name="path">The file or directory for which to set the access date and time information.</param>
        /// <param name="lastAccessTimeUtc">An object that contains the value to set for the access date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
        /// <exception cref="FileNotFoundException">The specified path was not found.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lastAccessTimeUtc"/> specifies a value outside the range of dates or times permitted for this operation.</exception>
        public static void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        {
            new LongPathDirectoryInfo(path).LastAccessTimeUtc = lastAccessTimeUtc;
        }

        /// <summary>
        /// Sets the date and time a directory was last written to.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <param name="lastWriteTime">The date and time the directory was last written to. This value is expressed in local time.</param>
        /// <exception cref="FileNotFoundException">The specified path was not found.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lastWriteTime"/> specifies a value outside the range of dates or times permitted for this operation.</exception>
        public static void SetLastWriteTime(string path, DateTime lastWriteTime)
        {
            new LongPathDirectoryInfo(path).LastWriteTime = lastWriteTime;
        }

        /// <summary>
        /// Sets the date and time, in Coordinated Universal Time (UTC) format, that a directory was last written to.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <param name="lastWriteTimeUtc">The date and time the directory was last written to. This value is expressed in UTC time.</param>
        /// <exception cref="FileNotFoundException">The specified path was not found.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lastWriteTimeUtc"/> specifies a value outside the range of dates or times permitted for this operation.</exception>
        public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        {
            new LongPathDirectoryInfo(path).LastWriteTimeUtc = lastWriteTimeUtc;
        }

        #endregion // Public methods

        #region Internal methods

        /// <summary>
        /// Returns a enumerable containing the file and directory information of the specified directory
        /// that match the specified search pattern, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">A <see cref="String"/> containing the path of the directory to search.</param>
        /// <param name="searchPattern">
        /// A <see cref="String"/> containing search pattern to match against the names of the
        /// files and directories in <paramref name="path"/>, otherwise, <see langword="null"/>
        /// or an empty string ("") to use the default search pattern, "*".
        /// </param>
        /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
        /// <param name="includeDirectories">If set to <see langword="true"/>, directories will be included in the result.</param>
        /// <param name="includeFiles">If set to <see langword="true"/>, files will be included in the result.</param>
        /// <returns>
        /// A <see cref="IEnumerable{T}"/> containing the file and directory information within <paramref name="path"/> that match <paramref name="searchPattern"/>.
        /// </returns>
        internal static IEnumerable<LongPathFileSystemInfo> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption, bool includeDirectories, bool includeFiles)
        {
            // First check whether the specified path refers to a directory and exists.
            bool isDirectory;
            if (!LongPathCommon.Exists(path, out isDirectory))
            {
                throw new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "Path {0} is invalid.", path));
            }

            if (!isDirectory)
            {
                throw new IOException(string.Format(CultureInfo.InvariantCulture, "{0} is a file.", path));
            }

            if (searchPattern == null)
            {
                throw new ArgumentNullException(nameof(searchPattern));
            }

            return LongPathDirectory.EnumerateFileSystemIterator(path, searchPattern, searchOption, includeDirectories, includeFiles)
                .Select(d => LongPathCommon.IsDirectory(d) ? (LongPathFileSystemInfo)new LongPathDirectoryInfo(d) : new LongPathFileInfo(d));
        }

        /// <summary>
        /// Returns a enumerable containing the file and directory information of the specified directory
        /// that match the specified search pattern.
        /// </summary>
        /// <param name="path">A <see cref="String"/> containing the path of the directory to search.</param>
        /// <param name="searchPattern">Search pattern.</param>
        /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
        /// <param name="includeDirectories">If set to <see langword="true"/>, directories will be included in the result.</param>
        /// <param name="includeFiles">If set to <see langword="true"/>, files will be included in the result.</param>
        /// <returns>
        /// A <see cref="IEnumerable{T}"/> containing the file and directory information within <paramref name="path"/> that match <paramref name="searchPattern"/>.
        /// </returns>
        internal static IEnumerable<Win32FindData> EnumerateFileSystemIterator(string path, string searchPattern, SearchOption searchOption, bool includeDirectories, bool includeFiles)
        {
            // NOTE: Any exceptions thrown from this method are thrown on a call to IEnumerator<string>.MoveNext().
            string normalizedPath          = LongPathCommon.NormalizePath(path);
            string normalizedSearchPattern = LongPathDirectory.NormalizeSearchPattern(searchPattern);

            Queue<string> directoriesToEnumerate = new Queue<string>();
            directoriesToEnumerate.Enqueue(normalizedPath);

            while (directoriesToEnumerate.Count > 0)
            {
                string currentDirectory = directoriesToEnumerate.Dequeue();

                Win32FindData findData;
                using (SafeFindHandle handle = LongPathDirectory.BeginFind(Path.Combine(currentDirectory, normalizedSearchPattern), out findData))
                {
                    if (handle == null)
                    {
                        yield break;
                    }

                    do
                    {
                        string currentFileName = findData.FileName;
                        findData.FileName = Path.Combine(currentDirectory, currentFileName);

                        if (LongPathCommon.IsDirectory(findData))
                        {
                            // Don't include '.' and '..' directories.
                            if (LongPathDirectory.IsCurrentOrParentDirectory(currentFileName))
                            {
                                continue;
                            }

                            // If we need to recursively enumerate path, add the current child to the list of pending directories.
                            if (searchOption == SearchOption.AllDirectories)
                            {
                                directoriesToEnumerate.Enqueue(findData.FileName);
                            }

                            if (includeDirectories)
                            {
                                yield return findData;
                            }
                        }
                        else if (includeFiles)
                        {
                            yield return findData;
                        }
                    }
                    while (NativeMethods.FindNextFile(handle, out findData));

                    int hr = Marshal.GetHRForLastWin32Error();
                    if (hr != NativeMethods.ErrorNoMoreFiles)
                    {
                        throw LongPathCommon.GetExceptionForHr(hr, currentDirectory);
                    }
                }
            }
        }

        #endregion // Internal methods

        #region Private methods

        /// <summary>
        /// Start enumeration by calling the <see cref="Native.NativeMethods.FindFirstFile"/> method.
        /// </summary>
        /// <param name="normalizedPathWithSearchPattern">The normalized path with search pattern.</param>
        /// <param name="findData">Data received.</param>
        /// <returns>Handle to the first file system entry found.</returns>
        private static SafeFindHandle BeginFind(string normalizedPathWithSearchPattern, out Win32FindData findData)
        {
            SafeFindHandle handle = NativeMethods.FindFirstFile(normalizedPathWithSearchPattern, out findData);
            if (handle.IsInvalid)
            {
                int hr = Marshal.GetHRForLastWin32Error();
                if (hr != NativeMethods.ErrorFileNotFound && hr != NativeMethods.ErrorPathNotFound)
                {
                    throw LongPathCommon.GetExceptionForHr(hr, normalizedPathWithSearchPattern);
                }

                return null;
            }

            return handle;
        }

        /// <summary>
        /// Normalizes the <paramref name="searchPattern"/> given.
        /// </summary>
        /// <param name="searchPattern">Search pattern.</param>
        /// <returns>Normalized search pattern.</returns>
        private static string NormalizeSearchPattern(string searchPattern)
        {
            return string.IsNullOrEmpty(searchPattern) || string.Equals(searchPattern, ".", StringComparison.OrdinalIgnoreCase)
                   ? "*"
                   : searchPattern;
        }

        /// <summary>
        /// Determines whether the <paramref name="directoryName"/> given is a current or a parent directory.
        /// </summary>
        /// <param name="directoryName">Directory name.</param>
        /// <returns>
        /// <see langword="true"/>, if the <paramref name="directoryName"/> is current or a parent directory; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool IsCurrentOrParentDirectory(string directoryName)
        {
            return directoryName.Equals(".", StringComparison.OrdinalIgnoreCase) || directoryName.Equals("..", StringComparison.OrdinalIgnoreCase);
        }

        #endregion // Private methods
    }
}
