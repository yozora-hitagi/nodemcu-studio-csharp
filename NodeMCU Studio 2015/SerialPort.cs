using System;
using System.Text;
using System.Threading;
using SP = System.IO.Ports.SerialPort;

namespace NodeMCU_Studio_2015
{
    class SerialPort
    {
        private static SerialPort _instance;
        public readonly SP CurrentSp;
        public readonly object Lock = new object();

        private const int MaxRetries = 100;

        private SerialPort()
        {
            CurrentSp = new SP();
        }

        public string[] GetPortNames()
        {
            return SP.GetPortNames();
        }

        public void Close()
        {
            if (CurrentSp.IsOpen)
            {
                CurrentSp.Close();
                IsOpenChanged?.Invoke(CurrentSp.IsOpen);
            }
        }

        public bool Open(string port)
        {
            try
            {
                Close();
                CurrentSp.BaudRate = 9600;
                CurrentSp.ReadTimeout = 0;
                CurrentSp.PortName = port;
                CurrentSp.Open();
                IsOpenChanged?.Invoke(CurrentSp.IsOpen);
            }
            catch
            {
                // ignored
            }
            return CurrentSp.IsOpen;
        }

        public bool ExecuteAndWait(string command)
        {
            CurrentSp.WriteLine(command);
            for (var i = 0;i < MaxRetries;i++)
            {
                var s = CurrentSp.ReadExisting();
                OnDataReceived?.Invoke(s);
                if (s.Contains(">"))
                {
                    return true;
                }
                Thread.Sleep(100);
            }
            return false;
        }

        public string ExecuteWaitAndRead(string command)
        {
            var result = new StringBuilder();

            CurrentSp.WriteLine(command);
            for (var i = 0; i < MaxRetries; i++)
            {
                var s = CurrentSp.ReadExisting();
                result.Append(s);
                OnDataReceived?.Invoke(s);
                if (result.ToString().EndsWith("\n> "))
                {
                    break;
                }
                Thread.Sleep(100);
            }
            var str = result.ToString();
            return str.Substring(command.Length+2, str.Length-4-2-command.Length); // Kill the echo, '\r\n' and '\r\n> '
        }

        public static SerialPort GetInstance()
        {
            return LazyInitializer.EnsureInitialized(ref _instance, () => new SerialPort());
        }

        public void FireIsWorkingChanged(bool state)
        {
            IsWorkingChanged?.Invoke(state);
        }

        public event Action<bool> IsOpenChanged;
        public event Action<string> OnDataReceived;
        public event Action<bool> IsWorkingChanged;
    }
}
