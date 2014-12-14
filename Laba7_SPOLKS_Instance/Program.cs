using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net;
using Laba7_SPOLKS_Server;

namespace Laba7_SPOLKS_Instance
{
  class Program
  {
    static void Main(string[] args)
    {
      foreach (var arg in args)
      {
        Console.WriteLine(arg);
      }

      ServerInstanse serverInstanse = new ServerInstanse();
      var result = serverInstanse.ReceiveData(args);

      if (result == -1)
      {
        Console.WriteLine("Error!");
      }
      else
      {
        Console.WriteLine("Success!");
      }
    }
  }
}
