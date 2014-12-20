using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Laba7_SPOLKS_Server
{
  public class FileReceiver
  {
    private const int Size = 8192;
    private const int LocalPort = 11000;
    private const string SyncMessage = "S";
    private const string SharedMemoryFile = "SharedMemoryFile";
    private const string ServerInstansePath = "..\\..\\..\\Laba7_SPOLKS_Instance\\bin\\Debug\\Laba7_SPOLKS_Instance.exe";

    private int connectionFlag = 0;
    private int SelectTimeout = 60000000;   //60 seconds
    private int AvailableClientsAmount = Environment.ProcessorCount - 1;

    private readonly UdpFileClient _udpFileReceiver;
    private readonly Dictionary<IPEndPoint, FileDetails> _fileDetails;

    private IPEndPoint _remoteIpEndPoint = null;
    private MemoryMappedFile _memoryMapped;
    private Semaphore _semaphore;
    private ProcessesPool _processesPool;

    public FileReceiver()
    {
      _fileDetails = new Dictionary<IPEndPoint, FileDetails>();
      _udpFileReceiver = new UdpFileClient(LocalPort);
      _processesPool = new ProcessesPool();
      _semaphore = new Semaphore(0, AvailableClientsAmount, "sem");
      _memoryMapped = new MemoryMappedFile(SharedMemoryFile, "lab7");
    }

    public int Receive()
    {
      InitializeUdpClients();
      _processesPool.InitializePool(LocalPort);

      var result = ReceiveFileData();
      return result;
    }

    private void InitializeUdpClients()
    {
      _udpFileReceiver.Client.ReceiveTimeout = _udpFileReceiver.Client.SendTimeout = 10000;
    }

    private int ReceiveFileDetails()
    {
      try
      {
        using (MemoryStream memoryStream = new MemoryStream())
        {
          var receivedFileInfo = _udpFileReceiver.Receive(ref _remoteIpEndPoint);

          if (_fileDetails.ContainsKey(_remoteIpEndPoint) == false)
          {
            XmlSerializer serializer = new XmlSerializer(typeof(FileDetails));

            memoryStream.Write(receivedFileInfo, 0, receivedFileInfo.Length);
            memoryStream.Position = 0;

            var fileDetails = (FileDetails)serializer.Deserialize(memoryStream);
            _fileDetails.Add(_remoteIpEndPoint, fileDetails);

            _memoryMapped.Write(fileDetails, 0);
            _memoryMapped.Write(_remoteIpEndPoint, _memoryMapped.Size(fileDetails));

            Console.WriteLine(fileDetails.FileName);
            Console.WriteLine(fileDetails.FileLength);
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
        return -1;
      }

      return 0;
    }

    private int ReceiveFileData()
    {
      var availableToReadSockets = new List<Socket>();

      try
      {
        while (true)
        {
          availableToReadSockets.Clear();
          availableToReadSockets.Add(_udpFileReceiver.Client);

          Socket.Select(availableToReadSockets, null, null, SelectTimeout);

          if (availableToReadSockets.Any())
          {
            if (_fileDetails.Count < _processesPool.MaxClientsAmount)
            {
              ReceiveFileDetails();

              if (_fileDetails.Count > _processesPool.MinClientsAmount)
              {
                _processesPool.StartProcess();
              }

              _semaphore.Release();
            }
          }
          else
          {
            break;
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
        return 0;
      }
      finally
      {
        CloseResources();
      }

      return 0;
    }

    private void CloseResources()
    {
      _udpFileReceiver.Close();
      _memoryMapped.Dispose();
      _semaphore.Close();
      File.Delete(SharedMemoryFile);
    }
  }
}
