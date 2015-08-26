// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LongPathDirectoryInfo.cs">
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
    using System.Linq;
    using System.Security;

    using LongPath.Native;

    /// <summary>
    /// Exposes instance methods for creating, moving, and enumerating through directories and subdirectories.
    /// </summary>
    public sealed class LongPathDirectoryInfo : LongPathFileSystemInfo
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LongPathDirectoryInfo"/> class.
        /// </summary>
        /// <param name="path">The fully qualified name of a directory, or the relative directory name.</param>
        public LongPathDirectoryInfo(string path)
            : base(path, isDirectory: true)
        {
            this.UpdateProperties();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LongPathDirectoryInfo"/> class.
        /// </summary>
        /// <param name="entryData">Entry data.</param>
        internal LongPathDirectoryInfo(Win32FindData entryData)
            : base(entryData)
        {
            this.UpdateProperties();
        }

        #endregion // Constructors

        #region Properties

        /// <summary>
        /// Gets the parent directory of a specified subdirectory.
        /// </summary>
        public string Parent { get; private set; }

        #endregion // Properties

        #region Public methods

        /// <summary>
        /// Creates a directory.
        /// </summary>
        /// <exception cref="IOException">The directory cannot be created.</exception>
        public void Create()
        {
            LongPathDirectory.CreateDirectory(this.FullName);
            this.Refresh();
        }

        /// <summary>
        /// Creates a subdirectory or subdirectories on the specified path.
        /// The specified path can be relative to this instance of the <see cref="LongPathDirectoryInfo"/> class.
        /// </summary>
        /// <param name="path">The specified path.</param>
        /// <returns>The last directory specified in <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> does not specify a valid file path or contains invalid characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="IOException">
        /// The subdirectory cannot be created.
        ///     <para>-or-</para>
        /// A file or directory already has the name specified by <paramref name="path"/>.
        /// </exception>
        /// <exception cref="PathTooLongException">The caller does not have code access permission to create the directory.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> contains a colon character (<c>:</c>) that is not part of a drive label ("<c>C:\</c>").</exception>
        public LongPathDirectoryInfo CreateSubdirectory(string path)
        {
            return LongPathDirectory.CreateDirectory(Path.Combine(this.FullName, path));
        }

        /// <summary>
        /// Deletes this <see cref="LongPathDirectoryInfo"/> if it is empty.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">The directory contains a read-only file.</exception>
        /// <exception cref="DirectoryNotFoundException">The directory described by this <see cref="LongPathDirectoryInfo"/> object does not exist or could not be found.</exception>
        /// <exception cref="IOException">
        /// The directory is not empty.
        ///     <para>-or-</para>
        /// The directory is the application's current working directory.
        ///     <para>-or-</para>
        /// There is an open handle on the directory, and the operating system is Windows XP or earlier.
        /// </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public override void Delete()
        {
            this.Delete(false);
        }

        /// <summary>
        /// Deletes this instance of a <see cref="LongPathDirectoryInfo"/>, specifying whether to delete subdirectories and files.
        /// </summary>
        /// <param name="recursive"><see langword="true"/> to remove directories, subdirectories, and files in path; otherwise, <see langword="false"/>.</param>
        /// <exception cref="UnauthorizedAccessException">The directory contains a read-only file.</exception>
        /// <exception cref="DirectoryNotFoundException">The directory described by this <see cref="LongPathDirectoryInfo"/> object does not exist or could not be found.</exception>
        /// <exception cref="IOException">
        /// The directory is read-only.
        ///     <para>-or-</para>
        /// The directory contains one or more files or subdirectories and <paramref name="recursive"/> is <see langword="false"/>.
        ///     <para>-or-</para>
        /// The directory is the application's current working directory.
        ///     <para>-or-</para>
        /// There is an open handle on the directory, and the operating system is Windows XP or earlier.
        /// </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public void Delete(bool recursive)
        {
            LongPathDirectory.Delete(this.NormalizedPath, recursive);
            this.Refresh();
        }

        /// <summary>
        /// Returns an enumerable collection of directory information in the current directory.
        /// </summary>
        /// <returns>
        /// An enumerable collection of directories in the current directory.
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathDirectoryInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public IEnumerable<LongPathDirectoryInfo> EnumerateDirectories()
        {
            return LongPathDirectory.EnumerateFileSystemEntries(this.NormalizedPath, "*", SearchOption.TopDirectoryOnly, includeDirectories: true, includeFiles: false).Cast<LongPathDirectoryInfo>();
        }

        /// <summary>
        /// Returns an enumerable collection of directory information that matches a specified search pattern.
        /// </summary>
        /// <param name="searchPattern">The search string. The default pattern is "<c>*</c>", which returns all directories.</param>
        /// <returns>An enumerable collection of directories that matches <paramref name="searchPattern"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathDirectoryInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public IEnumerable<LongPathDirectoryInfo> EnumerateDirectories(string searchPattern)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(this.NormalizedPath, searchPattern, SearchOption.TopDirectoryOnly, includeDirectories: true, includeFiles: false).Cast<LongPathDirectoryInfo>();
        }

        /// <summary>
        /// Returns an enumerable collection of directory information that matches a specified search pattern and search subdirectory option.
        /// </summary>
        /// <param name="searchPattern">The search string. The default pattern is "<c>*</c>", which returns all directories.</param>
        /// <param name="searchOption">
        /// One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.
        /// The default value is <see cref="SearchOption.TopDirectoryOnly"/>.
        /// </param>
        /// <returns>An enumerable collection of directories that matches <paramref name="searchPattern"/> and <paramref name="searchOption"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathDirectoryInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public IEnumerable<LongPathDirectoryInfo> EnumerateDirectories(string searchPattern, SearchOption searchOption)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(this.NormalizedPath, searchPattern, searchOption, includeDirectories: true, includeFiles: false).Cast<LongPathDirectoryInfo>();
        }

        /// <summary>
        /// Returns an enumerable collection of file information in the current directory.
        /// </summary>
        /// <returns>An enumerable collection of the files in the current directory.</returns>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public IEnumerable<LongPathFileInfo> EnumerateFiles()
        {
            return LongPathDirectory.EnumerateFileSystemEntries(this.NormalizedPath, "*", SearchOption.TopDirectoryOnly, includeDirectories: false, includeFiles: true).Cast<LongPathFileInfo>();
        }

        /// <summary>
        /// Returns an enumerable collection of file information that matches a search pattern.
        /// </summary>
        /// <param name="searchPattern">The search string. The default pattern is "<c>*</c>", which returns all files.</param>
        /// <returns>An enumerable collection of files that matches <paramref name="searchPattern"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public IEnumerable<LongPathFileInfo> EnumerateFiles(string searchPattern)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(this.NormalizedPath, searchPattern, SearchOption.TopDirectoryOnly, includeDirectories: false, includeFiles: true).Cast<LongPathFileInfo>();
        }

        /// <summary>
        /// Returns an enumerable collection of file information that matches a specified search pattern and search subdirectory option.
        /// </summary>
        /// <param name="searchPattern">The search string. The default pattern is "<c>*</c>", which returns all files.</param>
        /// <param name="searchOption">
        /// One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.
        /// The default value is <see cref="SearchOption.TopDirectoryOnly"/>.
        /// </param>
        /// <returns>An enumerable collection of files that matches <paramref name="searchPattern"/> and <paramref name="searchOption"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public IEnumerable<LongPathFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(this.NormalizedPath, searchPattern, searchOption, includeDirectories: false, includeFiles: true).Cast<LongPathFileInfo>();
        }

        /// <summary>
        /// Returns an enumerable collection of file system information in the current directory.
        /// </summary>
        /// <returns>An enumerable collection of file system information in the current directory.</returns>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileSystemInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Infos", Justification = "Name was taken from the official API.")]
        public IEnumerable<LongPathFileSystemInfo> EnumerateFileSystemInfos()
        {
            return LongPathDirectory.EnumerateFileSystemEntries(this.NormalizedPath, "*", SearchOption.TopDirectoryOnly, includeDirectories: true, includeFiles: true);
        }

        /// <summary>
        /// Returns an enumerable collection of file system information that matches a specified search pattern.
        /// </summary>
        /// <param name="searchPattern">The search string. The default pattern is "<c>*</c>", which returns all files or directories.</param>
        /// <returns>An enumerable collection of file system information objects that matches <paramref name="searchPattern"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileSystemInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Infos", Justification = "Name was taken from the official API.")]
        public IEnumerable<LongPathFileSystemInfo> EnumerateFileSystemInfos(string searchPattern)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(this.NormalizedPath, searchPattern, SearchOption.TopDirectoryOnly, includeDirectories: true, includeFiles: true);
        }

        /// <summary>
        /// Returns an enumerable collection of file system information that matches a specified search pattern and search subdirectory option.
        /// </summary>
        /// <param name="searchPattern">The search string. The default pattern is "<c>*</c>", which returns all files or directories.</param>
        /// <param name="searchOption">
        /// One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.
        /// The default value is <see cref="SearchOption.TopDirectoryOnly"/>.
        /// </param>
        /// <returns>An enumerable collection of file system information objects that matches <paramref name="searchPattern"/> and <paramref name="searchOption"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileSystemInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Infos", Justification = "Name was taken from the official API.")]
        public IEnumerable<LongPathFileSystemInfo> EnumerateFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            return LongPathDirectory.EnumerateFileSystemEntries(this.NormalizedPath, searchPattern, searchOption, includeDirectories: true, includeFiles: true);
        }

        /// <summary>
        /// Returns the subdirectories of the current directory.
        /// </summary>
        /// <returns>An array of <see cref="LongPathDirectoryInfo"/> objects.</returns>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathDirectoryInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public LongPathDirectoryInfo[] GetDirectories()
        {
            return this.EnumerateDirectories().ToArray();
        }

        /// <summary>
        /// Returns an array of directories in the current <see cref="LongPathDirectoryInfo"/> matching the given search criteria.
        /// </summary>
        /// <param name="searchPattern">The search string. For example, "<c>System*</c>" can be used to search for all directories that begin with the word "<c>System</c>".</param>
        /// <returns>An array of type <see cref="LongPathDirectoryInfo"/> matching <paramref name="searchPattern"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathDirectoryInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public LongPathDirectoryInfo[] GetDirectories(string searchPattern)
        {
            return this.EnumerateDirectories(searchPattern).ToArray();
        }

        /// <summary>
        /// Returns an array of directories in the current <see cref="LongPathDirectoryInfo"/> matching the given search criteria and using a value to determine whether to search subdirectories.
        /// </summary>
        /// <param name="searchPattern">The search string. For example, "<c>System*</c>" can be used to search for all directories that begin with the word "<c>System</c>".</param>
        /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.</param>
        /// <returns>An array of type <see cref="LongPathDirectoryInfo"/> matching <paramref name="searchPattern"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathDirectoryInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public LongPathDirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption)
        {
            return this.EnumerateDirectories(searchPattern, searchOption).ToArray();
        }

        /// <summary>
        /// Returns a file list from the current directory.
        /// </summary>
        /// <returns>An array of type <see cref="LongPathFileInfo"/>.</returns>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public LongPathFileInfo[] GetFiles()
        {
            return this.EnumerateFiles().ToArray();
        }

        /// <summary>
        /// Returns a file list from the current directory matching the given search pattern.
        /// </summary>
        /// <param name="searchPattern">The search string, such as "<c>*.txt</c>".</param>
        /// <returns>An array of type <see cref="LongPathFileInfo"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public LongPathFileInfo[] GetFiles(string searchPattern)
        {
            return this.EnumerateFiles(searchPattern).ToArray();
        }

        /// <summary>
        /// Returns a file list from the current directory matching the given search pattern and using a value to determine whether to search subdirectories.
        /// </summary>
        /// <param name="searchPattern">The search string. For example, "<c>System*</c>" can be used to search for all files that begin with the word "<c>System</c>".</param>
        /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.</param>
        /// <returns>An array of type <see cref="LongPathFileInfo"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public LongPathFileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
        {
            return this.EnumerateFiles(searchPattern, searchOption).ToArray();
        }

        /// <summary>
        /// Returns an array of strongly typed <see cref="LongPathFileSystemInfo"/> entries representing all the files and subdirectories in a directory.
        /// </summary>
        /// <returns>An array of strongly typed <see cref="LongPathFileSystemInfo"/> entries.</returns>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileSystemInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Infos", Justification = "Name was taken from the official API.")]
        public LongPathFileSystemInfo[] GetFileSystemInfos()
        {
            return this.EnumerateFileSystemInfos().ToArray();
        }

        /// <summary>
        /// Retrieves an array of strongly typed <see cref="LongPathFileSystemInfo"/> objects representing the files and subdirectories that match the specified search criteria.
        /// </summary>
        /// <param name="searchPattern">The search string. For example, "<c>System*</c>" can be used to search for all directories that begin with the word "<c>System</c>".</param>
        /// <returns>An array of strongly typed <see cref="LongPathFileSystemInfo"/> objects matching the search criteria.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileSystemInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Infos", Justification = "Name was taken from the official API.")]
        public LongPathFileSystemInfo[] GetFileSystemInfos(string searchPattern)
        {
            return this.EnumerateFileSystemInfos(searchPattern).ToArray();
        }

        /// <summary>
        /// Retrieves an array of <see cref="LongPathFileSystemInfo"/> objects that represent the files and subdirectories matching the specified search criteria.
        /// </summary>
        /// <param name="searchPattern">The search string. The default pattern is "<c>*</c>", which returns all files and directories.</param>
        /// <param name="searchOption">
        /// One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.
        /// The default value is <see cref="SearchOption.TopDirectoryOnly"/>.
        /// </param>
        /// <returns>An array of file system entries that match the search criteria.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException">The path encapsulated in the <see cref="LongPathFileSystemInfo"/> object is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Infos", Justification = "Name was taken from the official API.")]
        public LongPathFileSystemInfo[] GetFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            return this.EnumerateFileSystemInfos(searchPattern, searchOption).ToArray();
        }

        #endregion // Public methods

        #region Private methods

        /// <summary>
        /// Updates the current class' properties.
        /// </summary>
        private void UpdateProperties()
        {
            this.Parent = LongPathCommon.RemoveLongPathPrefix(LongPathCommon.GetDirectoryName(this.NormalizedPath));
        }

        #endregion // Private methods
    }
}
