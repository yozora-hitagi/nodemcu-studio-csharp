using System;
using System.Text;
using System.Threading;
using SP = System.IO.Ports.SerialPort;

namespace NodeMCU_Studio_2015
{
    class SerialPort : IDisposable
    {
        private static SerialPort _instance;
        public readonly SP CurrentSp;
        public readonly object Lock = new object();

        private const int MaxRetries = 100;

        private SerialPort()
        {
            CurrentSp = new SP();
        }

        public static string[] GetPortNames()
        {
            return SP.GetPortNames();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && CurrentSp != null)
                CurrentSp.Dispose();
        }

        public void Close()
        {
            if (!CurrentSp.IsOpen) return;

            CurrentSp.Close();
            if (IsOpenChanged != null) {
                IsOpenChanged.Invoke(CurrentSp.IsOpen);
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
                if (IsOpenChanged != null) IsOpenChanged(CurrentSp.IsOpen);
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
                if (OnDataReceived != null) OnDataReceived(s);
                if (s.Contains(">"))
                {
                    return true;
                }
                Thread.Sleep(50);
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
                if (OnDataReceived != null) OnDataReceived.Invoke(s);
                if (result.ToString().EndsWith("\n> "))
                {
                    break;
                }
                Thread.Sleep(50);
            }
            var str = result.ToString();
            return str.Substring(command.Length+2, str.Length-4-command.Length); // Kill the echo, '\r\n> '
        }

        public static SerialPort GetInstance()
        {
            return LazyInitializer.EnsureInitialized(ref _instance, () => new SerialPort());
        }

        public void FireIsWorkingChanged(bool state)
        {
            if (IsWorkingChanged != null) IsWorkingChanged(state);
        }

        public event Action<bool> IsOpenChanged;
        public event Action<string> OnDataReceived;
        public event Action<bool> IsWorkingChanged;
    }
}
