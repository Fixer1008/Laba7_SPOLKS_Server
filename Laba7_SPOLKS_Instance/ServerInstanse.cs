using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    private const int PollTimeout = 10000000;

    private int _localPort;
    private int _remotePort;
    private long _fileLength;
    private string _fileName;
    private int _connectionFlag = 0;

    private IPAddress _remoteIPAddress;
    private IPEndPoint _remoteIpEndPoint;

    public int ReceiveData(string[] arguments)
    {
      if (ParseCommandLineArguments(arguments) == -1)
	    {
		    return -1;
	    }

      UdpFileClient udpFileClient = new UdpFileClient(_localPort);
      udpFileClient.Client.ReceiveTimeout = udpFileClient.Client.SendTimeout = 10000;

      _remoteIpEndPoint = new IPEndPoint(_remoteIPAddress, _remotePort);

      udpFileClient.Connect(_remoteIpEndPoint);

      if (udpFileClient.ActiveRemoteHost == false)
      {
        return -1;
      }

      var file = CreateNewFile(udpFileClient);

      var semaphore = Semaphore.OpenExisting("sem");

      for (file.Position = 0; file.Position < _fileLength; )
      {
        try
        {
          if (udpFileClient.Client.Poll(PollTimeout, SelectMode.SelectRead) == false)
          {
            Console.WriteLine("Poll");
            Console.WriteLine(semaphore.Release()); 
            return 0;
          }
          else
          {
            Console.WriteLine("No poll");
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
            Console.WriteLine("connectionFlag");
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

    private FileStream CreateNewFile(UdpFileClient udpFileClient)
    {
      var dotIndex = _fileName.IndexOf('.');
      var fileName = _fileName.Substring(0, dotIndex) + udpFileClient.Client.LocalEndPoint.GetHashCode() + _fileName.Substring(dotIndex);
      var file = new FileStream(fileName, FileMode.Create, FileAccess.Write);
      return file;
    }

    private int ParseCommandLineArguments(string[] arguments)
    {
      _fileName = arguments[0];

      if (long.TryParse(arguments[1], out _fileLength) == false)
      {
        return -1;
      }

      if (int.TryParse(arguments[2], out _localPort) == false)
	    {
		    return -1;
	    }

      if (IPAddress.TryParse(arguments[3], out _remoteIPAddress) == false)
      {
        return -1;
      }

      if (int.TryParse(arguments[4], out _remotePort) == false)
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

    private void ShowGetBytesCount(FileStream file)
    {
      Console.Clear();
      Console.Write("{0}: ", file.Name);
      Console.WriteLine(file.Position);
    }
  }
}
