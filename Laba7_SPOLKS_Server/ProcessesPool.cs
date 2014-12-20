using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Data;
using System.Security.Principal;
using System.ComponentModel;


namespace Laba7_SPOLKS_Server
{
  public class ProcessesPool
  {
    private const int Min = 2;
    private const int Max = 3;
    private const string ServerInstansePath = "..\\..\\..\\Laba7_SPOLKS_Instance\\bin\\Debug\\Laba7_SPOLKS_Instance.exe";
    
    private readonly Dictionary<int, Process> _processes;

    public ProcessesPool()
    {
      _processes = new Dictionary<int, Process>();
    }

    public int MinClientsAmount 
    {
      get { return Min; }
    }

    public int MaxClientsAmount 
    {
      get { return Max; }
    }

    public Dictionary<int, Process> Processes
    {
      get { return _processes; }
    }

    public void InitializePool(int localPort)
    {
      for (int i = 1; i < Min + 1; i++)
			{
        var processInfo = new ProcessStartInfo
        {
          FileName = ServerInstansePath,
          Arguments = (localPort + i).ToString()
        };

        _processes.Add(localPort + i, Process.Start(processInfo));
			}
    }

    public void StartProcess()
    {
      var port = _processes.Max(p => p.Key) + 1;

      var processInfo = new ProcessStartInfo
      {
        FileName = ServerInstansePath,
        Arguments = port.ToString()
      };

      _processes.Add(port, Process.Start(processInfo));
    }
  }
}
