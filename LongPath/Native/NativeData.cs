// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NativeData.cs">
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

namespace LongPath.Native
{
    using System;
    using System.Runtime.InteropServices;

    #region Enums

    /// <summary>
    /// A set of flags describing file access rights.
    /// </summary>
    [Flags]
    internal enum EFileAccess : uint
    {
        #region Standard section

        /// <summary>
        /// Controls the ability to get or set the SACL in an object's security descriptor.
        /// </summary>
        AccessSystemSecurity = 0x1000000,

        /// <summary>
        /// Maximum allowed access mask.
        /// </summary>
        MaximumAllowed = 0x2000000,

        /// <summary>
        /// The right to delete the object.
        /// </summary>
        Delete = 0x10000,

        /// <summary>
        /// The right to read the information in the object's security descriptor, not including the information in the system access control list (SACL).
        /// </summary>
        ReadControl = 0x20000,

        /// <summary>
        /// The right to modify the discretionary access control list (DACL) in the object's security descriptor.
        /// </summary>
        WriteDac = 0x40000,

        /// <summary>
        /// The right to change the owner in the object's security descriptor.
        /// </summary>
        WriteOwner = 0x80000,

        /// <summary>
        /// The right to use the object for synchronization.
        /// </summary>
        Synchronize = 0x100000,

        /// <summary>
        /// Combines <see cref="Delete"/>, <see cref="ReadControl"/>, <see cref="WriteDac"/>, and <see cref="WriteOwner"/> access.
        /// </summary>
        StandardRightsRequired = 0xF0000,

        /// <summary>
        /// Currently defined to equal <see cref="ReadControl"/>.
        /// </summary>
        StandardRightsRead = ReadControl,

        /// <summary>
        /// Currently defined to equal <see cref="ReadControl"/>.
        /// </summary>
        StandardRightsWrite = ReadControl,

        /// <summary>
        /// Currently defined to equal <see cref="ReadControl"/>.
        /// </summary>
        StandardRightsExecute = ReadControl,

        /// <summary>
        /// Combines <see cref="Delete"/>, <see cref="ReadControl"/>, <see cref="WriteDac"/>, <see cref="WriteOwner"/>, and <see cref="Synchronize"/> access.
        /// </summary>
        StandardRightsAll = 0x1F0000,

        /// <summary>
        /// Contains specific rights.
        /// </summary>
        SpecificRightsAll = 0xFFFF,

        /// <summary>
        /// For a file object, the right to read the corresponding file data.
        /// For a directory object, the right to read the corresponding directory data.
        /// </summary>
        FileReadData = 0x0001,

        /// <summary>
        /// For a directory, the right to list the contents of the directory.
        /// </summary>
        FileListDirectory = 0x0001,

        /// <summary>
        /// For a file object, the right to write data to the file.
        /// For a directory object, the right to create a file in the directory.
        /// </summary>
        FileWriteData = 0x0002,

        /// <summary>
        /// For a directory, the right to create a file in the directory.
        /// </summary>
        FileAddFile = 0x0002,

        /// <summary>
        /// For a file object, the right to append data to the file.
        /// For a directory object, the right to create a subdirectory.
        /// </summary>
        FileAppendData = 0x0004,

        /// <summary>
        /// For a directory, the right to create a subdirectory.
        /// </summary>
        FileAddSubdirectory = 0x0004,

        /// <summary>
        /// For a named pipe, the right to create a pipe.
        /// </summary>
        FileCreatePipeInstance = 0x0004,

        /// <summary>
        /// The right to read extended file attributes.
        /// </summary>
        FileReadEa = 0x0008,

        /// <summary>
        /// The right to write extended file attributes.
        /// </summary>
        FileWriteEa = 0x0010,

        /// <summary>
        /// The right to execute the file.
        /// </summary>
        FileExecute = 0x0020,

        /// <summary>
        /// For a directory, the right to traverse the directory.
        /// </summary>
        FileTraverse = 0x0020,

        /// <summary>
        /// For a directory, the right to delete a directory and all the files it contains, including read-only files.
        /// </summary>
        FileDeleteChild = 0x0040,

        /// <summary>
        /// The right to read file attributes.
        /// </summary>
        FileReadAttributes = 0x0080,

        /// <summary>
        /// The right to write file attributes.
        /// </summary>
        FileWriteAttributes = 0x0100,

        #endregion // Standard section

        #region Generic section

        /// <summary>
        /// Read access.
        /// </summary>
        GenericRead = 0x80000000,

        /// <summary>
        /// Write access.
        /// </summary>
        GenericWrite = 0x40000000,

        /// <summary>
        /// Execute access.
        /// </summary>
        GenericExecute = 0x20000000,

        /// <summary>
        /// All possible access rights.
        /// </summary>
        GenericAll = 0x10000000,

        /// <summary>
        /// All access rights.
        /// </summary>
        FileAllAccess =
            StandardRightsRequired |
            Synchronize |
            0x1FF,

        /// <summary>
        /// Generic read access rights.
        /// </summary>
        FileGenericRead =
            StandardRightsRead |
            FileReadData |
            FileReadAttributes |
            FileReadEa |
            Synchronize,

        /// <summary>
        /// Generic write access rights.
        /// </summary>
        FileGenericWrite =
            StandardRightsWrite |
            FileWriteData |
            FileWriteAttributes |
            FileWriteEa |
            FileAppendData |
            Synchronize,

