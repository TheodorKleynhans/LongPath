// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LongPathFileSystemInfo.cs">
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
    using System.Security;
    using System.Security.Permissions;

    using LongPath.Native;

    /// <summary>
    /// Provides the base class for both <see cref="LongPathFileInfo"/> and <see cref="LongPathDirectoryInfo"/> objects.
    /// </summary>
    /// <remarks>
    /// This class is not thread-safe.
    /// </remarks>
    [FileIOPermission(SecurityAction.InheritanceDemand, Unrestricted = true)]
    public abstract class LongPathFileSystemInfo
    {
        #region Fields

        /// <summary>
        /// Whether the current system entry is directory.
        /// </summary>
        private readonly bool isDirectory;

        /// <summary>
        /// Current system entry data.
        /// </summary>
        private Win32FindData? entryData;

        /// <summary>
        /// Normalized path to the file or directory.
        /// </summary>
        private string normalizedPath;

        /// <summary>
        /// Whether the <see cref="entryData"/> was initialized.
        /// </summary>
        private volatile bool initialized;

        #endregion // Fields

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LongPathFileSystemInfo"/> class.
        /// </summary>
        /// <param name="data">File data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
        internal LongPathFileSystemInfo(Win32FindData data)
        {
            this.entryData = data;

            this.OriginalPath   = this.entryData.Value.FileName;
            this.NormalizedPath = LongPathCommon.NormalizePath(this.entryData.Value.FileName);
            this.isDirectory    = LongPathCommon.IsDirectory(this.entryData.Value);
            this.initialized    = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LongPathFileSystemInfo"/> class.
        /// </summary>
        /// <param name="path">The fully qualified name of the file or directory, or the relative file or directory name.</param>
        /// <param name="isDirectory">If set to <see langword="true"/>, the directory system information object will be retrieved.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        protected LongPathFileSystemInfo(string path, bool isDirectory)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            this.OriginalPath   = path;
            this.NormalizedPath = LongPathCommon.NormalizePath(path);
            this.isDirectory    = isDirectory;
            this.initialized    = false;
        }

        #endregion // Constructor

        #region Properties

        /// <summary>
        /// Gets or sets the attributes for the current file or directory.
        /// </summary>
        /// <value>
        /// <see cref="FileAttributes"/> of the current <see cref="LongPathFileSystemInfo"/>.
        /// </value>
        /// <exception cref="FileNotFoundException">The specified file does not exist.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid; for example, it is on an unmapped drive.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">
        /// The caller attempts to set an invalid file attribute.
        ///     <para>-or-</para>
        /// The user attempts to set an attribute value but does not have write permission.
        /// </exception>
        /// <exception cref="IOException"><see cref="Refresh"/> cannot initialize the data.</exception>
        public FileAttributes Attributes
        {
            get
            {
                this.RefreshIfNeeded(true);
                return (FileAttributes)this.entryData.Value.FileAttributes;
            }

            set
            {
                this.RefreshIfNeeded(true);
                LongPathCommon.SetAttributes(this.normalizedPath, value);
            }
        }

        /// <summary>
        /// Gets or sets the creation time of the current file or directory.
        /// </summary>
        /// <value>
        /// The creation date and time of the current <see cref="LongPathFileSystemInfo"/> object.
        /// </value>
        /// <exception cref="IOException"><see cref="Refresh"/> cannot initialize the data.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid; for example, it is on an unmapped drive.</exception>
        /// <exception cref="PlatformNotSupportedException">The current operating system is not Windows NT or later.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The caller attempts to set an invalid creation time.</exception>
        public DateTime CreationTime
        {
            get
            {
                this.RefreshIfNeeded(true);
                return Converter.FileTimeToDateTime(this.entryData.Value.CreationTime);
            }

            set
            {
                this.RefreshIfNeeded(true);
                LongPathCommon.SetTimestamps(this.normalizedPath, value, this.LastAccessTime, this.LastWriteTime);
            }
        }

        /// <summary>
        /// Gets or sets the creation time, in coordinated universal time (UTC), of the current file or directory.
        /// </summary>
        /// <value>
        /// The creation date and time in UTC format of the current <see cref="LongPathFileSystemInfo"/> object.
        /// </value>
        /// <exception cref="IOException"><see cref="Refresh"/> cannot initialize the data.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid; for example, it is on an unmapped drive.</exception>
        /// <exception cref="PlatformNotSupportedException">The current operating system is not Windows NT or later.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The caller attempts to set an invalid creation time.</exception>
        public DateTime CreationTimeUtc
        {
            get
            {
                return this.CreationTime.ToUniversalTime();
            }

            set
            {
                this.CreationTime = value.ToLocalTime();
            }
        }

        /// <summary>
        /// Gets a value indicating whether a file or directory exists.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the file or directory exists; otherwise, <see langword="false"/>.
        /// </value>
        public bool Exists
        {
            get
            {
                this.RefreshIfNeeded(false);
                return this.entryData.HasValue;
            }
        }

        /// <summary>
        /// Gets or sets the time the current file or directory was last accessed.
        /// </summary>
        /// <value>
        /// The time that the current file or directory was last accessed.
        /// </value>
        /// <exception cref="IOException"><see cref="Refresh"/> cannot initialize the data.</exception>
        /// <exception cref="PlatformNotSupportedException">The current operating system is not Windows NT or later.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The caller attempts to set an invalid access time.</exception>
        public DateTime LastAccessTime
        {
            get
            {
                this.RefreshIfNeeded(true);
                return Converter.FileTimeToDateTime(this.entryData.Value.LastAccessTime);
            }

            set
            {
                this.RefreshIfNeeded(true);
                LongPathCommon.SetTimestamps(this.normalizedPath, this.CreationTime, value, this.LastWriteTime);
            }
        }

        /// <summary>
        /// Gets or sets the time, in coordinated universal time (UTC), that the current file or directory was last accessed.
        /// </summary>
        /// <value>
        /// The UTC time that the current file or directory was last accessed.
        /// </value>
        /// <exception cref="IOException"><see cref="Refresh"/> cannot initialize the data.</exception>
        /// <exception cref="PlatformNotSupportedException">The current operating system is not Windows NT or later.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The caller attempts to set an invalid access time.</exception>
        public DateTime LastAccessTimeUtc
        {
            get
            {
                return this.LastAccessTime.ToUniversalTime();
            }

            set
            {
                this.LastAccessTime = value.ToLocalTime();
            }
        }

        /// <summary>
        /// Gets or sets the time when the current file or directory was last written to.
        /// </summary>
        /// <value>
        /// The time the current file was last written.
        /// </value>
        /// <exception cref="IOException"><see cref="Refresh"/> cannot initialize the data.</exception>
        /// <exception cref="PlatformNotSupportedException">The current operating system is not Windows NT or later.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The caller attempts to set an invalid write time.</exception>
        public DateTime LastWriteTime
        {
            get
            {
                this.RefreshIfNeeded(true);
                return Converter.FileTimeToDateTime(this.entryData.Value.LastWriteTime);
            }

            set
            {
                this.RefreshIfNeeded(true);
                LongPathCommon.SetTimestamps(this.normalizedPath, this.CreationTime, this.LastAccessTime, value);
            }
        }

        /// <summary>
        /// Gets or sets the time, in coordinated universal time (UTC), when the current file or directory was last written to.
        /// </summary>
        /// <value>
        /// The UTC time when the current file was last written to.
        /// </value>
        /// <exception cref="IOException"><see cref="Refresh"/> cannot initialize the data.</exception>
        /// <exception cref="PlatformNotSupportedException">The current operating system is not Windows NT or later.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The caller attempts to set an invalid write time.</exception>
        public DateTime LastWriteTimeUtc
        {
            get
            {
                return this.LastWriteTime.ToUniversalTime();
            }

            set
            {
                this.LastWriteTime = value.ToLocalTime();
            }
        }

        /// <summary>
        /// Gets the name of the file or directory.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the string representing the extension part of the file.
        /// </summary>
        /// <value>
        /// A string containing the <see cref="LongPathFileSystemInfo"/> extension.
        /// </value>
        public string Extension { get; private set; }

        /// <summary>
        /// Gets the full path of the directory or file.
        /// </summary>
        /// <value>
        /// A string containing the full path.
        /// </value>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public string FullName { get; private set; }

        /// <summary>
        /// Gets the current file system entry data.
        /// </summary>
        internal Win32FindData? EntryData => this.entryData;

        /// <summary>
        /// Gets or sets the normalized path to the current file or directory.
        /// </summary>
        internal string NormalizedPath
        {
            get
            {
                return this.normalizedPath;
            }

            set
            {
                this.normalizedPath = value;

                string trimmedPath  = this.normalizedPath.TrimEnd(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                this.FullName       = LongPathCommon.RemoveLongPathPrefix(trimmedPath);
                this.Name           = Path.GetFileName(trimmedPath);
                this.Extension      = Path.GetExtension(trimmedPath);
            }
        }

        /// <summary>
        /// Gets the path originally specified by the user, whether relative or absolute.
        /// </summary>
        internal string OriginalPath { get; }

        #endregion // Properties

        #region Public methods

        /// <summary>
        /// Deletes a file or directory.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid; for example, it is on an unmapped drive.</exception>
        /// <exception cref="IOException">There is an open handle on the file or directory, and the operating system is Windows XP or earlier.</exception>
        public abstract void Delete();

        /// <summary>
        /// Refreshes the state of the object.
        /// </summary>
        /// <exception cref="IOException">A device such as a disk drive is not ready.</exception>
        public void Refresh()
        {
            List<Win32FindData> foundFiles = LongPathDirectory.EnumerateFileSystemIterator(
                LongPathCommon.GetDirectoryName(this.normalizedPath),
                Path.GetFileName(this.normalizedPath),
                SearchOption.TopDirectoryOnly,
                this.isDirectory,
                !this.isDirectory).ToList();

            this.entryData   = foundFiles.Count > 0 ? foundFiles[0] : (Win32FindData?)null;
            this.initialized = true;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.FullName;
        }

        #endregion // Public methods

        #region Protected methods

        /// <summary>
        /// Refreshes the state of the object, if needed.
        /// </summary>
        /// <param name="throwIfNotFound">Whether to throw an exception, if the target path cannot be found.</param>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid; for example, it is on an unmapped drive.</exception>
        protected void RefreshIfNeeded(bool throwIfNotFound)
        {
            if (!this.initialized)
            {
                this.Refresh();
            }

            if (throwIfNotFound && !this.entryData.HasValue)
            {
                throw new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "Unable to find path {0}", this.OriginalPath));
            }
        }

        #endregion // Protected methods
    }
}
