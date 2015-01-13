using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NodeMCU_Studio_2015
{
    public class StringEventArgs : EventArgs {
        public string Text { get; private set; }
        public StringEventArgs( string str ) {
            this.Text = str;
        }
    }

    public class Workspace {
        private static Workspace instance = null;
        private SerialPort serialport = null;
        private bool isvalid = false;

        public event EventHandler IsValidChanged = null;
        public event EventHandler OnTimeout = null;
        public event EventHandler<StringEventArgs> OnDataReceived = null;

        public void Write( LuaProject proj , Action action = null) {
            try {
                this.Write( string.Format( "file.remove(\"{0}\")" , proj.FileName ) );
                var txt = this.Write( string.Format( "file.open(\"{0}\",\"w+\")" , proj.FileName ) );
                var lins = proj.SourceLine;
                foreach ( var line in lins ) {
                    var q_str = string.Format( "{0}{1}" , @"\" , "\"" );
                    var m_line = line.Replace( @"\" , @"\\" ).Replace( "\"" , q_str );
                    this.Write( string.Format( "file.writeline(\"{0}\")" , m_line ) );
                    if ( action != null ) {
                        action();
                    }
                }
                this.Write( string.Format( "file.close()" ) );
            } catch {
                if ( OnTimeout != null ) {
                    OnTimeout( this , EventArgs.Empty );
                }
            } finally {
            }
        }

        public void AttachTo( UIElement uielement ) {
            uielement.GotFocus += this.OpenSerialPort;
            uielement.LostFocus += this.CloseSerialPort;
        }

        private void OpenSerialPort( Object sender , RoutedEventArgs e ) {
            if ( !this.serialport.IsOpen ) {
                this.serialport.Open();
            }
        }

        private void CloseSerialPort( Object sender , RoutedEventArgs e ) {
            if ( this.serialport.IsOpen ) {
                this.serialport.Close();
            }
        }

        public void Execute( LuaProject proj ) {
            try {
                this.Write( string.Format( "dofile(\"{0}\")" , proj.FileName ) );
            } catch {
                if ( OnTimeout != null ) {
                    OnTimeout( this , EventArgs.Empty );
                }
            } finally {
            }
        }

        public bool IsValid {
            get { return this.isvalid; }
            private set {
                if ( this.isvalid != value ) {
                    this.isvalid = value;
                    if ( this.IsValidChanged != null ) {
                        this.IsValidChanged( this , EventArgs.Empty );
                    }
                }
            }
        }

        public string[] SerialPorts {
            get {
                return SerialPort.GetPortNames();
            }
        }

        public void Open( string name ) {
            this.serialport = new SerialPort( name );
            this.serialport.BaudRate = 9600;
            this.serialport.DataBits = 8;
            this.serialport.Parity = Parity.None;
            this.serialport.StopBits = StopBits.One;
            this.serialport.Encoding = Encoding.UTF8;
            this.serialport.NewLine = "\n";
            this.serialport.DataReceived += Serialport_DataReceived;
            this.serialport.Open();
            //Thread.Sleep( 1000 );
            //var dd = this.serialport.ReadExisting();
            //var t = this.Write( "print(\"hello\")" );
            //if (t.Contains( "hello" )) { //"
            //    this.IsValid = true;
            // } else {
            //    this.serialport.Close();
            //}
            this.IsValid = true;
        }

        public byte[] Write( byte key , int cnt = 1 ) {
            this.serialport.DataReceived -= Serialport_DataReceived;
            var keys = new byte[] { key };
            this.serialport.Write( keys , 0 , keys.Length );
            var data_to_read = new byte[cnt];
            int count = 0;
            do {
                count += ( byte ) this.serialport.Read( data_to_read , count , cnt - count );
            } while ( count < cnt );
            this.serialport.DataReceived += Serialport_DataReceived;
            return data_to_read;
        }

        public void WriteLine() {
            this.serialport.DataReceived -= Serialport_DataReceived;
            this.serialport.Write( this.serialport.NewLine );
            int cnt = 2;
            var data_to_read = new byte[cnt];
            int count = 0;
            do {
                count += ( byte ) this.serialport.Read( data_to_read , count , cnt - count );
            } while ( count < cnt );
            var datas = this.serialport.ReadExisting();
            if ( !string.IsNullOrEmpty( datas ) ) {
                if ( this.OnDataReceived != null ) {
                    this.OnDataReceived( this , new StringEventArgs( datas ) );
                }
            }
            this.serialport.DataReceived += Serialport_DataReceived;
        }


        public string Write( string cmd , bool appendline = true ) {
            var txt = string.Empty;
            if ( appendline ) {
                this.serialport.DataReceived -= Serialport_DataReceived;
                this.serialport.WriteLine( cmd );
                StringBuilder builder = new StringBuilder();
                char lst = 'a';
                DateTime time = DateTime.Now;
                DateTime time_end;
                string txt_temp;
                do {
                    builder.Append( this.serialport.ReadExisting() );
                    time_end = DateTime.Now;
                    if ( ( time_end.Subtract( time ).Seconds > 7 ) ) {
                        throw new TimeoutException();
                    }
                    if ( builder.Length < cmd.Length ) {
                        continue;
                    }
                    txt_temp = builder.ToString();
                    if ( txt_temp.Length < 2 ) {
                        continue;
                    }
                    lst = txt_temp[txt_temp.Length - 2];

                } while ( lst != '>' );
                txt = builder.ToString();
                if ( this.OnDataReceived != null ) {
                    this.OnDataReceived( this , new StringEventArgs( txt ) );
                }
                this.serialport.DataReceived += Serialport_DataReceived;
            } else {
                this.serialport.DataReceived -= Serialport_DataReceived;
                this.serialport.Write( cmd );
                StringBuilder builder = new StringBuilder();
                DateTime time = DateTime.Now;
                DateTime time_end;
                string txt_temp;
                do {
                    builder.Append( this.serialport.ReadExisting() );
                    time_end = DateTime.Now;
                    if ( ( time_end.Subtract( time ).Seconds > 7 ) ) {
                        throw new TimeoutException();
                    }
                    txt_temp = builder.ToString().Trim();
                    if ( txt_temp.Length == 0 ) {
                        continue;
                    }
                } while ( builder.Length < cmd.Length );
                txt = builder.ToString();
                this.serialport.DataReceived += Serialport_DataReceived;
            }
            return txt;
        }

        public List<string> Load() {
            try {
                List<string> list = new List<string>();
                var str = this.Write( "for k,v in pairs(file.list()) do print(k) end" );
                var files = str.Split( new string[] { "\r\n" } , StringSplitOptions.RemoveEmptyEntries );
                for ( int i = 1 ; i < files.Length - 1 ; i++ ) {
                    list.Add( files[i] );
                }
                return list;
            } catch {
                if ( OnTimeout != null ) {

                    OnTimeout( this , EventArgs.Empty );
                }
                return new List<string>();
            } finally {
            }
        }

        public string Load( String f ) {
            try {
                this.Write( string.Format( "file.open(\"{0}\")" , f ) );
                var cmd = string.Format( "repeat local line = file.readline() if line then line = (string.gsub(line,\"{0}\",\"\")) print(line) end until not line " , @"\n" );
                string text = this.Write( cmd );
                this.Write( "file.close()" );
                if ( text.Length > cmd.Length ) {
                    text = text.Remove( 0 , cmd.Length + 1 ).Trim();
                } else {
                    text = text.Remove( 0 , cmd.Length ).Trim();
                }
                var lines = text.Split( new string[] { "\r\n" } , StringSplitOptions.RemoveEmptyEntries );
                StringBuilder builder = new StringBuilder();
                for ( int i = 0 ; i < lines.Length - 1 ; i++ ) {
                    builder.AppendLine( lines[i] );
                }
                return builder.ToString();
            } catch {
                if ( OnTimeout != null ) {

                    OnTimeout( this , EventArgs.Empty );
                }
                return string.Empty;
            } finally {
            }
        }

        public void Delete( String file  ) {
            try {
                this.Write( string.Format( "file.remove(\"{0}\")" , file ) );
                File.Delete( this.LuaDeivceProgramsDirectory + file );
            } catch {
                if ( OnTimeout != null ) {
                    OnTimeout(this,EventArgs.Empty);
                }
            } finally {
            }
        }

        private void Serialport_DataReceived( Object sender , SerialDataReceivedEventArgs e ) {
            if ( this.OnDataReceived != null ) {
                this.OnDataReceived( this , new StringEventArgs( this.serialport.ReadExisting() ) );
            }
        }

        public void Close() {
            if ( this.serialport != null && this.serialport.IsOpen ) {
                this.serialport.Close();
            }
            this.IsValid = false;
        }

        public static Workspace Instance {
            get {
                if ( instance == null ) {
                    instance = new Workspace();
                }
                return instance;
            }
        }

        public Workspace() {
            this.RefreshWorkspace();
        }

        public void RefreshWorkspace() {
            this.file_list.Clear();
            this.LuaProgramsDirectory = "Lua Programs\\";
            this.LuaDeivceProgramsDirectory = "Lua Programs\\Device Files\\";
            var path = Path.GetFullPath( this.LuaProgramsDirectory );
            if ( !Directory.Exists( path ) ) {
                Directory.CreateDirectory( path );
            }
            var file = from e in Directory.GetFiles( path , "*.lua" )
                       select new LuaProject() { UnSaved = false , File = Path.GetFullPath( e ) };
            foreach ( var t in file ) {
                this.file_list.Add( t );
            }
        }

        public void Add( LuaProject project ) {
            var list = from e in this.file_list
                       where e.File == project.File
                       select e;
            if ( list.Count() > 0 ) {
                return;
            } else {
                this.file_list.Add( project );
            }
        }

        public string LuaProgramsDirectory { get; private set; }

        public string LuaDeivceProgramsDirectory { get; private set; }

        private ObservableCollection<LuaProject> file_list = new ObservableCollection<LuaProject>();

        public ObservableCollection<LuaProject> ProjectList {
            get {
                return this.file_list;
            }
        }

    }

    public class LuaProject
    {
        public bool UnSaved { get; set; }

        public string File { get; set; }

        public string FileName {
            get {
                return Path.GetFileName( File );
            }
        }

        public string SourceCode {
            get {
                if ( System.IO.File.Exists( this.File ) ) {
                    FileStream f = new FileStream( this.File , System.IO.FileMode.Open );
                    StreamReader reader = new StreamReader( f );
                    var tr= reader.ReadToEnd();
                    f.Close();
                    return tr;
                } else {
                    return string.Empty;
                }
            }
        }

        public string[] SourceLine {
            get {
                List<string> list = new List<string>();
                if ( System.IO.File.Exists( this.File ) ) {
                    FileStream f = new FileStream( this.File , System.IO.FileMode.Open );
                    StreamReader reader = new StreamReader( f );
                    while ( !reader.EndOfStream ) {
                        var t = reader.ReadLine();
                        list.Add( t );
                    }
                    f.Close();
                }
                return list.ToArray();
            }
        }

        public void Save( string code ) {
            FileStream f = new FileStream( this.File , System.IO.FileMode.Create );
            StreamWriter writer = new StreamWriter( f );
            writer.Write( code );
            writer.Flush();
            f.Close();
        }
    }
}
