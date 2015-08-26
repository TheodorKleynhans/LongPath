// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LongPathFileInfo.cs">
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
    using System.IO;
    using System.Security;

    using LongPath.Native;

    /// <summary>
    /// Provides properties and instance methods for the creation, copying, deletion, moving, and opening of files, and aids in the creation of <see cref="FileStream"/> objects.
    /// </summary>
    public sealed class LongPathFileInfo : LongPathFileSystemInfo
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LongPathFileInfo"/> class.
        /// </summary>
        /// <param name="path">The fully qualified name of the file, or the relative file name.</param>
        public LongPathFileInfo(string path)
            : base(path, isDirectory: false)
        {
            this.UpdateProperties();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LongPathFileInfo"/> class.
        /// </summary>
        /// <param name="entryData">Entry data.</param>
        internal LongPathFileInfo(Win32FindData entryData)
            : base(entryData)
        {
            this.UpdateProperties();
        }

        #endregion // Constructors

        #region Properties

        /// <summary>
        /// Gets a string representing the directory's full path.
        /// </summary>
        public string DirectoryName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current file is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                this.RefreshIfNeeded(true);
                return this.EntryData.Value.FileAttributes.HasFlag(EFileAttributes.ReadOnly);
            }
        }

        /// <summary>
        /// Gets the size, in bytes, of the current file.
        /// </summary>
        public long Length
        {
            get
            {
                this.RefreshIfNeeded(true);
                return Converter.DoubleIntToLong(this.EntryData.Value.FileSizeHigh, this.EntryData.Value.FileSizeLow);
            }
        }

        #endregion // Properties

        #region Public methods

        /// <summary>
        /// Creates a <see cref="StreamWriter"/> that appends text to the file represented by this instance of the <see cref="LongPathFileInfo"/>.
        /// </summary>
        /// <returns>A new <see cref="StreamWriter"/>.</returns>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public StreamWriter AppendText()
        {
            return LongPathFile.AppendText(this.NormalizedPath);
        }

        /// <summary>
        /// Copies an existing file to a new file, disallowing the overwriting of an existing file.
        /// </summary>
        /// <param name="destFileName">The name of the new file to copy to.</param>
        /// <returns>A new file with a fully qualified path.</returns>
        /// <exception cref="ArgumentException"><paramref name="destFileName"/> is empty, contains only white spaces, or contains invalid characters.</exception>
        /// <exception cref="IOException">An error occurs, or the destination file already exists.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destFileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The directory specified in <paramref name="destFileName"/> does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">A directory path is passed in, or the file is being moved to a different drive.</exception>
        /// <exception cref="PathTooLongException"><paramref name="destFileName"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="destFileName"/> is in an invalid format.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest", Justification = "Name was taken from the official API.")]
        public LongPathFileInfo CopyTo(string destFileName)
        {
            return this.CopyTo(destFileName, false);
        }

        /// <summary>
        /// Copies an existing file to a new file, allowing the overwriting of an existing file.
        /// </summary>
        /// <param name="destFileName">The name of the new file to copy to.</param>
        /// <param name="overwrite"><see langword="true"/> to allow an existing file to be overwritten; otherwise, <see langword="false"/>.</param>
        /// <returns>
        /// A new file, or an overwrite of an existing file if overwrite is <see langword="true"/>.
        /// If the file exists and overwrite is false, an <see cref="IOException"/> is thrown.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="destFileName"/> is empty, contains only white spaces, or contains invalid characters.</exception>
        /// <exception cref="IOException">An error occurs, or the destination file already exists and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destFileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The directory specified in <paramref name="destFileName"/> does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">A directory path is passed in, or the file is being moved to a different drive.</exception>
        /// <exception cref="PathTooLongException"><paramref name="destFileName"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException"><paramref name="destFileName"/> is in an invalid format.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest", Justification = "Name was taken from the official API.")]
        public LongPathFileInfo CopyTo(string destFileName, bool overwrite)
        {
            LongPathFile.Copy(this.NormalizedPath, destFileName, overwrite);
            return new LongPathFileInfo(destFileName);
        }

        /// <summary>
        /// Creates a file.
        /// </summary>
        /// <returns>A new file.</returns>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public FileStream Create()
        {
            return LongPathFile.Create(this.NormalizedPath);
        }

        /// <summary>
        /// Creates the text.
        /// </summary>
        /// <returns>A new <see cref="StreamWriter"/>.</returns>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// This operation is not supported on the current platform.
        ///     <para>-or-</para>
        /// The caller does not have the required permission.
        /// </exception>
        public StreamWriter CreateText()
        {
            return LongPathFile.CreateText(this.NormalizedPath);
        }

        /// <summary>
        /// Permanently deletes a file.
        /// </summary>
        /// <exception cref="IOException">
        /// The target file is open or memory-mapped on a computer running Microsoft Windows NT.
        ///     <para>-or-</para>
        /// There is an open handle on the file, and the operating system is Windows XP or earlier.
        /// </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The path is a directory.</exception>
        public override void Delete()
        {
            LongPathFile.Delete(this.NormalizedPath);
            this.Refresh();
        }

        /// <summary>
        /// Moves a specified file to a new location, providing the option to specify a new file name.
        /// </summary>
        /// <param name="destFileName">The path to move the file to, which can specify a different file name.</param>
        /// <exception cref="IOException">
        /// The destination file already exists.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="destFileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="destFileName"/> is a zero-length string, contains only white space, or contains invalid characters as defined in <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in sourceFileName or destFileName is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="NotSupportedException"><paramref name="destFileName"/> is in an invalid format.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest", Justification = "Name was taken from the official API.")]
        public void MoveTo(string destFileName)
        {
            LongPathFile.Move(this.NormalizedPath, destFileName);
        }

        /// <summary>
        /// Opens a file in the specified mode.
        /// </summary>
        /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, <see cref="FileMode.Open"/> or <see cref="FileMode.Append"/>) in which to open the file.</param>
        /// <returns>A file opened in the specified mode, with read/write access and unshared.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="FileNotFoundException">The file is not found.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        public FileStream Open(FileMode mode)
        {
            return LongPathFile.Open(this.NormalizedPath, mode, FileAccess.ReadWrite, FileShare.None);
        }

        /// <summary>
        /// Opens a file in the specified mode with read, write, or read/write access.
        /// </summary>
        /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, <see cref="FileMode.Open"/> or <see cref="FileMode.Append"/>) in which to open the file.</param>
        /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with <see cref="FileAccess.Read"/>, <see cref="FileAccess.Write"/>, or <see cref="FileAccess.ReadWrite"/> file access.</param>
        /// <returns>A FileStream object opened in the specified mode and access, and unshared.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="FileNotFoundException">The file is not found.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        public FileStream Open(FileMode mode, FileAccess access)
        {
            return LongPathFile.Open(this.NormalizedPath, mode, access, FileShare.None);
        }

        /// <summary>
        /// Opens a file in the specified mode with read, write, or read/write access and the specified sharing option.
        /// </summary>
        /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, <see cref="FileMode.Open"/> or <see cref="FileMode.Append"/>) in which to open the file.</param>
        /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with <see cref="FileAccess.Read"/>, <see cref="FileAccess.Write"/>, or <see cref="FileAccess.ReadWrite"/> file access.</param>
        /// <param name="share">A <see cref="FileShare"/> constant specifying the type of access other <see cref="FileStream"/> objects have to this file.</param>
        /// <returns>A <see cref="FileStream"/> object opened with the specified mode, access, and sharing options.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="FileNotFoundException">The file is not found.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        public FileStream Open(FileMode mode, FileAccess access, FileShare share)
        {
            return LongPathFile.Open(this.NormalizedPath, mode, access, share);
        }

        /// <summary>
        /// Creates a read-only <see cref="FileStream"/>.
        /// </summary>
        /// <returns>A new read-only <see cref="FileStream"/> object.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="FileNotFoundException">The file is not found.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        public FileStream OpenRead()
        {
            return LongPathFile.OpenRead(this.NormalizedPath);
        }

        /// <summary>
        /// Creates a <see cref="StreamReader"/> with UTF8 encoding that reads from an existing text file.
        /// </summary>
        /// <returns>A new <see cref="StreamReader"/> with UTF8 encoding.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="FileNotFoundException">The file is not found.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        public StreamReader OpenText()
        {
            return LongPathFile.OpenText(this.NormalizedPath);
        }

        /// <summary>
        /// Creates a write-only <see cref="FileStream"/>.
        /// </summary>
        /// <returns>A new write-only <see cref="FileStream"/> object.</returns>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="FileNotFoundException">The file is not found.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">An I/O error occurred while creating the file.</exception>
        public FileStream OpenWrite()
        {
            return LongPathFile.OpenWrite(this.NormalizedPath);
        }

        #endregion // Public methods

        #region Private methods

        /// <summary>
        /// Updates the current class' properties.
        /// </summary>
        private void UpdateProperties()
        {
            this.DirectoryName = LongPathCommon.RemoveLongPathPrefix(LongPathCommon.GetDirectoryName(this.NormalizedPath));
        }

        #endregion // Private methods
    }
}
