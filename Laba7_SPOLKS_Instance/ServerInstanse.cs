using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Net;
using Laba7_SPOLKS_Server;

namespace Laba7_SPOLKS_Instance
{
  public class ServerInstanse
  {
    private const string SyncMessage = "S";
    private const int Size = 8192;
    private const int PollTimeout = 15000000;  //15 seconds
    private const int SemaphoreTimeout = 60000;
    private const string SharedMemoryFile = "SharedMemoryFile";

    private int _localPort;
    private int _connectionFlag = 0;

    private IPEndPoint _remoteIpEndPoint;
    private FileDetails _fileDetails;

    private MemoryMappedFile _memoryMapped;

    public ServerInstanse()
    {
      _memoryMapped = new MemoryMappedFile(SharedMemoryFile, "lab7");
    }

    public int ReceiveData(string[] arguments)
    {
      var semaphore = Semaphore.OpenExisting("sem");

      if (ParseCommandLineArguments(arguments) == -1)
      {
        return -1;
      }

      if (semaphore.WaitOne(SemaphoreTimeout) == false)
      {
        semaphore.Close();
        _memoryMapped.Dispose();
        return -1;
      }

      this.ReadExtraDataFromMemory();
      this.ShowExtraData();

      UdpFileClient udpFileClient = CreateUdpSocket();

      if (udpFileClient.ActiveRemoteHost == false)
      {
        return -1;
      }

      udpFileClient.Send(Encoding.UTF8.GetBytes(SyncMessage), SyncMessage.Length);

      var file = CreateNewFile();

      for (file.Position = 0; file.Position < _fileDetails.FileLength; )
      {
        try
        {
          if (udpFileClient.Client.Poll(PollTimeout, SelectMode.SelectRead) == false)
          {
            return 0;
          }

          var fileDataArray = udpFileClient.Receive(ref _remoteIpEndPoint);
          file.Write(fileDataArray, 0, fileDataArray.Length);

          ShowGetBytesCount(file);

          var sendBytesAmount = udpFileClient.Send(Encoding.UTF8.GetBytes(SyncMessage), SyncMessage.Length);
        }
        catch (SocketException e)
        {
          if (e.SocketErrorCode == SocketError.TimedOut && _connectionFlag < 3)
          {
            UploadFile(udpFileClient);
            continue;
          }
          else
          {
            CloseResources(file, udpFileClient);
            return -1;
          }
        }
      }

      return 0;
    }

    private void ReadExtraDataFromMemory()
    {
      var memeoryObject = _memoryMapped.Read(0);
      _fileDetails = memeoryObject as FileDetails;

      memeoryObject = _memoryMapped.Read(_memoryMapped.Size(_fileDetails));
      _remoteIpEndPoint = memeoryObject as IPEndPoint;
    }

    private UdpFileClient CreateUdpSocket()
    {
      UdpFileClient udpFileClient = new UdpFileClient(_localPort);
      udpFileClient.Client.ReceiveTimeout = udpFileClient.Client.SendTimeout = 10000;
      udpFileClient.Connect(_remoteIpEndPoint);
      return udpFileClient;
    }

    private FileStream CreateNewFile()
    {
      var dotIndex = _fileDetails.FileName.IndexOf('.');
      var fileName = _fileDetails.FileName.Substring(0, dotIndex) + _remoteIpEndPoint.GetHashCode() + _fileDetails.FileName.Substring(dotIndex);
      var file = new FileStream(fileName, FileMode.Create, FileAccess.Write);
      return file;
    }

    private int ParseCommandLineArguments(string[] arguments)
    {
      if (int.TryParse(arguments[0], out _localPort) == false)
      {
        return -1;
      }

      return 0;
    }

    private void UploadFile(UdpFileClient udpClient)
    {
      udpClient.Connect(_remoteIpEndPoint);

      if (udpClient.ActiveRemoteHost)
      {
        _connectionFlag = 0;
      }
      else
      {
        _connectionFlag++;
      }
    }

    private void CloseResources(FileStream file, UdpFileClient client)
    {
      file.Close();
      client.Close();
    }

    /// <summary>
    /// For debugging
    /// </summary>
    private void ShowExtraData()
    {
      Console.WriteLine(_fileDetails.FileName);
      Console.WriteLine(_fileDetails.FileLength);
      Console.WriteLine(_remoteIpEndPoint.Address);
      Console.WriteLine(_remoteIpEndPoint.Port);
    }

    /// <summary>
    /// For debugging
    /// </summary>
    /// <param name="file"></param>
    private void ShowGetBytesCount(FileStream file)
    {
      Console.Clear();
      Console.Write("{0}: ", file.Name);
      Console.WriteLine(file.Position);
    }
  }
}
