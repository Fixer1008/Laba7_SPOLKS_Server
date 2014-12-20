using System;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Laba7_SPOLKS_Server
{
  internal sealed class Win32API
  {
    [DllImport("Kernel32", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpAttributes, FileMapProtection flProtect, Int32 dwMaxSizeHi, Int32 dwMaxSizeLow, string lpName);

    internal static IntPtr CreateFileMapping(System.IO.FileStream File, FileMapProtection flProtect, Int64 ddMaxSize, string lpName)
    {
      int Hi = (Int32)(ddMaxSize / Int32.MaxValue);
      int Lo = (Int32)(ddMaxSize % Int32.MaxValue);
      return CreateFileMapping(File.SafeFileHandle.DangerousGetHandle(), IntPtr.Zero, flProtect, Hi, Lo, lpName);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr OpenFileMapping(FileMapAccess DesiredAccess, bool bInheritHandle, string lpName);

    [Flags]
    internal enum FileMapProtection : uint
    {
      PageReadonly = 0x02,
      PageReadWrite = 0x04,
      PageWriteCopy = 0x08,
      PageExecuteRead = 0x20,
      PageExecuteReadWrite = 0x40,
      SectionCommit = 0x8000000,
      SectionImage = 0x1000000,
      SectionNoCache = 0x10000000,
      SectionReserve = 0x4000000,
    }

    [DllImport("Kernel32", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr MapViewOfFile(IntPtr hFileMapping, FileMapAccess dwDesiredAccess, Int32 dwFileOffsetHigh, Int32 dwFileOffsetLow, Int32 dwNumberOfBytesToMap);
    internal static IntPtr MapViewOfFile(IntPtr hFileMapping, FileMapAccess dwDesiredAccess, Int64 ddFileOffset, Int32 dwNumberOfBytesToMap)
    {
      int Hi = (Int32)(ddFileOffset / Int32.MaxValue);
      int Lo = (Int32)(ddFileOffset % Int32.MaxValue);
      return MapViewOfFile(hFileMapping, dwDesiredAccess, Hi, Lo, dwNumberOfBytesToMap);
    }

    [Flags]
    internal enum FileMapAccess : uint
    {
      FileMapCopy = 0x0001,
      FileMapWrite = 0x0002,
      FileMapRead = 0x0004,
      FileMapAllAccess = 0x001f,
      fileMapExecute = 0x0020,
    }

    [DllImport("kernel32.dll")]
    internal static extern bool FlushViewOfFile(IntPtr lpBaseAddress,
       Int32 dwNumberOfBytesToFlush);

    [DllImport("kernel32")]
    internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

    [DllImport("kernel32", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr hFile);

    [DllImport("kernel32.dll")]
    internal static extern void GetSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SYSTEM_INFO lpSystemInfo);

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_INFO
    {
      internal _PROCESSOR_INFO_UNION uProcessorInfo;
      public uint dwPageSize;
      public IntPtr lpMinimumApplicationAddress;
      public IntPtr lpMaximumApplicationAddress;
      public IntPtr dwActiveProcessorMask;
      public uint dwNumberOfProcessors;
      public uint dwProcessorType;
      public uint dwAllocationGranularity;
      public ushort dwProcessorLevel;
      public ushort dwProcessorRevision;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct _PROCESSOR_INFO_UNION
    {
      [FieldOffset(0)]
      internal uint dwOemId;
      [FieldOffset(0)]
      internal ushort wProcessorArchitecture;
      [FieldOffset(2)]
      internal ushort wReserved;
    }
  }
}