        /// <summary>
        /// Generic execution access rights.
        /// </summary>
        FileGenericExecute =
            StandardRightsExecute |
            FileReadAttributes |
            FileExecute |
            Synchronize

        #endregion // Generic section
    }

    /// <summary>
    /// A set of flags describing file or device attributes and flags.
    /// </summary>
    [Flags]
    internal enum EFileAttributes : uint
    {
        /// <summary>
        /// A file that is read-only.
        /// Applications can read the file, but cannot write to it or delete it.
        /// </summary>
        ReadOnly = 0x00000001,

        /// <summary>
        /// The file or directory is hidden.
        /// It is not included in an ordinary directory listing.
        /// </summary>
        Hidden = 0x00000002,

        /// <summary>
        /// A file or directory that the operating system uses a part of, or uses exclusively.
        /// </summary>
        System = 0x00000004,

        /// <summary>
        /// The handle that identifies a directory.
        /// </summary>
        Directory = 0x00000010,

        /// <summary>
        /// A file or directory that is an archive file or directory. 
        /// </summary>
        Archive = 0x00000020,

        /// <summary>
        /// This value is reserved for system use.
        /// </summary>
        Device = 0x00000040,

        /// <summary>
        /// A file that does not have other attributes set.
        /// This attribute is valid only when used alone.
        /// </summary>
        Normal = 0x00000080,

        /// <summary>
        /// A file that is being used for temporary storage.
        /// </summary>
        Temporary = 0x00000100,

        /// <summary>
        /// A file that is a sparse file.
        /// </summary>
        SparseFile = 0x00000200,

        /// <summary>
        /// A file or directory that has an associated reparse point, or a file that is a symbolic link.
        /// </summary>
        ReparsePoint = 0x00000400,

        /// <summary>
        /// A file or directory that is compressed.
        /// </summary>
        Compressed = 0x00000800,

        /// <summary>
        /// The data of a file is not available immediately.
        /// </summary>
        Offline = 0x00001000,

        /// <summary>
        /// The file or directory is not to be indexed by the content indexing service.
        /// </summary>
        NotContentIndexed = 0x00002000,

        /// <summary>
        /// A file or directory that is encrypted.
        /// </summary>
        Encrypted = 0x00004000,

        /// <summary>
        /// Write operations will not go through any intermediate cache, they will go directly to disk.
        /// </summary>
        WriteThrough = 0x80000000,

        /// <summary>
        /// The file or device is being opened or created for asynchronous I/O.
        /// </summary>
        Overlapped = 0x40000000,

        /// <summary>
        /// The file or device is being opened with no system caching for data reads and writes.
        /// </summary>
        NoBuffering = 0x20000000,

        /// <summary>
        /// Access is intended to be random.
        /// </summary>
        RandomAccess = 0x10000000,

        /// <summary>
        /// Access is intended to be sequential from beginning to end. 
        /// </summary>
        SequentialScan = 0x08000000,

        /// <summary>
        /// The file is to be deleted immediately after all of its handles are closed,
        /// which includes the specified handle and any other open or duplicated handles.
        /// </summary>
        DeleteOnClose = 0x04000000,

        /// <summary>
        /// The file is being opened or created for a backup or restore operation.
        /// </summary>
        BackupSemantics = 0x02000000,

        /// <summary>
        /// Access will occur according to POSIX rules.
        /// </summary>
        PosixSemantics = 0x01000000,

        /// <summary>
        /// Normal reparse point processing will not occur.
        /// </summary>
        OpenReparsePoint = 0x00200000,

        /// <summary>
        /// The file data is requested, but it should continue to be located in remote storage.
        /// </summary>
        OpenNoRecall = 0x00100000,

        /// <summary>
        /// Invalid file attributes.
        /// </summary>
        Invalid = 0xFFFFFFFF
    }

    #endregion // Enums

    #region Structures

    /// <summary>
    /// Contains information about the file that is found by the <c>FindFirstFile</c>, <c>FindFirstFileEx</c>, or <c>FindNextFile</c> function.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct Win32FindData
    {
        /// <summary>
        /// The file attributes of a file.
        /// </summary>
        public EFileAttributes FileAttributes;

        /// <summary>
        /// A <c>FILETIME</c> structure that specifies when a file or directory was created.
        /// </summary>
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;

        /// <summary>
        /// A <c>FILETIME</c> structure that specifies when a file or directory was previously accessed.
        /// </summary>
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;

        /// <summary>
        /// A <c>FILETIME</c> structure that specifies when a file or directory was previously written.
        /// </summary>
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;

        /// <summary>
        /// The high-order <c>DWORD</c> value of the file size, in bytes.
        /// </summary>
        public int FileSizeHigh;

        /// <summary>
        /// The low-order <c>DWORD</c> value of the file size, in bytes.
        /// </summary>
        public int FileSizeLow;

        /// <summary>
        /// This value is undefined and should not be used.
        /// </summary>
        public int Reserved0;

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        public int Reserved1;

        /// <summary>
        /// The name of the file.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeMethods.MaxPath)]
        public string FileName;

        /// <summary>
        /// An alternative name for the file.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeMethods.MaxAlternate)]
        public string Alternate;
    }

    #endregion // Structures
}
