// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NativeMethods.cs">
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
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Contains P/Invoke method prototypes.
    /// </summary>
    /// <remarks>
    /// List of Win32 error codes:
    /// <c>http://msdn.microsoft.com/en-us/library/cc231199(v=prot.20).aspx</c>
    /// </remarks>
    internal static class NativeMethods
    {
        #region Constants

        /// <summary>
        /// Maximum path length.
        /// </summary>
        public const int MaxPath = 248;

        /// <summary>
        /// While Windows allows larger paths up to a maximum of <c>32767</c> characters, because this is only an approximation and
        /// can vary across systems and OS versions, we choose a limit well under so that we can give a consistent behavior.
        /// </summary>
        public const int MaxLongPath = 32000;

        /// <summary>
        /// Maximum alternate path length.
        /// </summary>
        public const int MaxAlternate = 14;

        /// <summary>
        /// Windows return code for successful operations.
        /// </summary>
        public const int Ok = 0;

        /// <summary>
        /// The system cannot find the file specified.
        /// </summary>
        public const int ErrorFileNotFound = unchecked((int)0x80070002);

        /// <summary>
        /// The system cannot find the path specified.
        /// </summary>
        public const int ErrorPathNotFound = unchecked((int)0x80070003);

        /// <summary>
        /// Access is denied.
        /// </summary>
        public const int ErrorAccessDenied = unchecked((int)0x80070005);

        /// <summary>
        /// The system cannot find the drive specified.
        /// </summary>
        public const int ErrorInvalidDrive = unchecked((int)0x8007000F);

        /// <summary>
        /// There are no more files.
        /// </summary>
        public const int ErrorNoMoreFiles = unchecked((int)0x80070012);

        /// <summary>
        /// The file name, directory name, or volume label syntax is incorrect.
        /// </summary>
        public const int ErrorInvalidName = unchecked((int)0x8007007B);

        /// <summary>
        /// Cannot create a file when that file already exists.
        /// </summary>
        public const int ErrorAlreadyExists = unchecked((int)0x800700B7);

        /// <summary>
        /// The file name or extension is too long.
        /// </summary>
        public const int ErrorFilenameExcedRange = unchecked((int)0x800700CE);

        /// <summary>
        /// The I/O operation has been aborted because of either a thread exit or an application request.
        /// </summary>
        public const int ErrorOperationAborted = unchecked((int)0x800703E3);

        /// <summary>
        /// The directory is not empty.
        /// </summary>
        public const int ErrorDirNotEmpty = unchecked((int)0x80070091);

        /// <summary>
        /// The file exists.
        /// </summary>
        public const int ErrorFileExists = unchecked((int)0x80070050);

        #endregion // Constants

        #region kernel32.dll

        /// <summary>
        /// Sets the date and time that the specified file or directory was created, last accessed, or last modified.
        /// </summary>
        /// <param name="fileHandle">A handle to the file or directory.</param>
        /// <param name="creationTime">New creation date and time for the file or directory.</param>
        /// <param name="lastAccessTime">New last access date and time for the file or directory.</param>
        /// <param name="lastWriteTime">New last modified date and time for the file or directory.</param>
        /// <returns>If the function succeeds, the return value is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetFileTime(
            /* [in] */ SafeFileHandle fileHandle,
            /* [in] */ ref long creationTime,
            /* [in] */ ref long lastAccessTime,
            /* [in] */ ref long lastWriteTime);

        /// <summary>
        /// Retrieves file system attributes for a specified file or directory.
        /// </summary>
        /// <param name="fileName">The name of the file or directory.</param>
        /// <returns>If the function succeeds, the return value is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern EFileAttributes GetFileAttributes(
            /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string fileName);

        /// <summary>
        /// Sets the attributes for a file or directory.
        /// </summary>
        /// <param name="fileName">The name of the file whose attributes are to be set.</param>
        /// <param name="fileAttributes">The file attributes to set for the file.</param>
        /// <returns>If the function succeeds, the return value is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2205:UseManagedEquivalentsOfWin32Api", Justification = "Managed alternative does not support long paths.")]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetFileAttributes(
             /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string fileName,
             /* [in] */ [MarshalAs(UnmanagedType.U4)] EFileAttributes fileAttributes);

        /// <summary>
        /// Searches a directory for a file or subdirectory with a name that matches a specific name (or partial name if wildcards are used).
        /// </summary>
        /// <param name="fileName">The directory or path, and the file name, which can include wildcard characters.</param>
        /// <param name="findFileData">The <see cref="Win32FindData"/> structure that receives information about a found file or directory.</param>
        /// <returns>If the function succeeds, the return value is a search handle used in a subsequent call to <see cref="FindNextFile"/> or <see cref="FindClose"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeFindHandle FindFirstFile(
            /* [in]  */ [MarshalAs(UnmanagedType.LPWStr)] string fileName,
            /* [out] */ out Win32FindData findFileData);

        /// <summary>
        /// Continues a file search from a previous call to the <see cref="FindFirstFile"/>.
        /// </summary>
        /// <param name="findFile">The search handle returned by a previous call to the <see cref="FindFirstFile"/> function.</param>
        /// <param name="findFileData">The <see cref="Win32FindData"/> structure that receives information about a found file or directory.</param>
        /// <returns>If the function succeeds, the return value is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindNextFile(
            /* [in]  */ SafeFindHandle findFile,
            /* [out] */ out Win32FindData findFileData);

        /// <summary>
        /// Closes a file search handle opened by the <see cref="FindFirstFile"/> or <see cref="FindNextFile"/>.
        /// </summary>
        /// <param name="findFile">The file search handle.</param>
        /// <returns>If the function succeeds, the return value is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindClose(
            /* [in] */ IntPtr findFile);

        /// <summary>
        /// Creates or opens a file or I/O device.
        /// </summary>
        /// <param name="fileName">The name of the file or device to be created or opened.</param>
        /// <param name="desiredAccess">The requested access to the file or device, which can be summarized as read, write, both or neither zero.</param>
        /// <param name="shareMode">The requested sharing mode of the file or device, which can be read, write, both, delete, all of these, or none.</param>
        /// <param name="securityAttributes">A pointer to a <c>SECURITY_ATTRIBUTES</c> structure.</param>
        /// <param name="createMode">An action to take on a file or device that exists or does not exist.</param>
        /// <param name="flagsAndAttributes">The file or device attributes and flags.</param>
        /// <param name="templateFile">A valid handle to a template file with the <c>GENERIC_READ</c> access right.</param>
        /// <returns>If the function succeeds, the return value is an open handle to the specified file, device, named pipe, or mail slot.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
            /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string fileName,
            /* [in] */ [MarshalAs(UnmanagedType.U4)] EFileAccess desiredAccess,
            /* [in] */ [MarshalAs(UnmanagedType.U4)] FileShare shareMode,
            /* [in] */ IntPtr securityAttributes,
            /* [in] */ [MarshalAs(UnmanagedType.U4)] FileMode createMode,
            /* [in] */ [MarshalAs(UnmanagedType.U4)] EFileAttributes flagsAndAttributes,
            /* [in] */ IntPtr templateFile);

        /// <summary>
        /// Copies an existing file to a new file.
        /// </summary>
        /// <param name="existingFileName">The name of an existing file.</param>
        /// <param name="newFileName">The name of the new file.</param>
        /// <param name="failIfExists">
        /// If this parameter is <see langword="true"/> and the new file specified by <paramref name="newFileName"/> already exists,
        /// the function fails.<br/>
        /// If this parameter is <see langword="false"/> and the new file already exists, the function overwrites the existing file and succeeds.
        /// </param>
        /// <returns>If the function succeeds, the return value is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CopyFile(
            /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string existingFileName,
            /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string newFileName,
            /* [in] */ [MarshalAs(UnmanagedType.Bool)] bool failIfExists);

        /// <summary>
        /// Moves an existing file or a directory, including its children.
        /// </summary>
        /// <param name="pathNameFrom">The current name of the file or directory on the local computer.</param>
        /// <param name="pathNameTo">The new name for the file or directory.</param>
        /// <returns>If the function succeeds, the return value is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MoveFile(
            /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string pathNameFrom,
            /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string pathNameTo);

        /// <summary>
        /// Deletes an existing file.
        /// </summary>
        /// <param name="fileName">The name of the file to be deleted.</param>
        /// <returns>If the function succeeds, the return value is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteFile(
            /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string fileName);

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="pathName">The path of the directory to be created.</param>
        /// <param name="securityAttributes">A pointer to a <c>SECURITY_ATTRIBUTES</c> structure.</param>
        /// <returns>If the function succeeds, the return value is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateDirectory(
            /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string pathName,
            /* [in] */ IntPtr securityAttributes);

        /// <summary>
        /// Deletes an existing empty directory.
        /// </summary>
        /// <param name="pathName">The path of the directory to be removed.</param>
        /// <returns>If the function succeeds, the return value is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RemoveDirectory(
            /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string pathName);

        /// <summary>
        /// Retrieves the full path and file name of the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="bufferLength">The size of the <paramref name="buffer"/> to receive the null-terminated string for the drive and path.</param>
        /// <param name="buffer">A buffer that receives the null-terminated string for the drive and path.</param>
        /// <param name="filePart">A pointer to a buffer that receives the address (within <paramref name="buffer"/>) of the final file name component in the path.</param>
        /// <returns>If the function succeeds, the return value is the length, in <c>TCHARs</c>, of the string copied to <paramref name="buffer"/>, not including the terminating null character.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint GetFullPathName(
            /* [in]  */ [MarshalAs(UnmanagedType.LPWStr)] string fileName,
            /* [in]  */ uint bufferLength,
            /* [out] */ StringBuilder buffer,
            /* [out] */ IntPtr filePart);

        #endregion // kernel32.dll
    }
}
