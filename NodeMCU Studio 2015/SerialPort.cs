using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SP = System.IO.Ports.SerialPort;

namespace NodeMCU_Studio_2015
{
    class SerialPort : IDisposable
    {
        public static int Default_BaudRate = 9600;

        private static SerialPort _instance;
        public readonly SP CurrentSp;
        public readonly object Lock = new object();

        private const int MaxRetries = 100;

        //private int _baudrate=9600;
        //public int BaudRate
        //{
        //    get { return _baudrate; }
        //    set { _baudrate = value; CurrentSp.BaudRate = value; }
        //}

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
            if (IsOpenChanged != null)
            {
                IsOpenChanged.Invoke(CurrentSp.IsOpen);
            }
        }

        public bool Open(string port,int bautrate)
        {
            try
            {
                Close();
                CurrentSp.BaudRate = bautrate;
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

        private readonly Regex _end = new Regex("\n>+ $");


        public string Execute(string command)
        {
            //拦截ide命令
            if (command.IndexOf("ide_") == 0)
            {
                OnDataReceived.Invoke(command);
                return null;
            }

            var early = CurrentSp.ReadExisting();
            if (OnDataReceived != null)
            {
                OnDataReceived.Invoke(early);
            }

            var result = new StringBuilder();

            CurrentSp.WriteLine(command);
            for (var i = 0; i < MaxRetries; i++)
            {
                var s = CurrentSp.ReadExisting();
                result.Append(s);
                if (OnDataReceived != null) OnDataReceived.Invoke(s);
                if (_end.IsMatch(
                    result.ToString()))
                {
                    break;
                }
                Thread.Sleep(50);
            }
            var str = result.ToString();

            str = str.Trim();

            if (str.IndexOf(command + "\r\n>") == 0) { return null; }
            else if (str.IndexOf(command + "\r\n") == 0) { str = str.Substring(command.Length + 2); }

            if (str.IndexOf("\r\n>") >= 0)
            {
                str = str.Substring(0, str.IndexOf("\r\n>"));
            }
            return str;

            //return str.Substring(command.Length + 2, str.Length - 4 - command.Length );
        }

        public static SerialPort GetInstance()
        {
            return LazyInitializer.EnsureInitialized(ref _instance, () => new SerialPort());
        }

        public void FireIsWorkingChanged(bool state)
        {
            if (IsWorkingChanged == null) return;

            try
            {
                IsWorkingChanged(state);
            }
            catch
            {
                // ignored
            }
        }

        public void FireOnDataReceived(string s)
        {
            if (OnDataReceived == null) return;

            try
            {
                OnDataReceived(s);
            }
            catch
            {
                // ignored
            }
        }

        public event Action<bool> IsOpenChanged;
        public event Action<string> OnDataReceived;
        public event Action<bool> IsWorkingChanged;
    }
}
