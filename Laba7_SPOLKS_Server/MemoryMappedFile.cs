using System;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;

namespace Laba7_SPOLKS_Server
{
  /// <summary>
  /// A managed version of Memory mapped file
  /// By SHG at mail@Toolsbox.dk
  /// </summary>
  public class MemoryMappedFile : IDisposable
  {
    IntPtr _hMMF = IntPtr.Zero;
    FileStream _fs;
    public uint _AllocationGranularity;
    BinaryFormatter _Formatter = new BinaryFormatter();

    /// <summary>
    /// Creates a FileMapping handel
    /// </summary>
    /// <param name="FileName"></param>
    /// <param name="Name"></param>
    public MemoryMappedFile(string FileName, string Name)
    {
      _hMMF = Win32API.OpenFileMapping(Win32API.FileMapAccess.FileMapAllAccess, false, Name);

      if (_hMMF == IntPtr.Zero)
      {
        _fs = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        _hMMF = Win32API.CreateFileMapping(_fs, Win32API.FileMapProtection.PageReadWrite, Int64.MaxValue, Name);
        if (_hMMF == IntPtr.Zero)
          throw new Win32Exception();
      }

      Win32API.SYSTEM_INFO sysinfo = new Win32API.SYSTEM_INFO();
      Win32API.GetSystemInfo(ref sysinfo);
      _AllocationGranularity = sysinfo.dwAllocationGranularity;
    }

    public long Length
    {
      get
      {
        if (_fs == null) return -1;
        return _fs.Length;
      }
    }

    unsafe public void Write(Object o, Int64 AtOffset)
    {
      IntPtr hMVF = IntPtr.Zero;

      try
      {
        Int64 FileMapStart = (AtOffset / _AllocationGranularity) * _AllocationGranularity;
        Int64 MapViewSize = (AtOffset % _AllocationGranularity) + _AllocationGranularity;
        Int64 iViewDelta = AtOffset - FileMapStart;

        hMVF = Win32API.MapViewOfFile(_hMMF, Win32API.FileMapAccess.FileMapWrite, FileMapStart, (Int32)MapViewSize);
        if (hMVF == IntPtr.Zero)
          throw new Win32Exception();
        byte* p = (byte*)hMVF.ToPointer() + iViewDelta;
        UnmanagedMemoryStream ums = new UnmanagedMemoryStream(p, MapViewSize, MapViewSize, FileAccess.Write);
        _Formatter.Serialize(ums, o);
        Win32API.FlushViewOfFile(hMVF, (Int32)MapViewSize);
      }
      finally
      {
        if (hMVF != IntPtr.Zero)
          Win32API.UnmapViewOfFile(hMVF);
      }
    }

    unsafe public object Read(Int64 AtOffset)
    {
      IntPtr hMVF = IntPtr.Zero;

      try
      {
        Int64 FileMapStart = (AtOffset / _AllocationGranularity) * _AllocationGranularity;
        Int64 MapViewSize = (AtOffset % _AllocationGranularity) + _AllocationGranularity;
        Int64 iViewDelta = AtOffset - FileMapStart;

        hMVF = Win32API.MapViewOfFile(_hMMF, Win32API.FileMapAccess.FileMapRead, FileMapStart, (Int32)MapViewSize);
        if (hMVF == IntPtr.Zero)
          throw new Win32Exception();
        byte* p = (byte*)hMVF.ToPointer() + iViewDelta;
        UnmanagedMemoryStream ums = new UnmanagedMemoryStream(p, MapViewSize, MapViewSize, FileAccess.Read);
        object o = _Formatter.Deserialize(ums);
        return o;
      }
      finally
      {
        if (hMVF != IntPtr.Zero)
          Win32API.UnmapViewOfFile(hMVF);
      }
    }

    /// <summary>
    /// Writes a sequence of bytes
    /// </summary>
    /// <param name="Buffer"></param>
    /// <param name="BytesToWrite"></param>
    /// <param name="AtOffset"></param>
    unsafe public void Write(byte[] Buffer, int BytesToWrite, Int64 AtOffset)
    {
      IntPtr hMVF = IntPtr.Zero;

      try
      {
        Int64 FileMapStart = (AtOffset / _AllocationGranularity) * _AllocationGranularity;
        Int64 MapViewSize = (AtOffset % _AllocationGranularity) + _AllocationGranularity;
        Int64 iViewDelta = AtOffset - FileMapStart;

        hMVF = Win32API.MapViewOfFile(_hMMF, Win32API.FileMapAccess.FileMapWrite, FileMapStart, (Int32)MapViewSize);
        if (hMVF == IntPtr.Zero)
          throw new Win32Exception();
        byte* p = (byte*)hMVF.ToPointer() + iViewDelta;
        UnmanagedMemoryStream ums = new UnmanagedMemoryStream(p, MapViewSize, MapViewSize, FileAccess.Write);
        ums.Write(Buffer, 0, BytesToWrite);
        Win32API.FlushViewOfFile(hMVF, (Int32)MapViewSize);
      }
      finally
      {
        if (hMVF != IntPtr.Zero)
          Win32API.UnmapViewOfFile(hMVF);
      }
    }

    /// <summary>
    /// Read sequence of bytes
    /// </summary>
    /// <param name="Buffer"></param>
    /// <param name="BytesToRead"></param>
    /// <param name="AtOffset"></param>
    /// <returns>Num bytes read</returns>
    unsafe public int Read(byte[] Buffer, int BytesToRead, Int64 AtOffset)
    {
      IntPtr hMVF = IntPtr.Zero;

      try
      {
        Int64 FileMapStart = (AtOffset / _AllocationGranularity) * _AllocationGranularity;
        Int64 MapViewSize = (AtOffset % _AllocationGranularity) + _AllocationGranularity;
        Int64 iViewDelta = AtOffset - FileMapStart;

        hMVF = Win32API.MapViewOfFile(_hMMF, Win32API.FileMapAccess.FileMapRead, FileMapStart, (Int32)MapViewSize);
        if (hMVF == IntPtr.Zero)
          throw new Win32Exception();
        byte* p = (byte*)hMVF.ToPointer() + iViewDelta;
        UnmanagedMemoryStream ums = new UnmanagedMemoryStream(p, MapViewSize, MapViewSize, FileAccess.Read);
        byte[] ba = new byte[BytesToRead];
        return ums.Read(Buffer, 0, BytesToRead);
      }
      finally
      {
        if (hMVF != IntPtr.Zero)
          Win32API.UnmapViewOfFile(hMVF);
      }
    }

    /// <summary>
    /// returns the streamed size of an object
    /// </summary>
    /// <param name="T"></param>
    /// <returns></returns>
    public long Size(Object T)
    {
      MemoryStream ms = new MemoryStream();
      BinaryFormatter bf = new BinaryFormatter();
      bf.Serialize(ms, T);
      return ms.Length;
    }

    public void Dispose()
    {
      if (_hMMF != IntPtr.Zero)
      {
        Win32API.CloseHandle(_hMMF);
      }

      _hMMF = IntPtr.Zero;

      if (_fs != null)
      {
        _fs.Close();
      }
    }
  }
}
