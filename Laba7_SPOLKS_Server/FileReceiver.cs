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
    private const int WindowSize = 5;
    private const string SyncMessage = "S";

    private const string ServerInstansePath = "..\\..\\..\\Laba7_SPOLKS_Instance\\bin\\Debug\\Laba7_SPOLKS_Instance.exe";

    private int connectionFlag = 0;
    private int SelectTimeout = 20000000;
    private int AvailableClientsAmount = Environment.ProcessorCount - 1;

    private readonly UdpFileClient[] _udpFileReceiver;
    private readonly Dictionary<Socket, FileDetails> _fileDetails;

    private IList<Process> _processesList;
    private IList<Socket> _socket;

    private IPEndPoint _remoteIpEndPoint = null;

    public FileReceiver()
    {
      _fileDetails = new Dictionary<Socket, FileDetails>();
      _udpFileReceiver = new UdpFileClient[AvailableClientsAmount];
      _socket = new List<Socket>();
      _processesList = new List<Process>();
    }

    public int Receive()
    {
      InitializeUdpClients();
      var result = ReceiveFileData();
      return result;
    }

    private void InitializeUdpClients()
    {
      for (int i = 0; i < _udpFileReceiver.Length; i++)
      {
        _udpFileReceiver[i] = new UdpFileClient(LocalPort + i);
        _udpFileReceiver[i].Client.ReceiveTimeout = _udpFileReceiver[i].Client.SendTimeout = 10000;
        _socket.Add(_udpFileReceiver[i].Client);
      }
    }

    private int ReceiveFileDetails(IList<Socket> checkReadSocket)
    {
      foreach (var socket in checkReadSocket)
      {
        if (_fileDetails.ContainsKey(socket) == false)
        {
          MemoryStream memoryStream = new MemoryStream();

          try
          {
            var udpClient = _udpFileReceiver.First(s => s.Client == socket);
            var receivedFileInfo = udpClient.Receive(ref _remoteIpEndPoint);

            XmlSerializer serializer = new XmlSerializer(typeof(FileDetails));

            memoryStream.Write(receivedFileInfo, 0, receivedFileInfo.Length);
            memoryStream.Position = 0;

            var fileDetails = (FileDetails)serializer.Deserialize(memoryStream);
            _fileDetails.Add(socket, fileDetails);

            Console.WriteLine(fileDetails.FileName);
            Console.WriteLine(fileDetails.FileLength);
          }
          catch (Exception e)
          {
            for (int i = 0; i < _socket.Count; i++)
            {
              _socket[i].Close();
            }

            memoryStream.Dispose();
            Console.WriteLine(e.Message);
            return -1;
          }

          memoryStream.Dispose();
        }
      }

      return 0;
    }

    private int ReceiveFileData()
    {
      var availableToReadSockets = new List<Socket>();

      Semaphore semaphore = new Semaphore(0, 2, "sem");

      try
      {
        for (int i = 0; ;)
        {
          availableToReadSockets.Clear();
          //availableToReadSockets.AddRange(_udpFileReceiver.Select(r => r.Client));

          availableToReadSockets.Add(_udpFileReceiver[i].Client);

          Socket.Select(availableToReadSockets, null, null, SelectTimeout);

          if (availableToReadSockets.Any())
          {
            ReceiveFileDetails(availableToReadSockets);

            var processInfo = new ProcessStartInfo
            {
              FileName = ServerInstansePath,
              Arguments = _fileDetails[availableToReadSockets[0]].FileName + " "
                + _fileDetails[availableToReadSockets[0]].FileLength + " "
                + (LocalPort + i).ToString() + " "
                + _remoteIpEndPoint.Address.ToString() + " "
                + _remoteIpEndPoint.Port.ToString()
            };

            var childProcess = Process.Start(processInfo);
            _processesList.Add(childProcess);

            availableToReadSockets[0].Close();

            i++;
          }

          if (semaphore.WaitOne(3000))
          {
            Console.WriteLine("Exit");
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

      semaphore.Close();

      return 0;
    }

    private void CloseResources()
    {
      for (int i = 0; i < _socket.Count; i++)
      {
        _socket[i].Close();
        _udpFileReceiver[i].Close();
      }    
    }
  }
}
