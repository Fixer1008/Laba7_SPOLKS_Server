using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Laba7_SPOLKS_Server
{
  class Program
  {
    static void Main(string[] args)
    {
      var ipAddress = Dns.GetHostAddresses("ALEX-NOTE");

      Console.WriteLine(ipAddress[1]);
      Console.WriteLine("Waiting for file receiving...");

      FileReceiver fileReceiver = new FileReceiver();
      var result = fileReceiver.Receive();

      if (result == -1)
      {
        Console.WriteLine("Error!");
      }
      else
      {
        Console.WriteLine("Success!");
      }

      Console.ReadLine();
    }
  }
}
