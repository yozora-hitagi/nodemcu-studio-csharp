using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Antlr4.Runtime;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using Microsoft.Win32;

namespace NodeMCU_Studio_2015
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IDisposable
    {
        private static ViewModel _viewModel;
        private readonly IList<ICompletionData> _completionDatas;
        private readonly List<string> _keywords = new List<string>();
        private readonly List<string> _methods = new List<string>();
        private readonly List<string> _snippets = new List<string>();
        private static TaskScheduler _uiThreadScheduler;
        private CompletionWindow _completionWindow;
        private TextMarkerService _textMarkerService;
        private Dispatcher _uiDispatcher;
        private static Int32 _syntaxErrors = 0;
        private Image _disconnectedImage;
        private Image _connectedImage;
        private Image _disconnectedImageMenuItem;
        private Image _connectedImageMenuItem;
        private volatile String _backgroundThreadParam = "";
        private AutoResetEvent _backgroundThreadEvent = new AutoResetEvent(false);

        public static readonly RoutedUICommand DownloadCommand = new RoutedUICommand();
        public static readonly RoutedUICommand UploadCommand = new RoutedUICommand();
        public static readonly RoutedUICommand RunCommand = new RoutedUICommand();
        public static readonly RoutedUICommand DeleteCommand = new RoutedUICommand();
        public static readonly RoutedUICommand CompileCommand = new RoutedUICommand();

        public MainWindow()
        {
            InitializeComponent();
            //波特率列表
            UartBautRateComboBox.ItemsSource = new int[] { 9600, 19200, 38400, 57600, 74880, 115200, 230400, 460800, 921600 };
            UartBautRateComboBox.SelectedIndex = 0;
            

            Utilities.ResourceToList("Resources/keywords.setting", _keywords);
            Utilities.ResourceToList("Resources/methods.setting", _methods);
            Utilities.ResourceToList("Resources/snippets.setting", _snippets);

            _viewModel = DataContext as ViewModel;

            _completionDatas = new List<ICompletionData>();
            foreach (var method in _methods)
            {
                _completionDatas.Add(new CompletionData(method));
            }

            _uiThreadScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            _uiDispatcher = Dispatcher.CurrentDispatcher;

            RefreshSerialPort();

            InitConnect();

            SerialPort.GetInstance().IsWorkingChanged += delegate (bool isWorking)
            {
                EnsureWorkInUiThread(() =>
                {
                    CompileButton.IsEnabled = DeleteButton.IsEnabled = RunButton.IsEnabled = ConnectButton.IsEnabled = CommandTextBox.IsEnabled = UploadButton.IsEnabled = DownloadButton.IsEnabled = !isWorking;
                    CompileMenuItem.IsEnabled = DeleteMenuItem.IsEnabled = RunMenuItem.IsEnabled = ConnectMenuItem.IsEnabled = UploadMenuItem.IsEnabled = DownloadMenuItem.IsEnabled = !isWorking;
                });
            };

            SerialPort.GetInstance().OnDataReceived += delegate (string data)
            {
                EnsureWorkInUiThread(() =>
                {
                    ConsoleTextEditor.AppendText(data);
                    ConsoleTextEditor.ScrollToEnd();
                });
            };

            StartBackgroundSerialPortUpdateThread();

            StartBackgroundUpdateThread();
        }

        private void InitConnect()
        {
            // for a toolbar and menu bug...
            _disconnectedImage = Resources["DisconnectedImage"] as Image;
            _disconnectedImageMenuItem = Resources["DisconnectedImageMenuItem"] as Image;

            _connectedImage = Resources["ConnectedImage"] as Image;
            _connectedImageMenuItem = Resources["ConnectedImageMenuItem"] as Image;

            ConnectButton.Content = _disconnectedImage;
            ConnectMenuItem.Icon = _disconnectedImageMenuItem;

            SerialPort.GetInstance().IsOpenChanged += delegate (bool isOpen)
            {
                EnsureWorkInUiThread(() =>
                {
                    if (isOpen)
                    {
                        ConnectButton.Content = _connectedImage;
                        ConnectMenuItem.Icon = _connectedImageMenuItem;

                        SerialPortComboBox.IsEnabled = false;
                        UartBautRateComboBox.IsEnabled = false;
                    }
                    else
                    {
                        ConnectButton.Content = _disconnectedImage;
                        ConnectMenuItem.Icon = _disconnectedImageMenuItem;

                        SerialPortComboBox.IsEnabled = true;
                        UartBautRateComboBox.IsEnabled = true;
                    }
                });
            };
        }

        private static void StartBackgroundSerialPortUpdateThread()
        {
            new Task(() =>
            {
                while (true)
                {
                    if (SerialPort.GetInstance().CurrentSp.IsOpen)
                    {
                        lock (SerialPort.GetInstance().Lock)
                        {
                            var s = SerialPort.GetInstance().CurrentSp.ReadExisting();
                            if (!String.IsNullOrEmpty(s))
                                SerialPort.GetInstance().FireOnDataReceived(s);
                        }
                    }
                    Thread.Sleep(1000);
                }
            }).Start();
        }

        private void StartBackgroundUpdateThread()
        {
            new Task(() =>
            {
                while (true)
                {
                    _backgroundThreadEvent.WaitOne();
                    var text = _backgroundThreadParam;

                    var markers = new List<Int32>();
                    var markerPositions = new List<MarkerPosition>();

                    var newFoldings = CreateNewFoldings(text, ref markerPositions);

                    for (var i = 0; i < text.Length; i++)
                    {
                        if (text[i] > 255)
                        {
                            markers.Add(i);
                        }
                    }

                    EnsureWorkInUiThread(() =>
                    {
                        if (_textMarkerService != null)
                            _textMarkerService.RemoveAll(m => true);
                        _viewModel.Functions.Reset(newFoldings);
                        _viewModel.FoldingManager.UpdateFoldings(newFoldings, -1);

                        foreach (var offest in markers)
                        {
                            if (_textMarkerService != null)
                            {
                                var marker = _textMarkerService.Create(offest, 1);
                                marker.MarkerTypes = TextMarkerTypes.SquigglyUnderline;
                                marker.MarkerColor = Colors.Red;
                            }
                        }

                        foreach (var position in markerPositions)
                        {
                            var offest = _viewModel.Editor.Document.GetOffset(position.Line, position.Position);
                            if (offest >= _viewModel.Editor.Text.Length)
                                offest = _viewModel.Editor.Text.Length - 1;
                            if (_textMarkerService != null)
                            {
                                var marker = _textMarkerService.Create(offest, 1);
                                marker.MarkerTypes = TextMarkerTypes.SquigglyUnderline;
                                marker.MarkerColor = Colors.Red;
                            }
                        }
                    });
                }
            }).Start();
        }

        private void Update(string text)
        {
            _backgroundThreadParam = text;
            _backgroundThreadEvent.Set();

        }

        private static void EnsureWorkInUiThread(Action action)
        {
            var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            if (dispatcher != null)
            {
                action();
            }
            else
            {
                new Task(action).Start(_uiThreadScheduler);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        private void OnNewExecuted(object sender, RoutedEventArgs args)
        {
            CreateTab(null, null);
        }

        private void SelectAndDoAction(string name, Action<string> callback)
        {
            var window = new SelectFileWindow { SelectButton = { Content = name } };
            var result = "";

            DoSerialPortAction(
                () =>
                {
                    ExecuteWaitAndRead("for k, v in pairs(file.list()) do print(k) end",str => { result = str; });
                }, () =>
                {
                        window.FileListComboBox.ItemsSource = result.Replace("\r", "").Split('\n').Where(s => !String.IsNullOrEmpty(s));
                        window.FileListComboBox.SelectedIndex = 0;
                });

            window.SelectButton.Click += delegate
            {
                window.Close();
                var s = window.FileListComboBox.SelectedItem as String;

                if (s == null)
                {
                    MessageBox.Show("No file selected!", "NodeMCU Studio 2015", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.Yes);
                    return;
                }

                callback(s);
            };

            if (SerialPort.GetInstance().CurrentSp.IsOpen)
            {
                window.ShowDialog();
            }
            else
            {
                window.Close();
            }
        }

        private void OnRunExecuted(object sender, RoutedEventArgs args)
        {
            SelectAndDoAction("Run", (s) =>
            {
                DoSerialPortAction(
                    () => ExecuteWaitAndRead(string.Format("dofile(\"{0}\")", Utilities.Escape(s))));
            });
        }

        private void OnCompileExecuted(object sender, RoutedEventArgs args)
        {
            SelectAndDoAction("Compile", (s) =>
            {
                DoSerialPortAction(
                    () => ExecuteWaitAndRead(string.Format("node.compile(\"{0}\")", Utilities.Escape(s))));
            });
        }

        private void OnDeleteExecuted(object sender, RoutedEventArgs args)
        {
            SelectAndDoAction("Delete", (s) =>
            {
                ExecuteWaitAndRead(string.Format("file.remove(\"{0}\")", Utilities.Escape(s)));
            });
        }


        private void OnUploadExecuted(object sender, RoutedEventArgs args)
        {
            SelectAndDoAction("Upload", (s) =>
            {
                var res = "";

                DoSerialPortAction(
                () => ExecuteWaitAndRead(string.Format("file.open(\"{0}\", \"r\")", Utilities.Escape(s)), _ =>
                {
                    var builder = new StringBuilder();
                    while (true)
                    {
                        try
                        {
                            ExecuteWaitAndRead("print(file.readline())", line =>
                            {
                                if (String.IsNullOrEmpty(line)) throw new Exception();

                                builder.Append(line);
                            });
                        }
                        catch (Exception) { break; }
                    }
                    res = builder.ToString();
                    ExecuteWaitAndRead("file.close()");

                }), () =>
                {
                    CreateTab(null, res);
                    CurrentTabItem.Text = res;
                });
            });
        }

        private void DoSerialPortAction(Action callback)
        {
            DoSerialPortAction(callback, () => { });
        }

        private void DoSerialPortAction(Action callback, Action cleanup)
        {
            if (!EnsureSerialPortOpened())
            {
                return;
            }

            var task = new Task(() =>
            {
                lock (SerialPort.GetInstance().Lock)
                {
                    SerialPort.GetInstance().FireIsWorkingChanged(true);
                    try
                    {
                        callback();
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(string.Format("Operation failed: {0}", exception), "NodeMCU Studio 2015", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                    }

                }
                SerialPort.GetInstance().FireIsWorkingChanged(false);
            });

            task.ContinueWith(_ =>
            {
                if (Application.Current.Windows.Count == 1)
                {
                    Activate();
                }
                cleanup();
            }, TaskScheduler.FromCurrentSynchronizationContext());
            task.Start();
        }

 
        private static void ExecuteWaitAndRead(string command, Action<string> callback)
        {
            var line = SerialPort.GetInstance().Execute(command);
            line = null == line ? "" : line;
            callback(line);
        }

        private static bool ExecuteWaitAndRead(string command)
        {
            ExecuteWaitAndRead(command, _ => { });
            return true;
        }

        private static IEnumerable<String> ReadLinesFrom(String s)
        {
           // var regex = new Regex("\\s*--[^[]");
            using (var reader = new StringReader(s))
            {
                String line;
                while ((line = reader.ReadLine()) != null)
                {
                    //if (regex.IsMatch(line))
                    //{
                    //    continue;
                    //}
                    yield return line;
                }
            }
        }

        private void OnDownloadExecuted(object sender, RoutedEventArgs args)
        {
            if (_viewModel.TabItems.Count == 0)
            {
                MessageBox.Show("Open a file first!", "NodeMCU Studio 2015", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                return;
            }

            var filename = CurrentTabItem.FileName;
            if (filename == null)
            {
                if (!SaveFile())
                    return;
                else
                    filename = CurrentTabItem.FileName;
            }

            if (_syntaxErrors > 0)
            {
                if (
                    MessageBox.Show("Syntax errors found in this file. Download anyway?", "NodeMCU Studio 2015",
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.No)
                {
                    return;
                }
            }

            var text = CurrentTabItem.Text;
            text = Utilities.ToDBC(text);

            if (Encoding.Default.GetByteCount(text) != text.Length)
            {
                if (
                    MessageBox.Show("Wide characters found in this file which is not supported by Lua. Download anyway?", "NodeMCU Studio 2015",
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.No)
                {
                    return;
                }
            }

            DoSerialPortAction(
                () => ExecuteWaitAndRead(string.Format("file.remove(\"{0}\")", Utilities.Escape(filename)), _ =>
                    ExecuteWaitAndRead(string.Format("file.open(\"{0}\", \"w+\")", Utilities.Escape(filename)), __ =>
                    {
                        if (
                            ReadLinesFrom(CurrentTabItem.Text)
                                .Any(
                                    line =>
                                        !ExecuteWaitAndRead(string.Format("file.writeline(\"{0}\")",
                                                Utilities.Escape(line)))))
                        {
                            ExecuteWaitAndRead("file.close()");
                            MessageBox.Show("Download to device failed!", "NodeMCU Studio 2015", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.Yes);
                        }
                        else
                        {
                            if (!ExecuteWaitAndRead("file.close()"))
                            {
                                MessageBox.Show("Download to device succeeded!", "NodeMCU Studio 2015", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.Yes);
                            }
                        }
                    })));
        }

        private void OnOpenExecuted(object sender, RoutedEventArgs args)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = "Lua files (*.lua)|*.lua|All files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var filename in dialog.FileNames)
                    CreateTab(filename,null);
            }
        }

        private void OnSaveExecuted(object sender, RoutedEventArgs args)
        {
            SaveFile();
            CurrentTabItem.IsEdited = false;
        }

        private Boolean SaveFile()
        {
            if (CurrentTabItem.FilePath != null)
            {
                File.WriteAllText(CurrentTabItem.FilePath, _viewModel.Editor.Text);
            }
            else
            {
                var dialog = new SaveFileDialog()
                {
                    Filter = "Lua files (*.lua)|*.lua|All files (*.*)|*.*"
                };
                if (dialog.ShowDialog() != true) return false;
                CurrentTabItem.FilePath = dialog.FileName;
                File.WriteAllText(CurrentTabItem.FilePath, _viewModel.Editor.Text);
            }
            return true;
        }

        private void OnSaveCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = false;
            if (_viewModel != null)
                args.CanExecute = _viewModel.TabItems.Count != 0;
        }

        private void OnCopy()
        {
        }

        private void OnPaste()
        {
        }

        private void OnToggleConnect(object sender, RoutedEventArgs args)
        {
            if (SerialPort.GetInstance().CurrentSp.IsOpen)
            {
                SerialPort.GetInstance().Close();
            }
            else
            {
                EnsureSerialPortOpened();
            }
        }

        private Boolean EnsureSerialPortOpened()
        {
            if (SerialPortComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a serial port or plug the device first!", "NodeMCU Studio 2015", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                return false;
            }else if (UartBautRateComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a bautrate first!", "NodeMCU Studio 2015", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                return false;
            }
            else if (!SerialPort.GetInstance().Open(SerialPortComboBox.SelectedItem.ToString(), (int)UartBautRateComboBox.SelectedValue))
            {
                MessageBox.Show("Cannot open serial port!", "NodeMCU Studio 2015", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                return false;
            }
            return true;
        }

        private void OnRefreshExecuted(object sender, EventArgs args)
        {
            RefreshSerialPort();
        }

        private void RefreshSerialPort()
        {
            SerialPortComboBox.ItemsSource = SerialPort.GetPortNames();
        }

        private void CreateTab(string fileName,string content)
        {
            try
            {
                var tabItem = new TabItem
                {
                    FilePath = fileName,
                    Index = _viewModel.TabItems.Count
                };

                if (fileName != null)
                {
                    // Maybe some better way is needed.
                    tabItem.Text = File.ReadAllText(fileName);
                }

                _viewModel.TabItems.Add(tabItem);
                _viewModel.CurrentTabItemIndex = _viewModel.TabItems.Count - 1;
                CurrentTabItem.IsEdited = false;

                if (content != null)
                {
                    tabItem.Text = content;
                }

            }
            catch (Exception ex)
            {
                if (
                    MessageBox.Show(ex.Message, "Create file failed. Retry?", MessageBoxButton.YesNo,
                        MessageBoxImage.Error, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                    CreateTab(fileName,content);
            }
        }

        private TabItem CurrentTabItem
        {
            get
            {
                return _viewModel.CurrentTabItemIndex == -1 ? null : _viewModel.TabItems[_viewModel.CurrentTabItemIndex];
            }
        }

        private void OnEditorLoaded(object sender, RoutedEventArgs e)
        {
            var editor = e.Source as TextEditor;
            if (editor == null) return;

            _viewModel.Editor = editor;
            editor.Text = CurrentTabItem.Text;
            Update(CurrentTabItem.Text);
            CurrentTabItem.IsEdited = false;
            editor.TextArea.TextEntered += TextEntered;
            editor.TextArea.TextEntering += TextEntering;

            _viewModel.FoldingManager = FoldingManager.Install(editor.TextArea);

            _textMarkerService = new TextMarkerService(editor.Document);
            editor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            editor.TextArea.TextView.LineTransformers.Add(_textMarkerService);
            var services = editor.Document.ServiceProvider.GetService(typeof(IServiceContainer)) as IServiceContainer;
            if (services != null)
                services.AddService(typeof(ITextMarkerService), _textMarkerService);
        }

        private void TextEntered(object sender, TextCompositionEventArgs e)
        {
            var text = _viewModel.Editor.Text;
            if (e.Text == ".")
            {
                var index = _viewModel.Editor.CaretOffset - 1;
                while (index > 0)
                {
                    if (Char.IsLetterOrDigit(text[index]) || text[index] == '.')
                    {
                        index--;
                    }
                    else
                    {
                        break;
                    }
                }
                var s = text.Substring(index + 1, _viewModel.Editor.CaretOffset - index - 1);
                _completionWindow = new CompletionWindow(_viewModel.Editor.TextArea) { CloseWhenCaretAtBeginning = true, StartOffset = index + 1 };
                foreach (var item in _completionDatas.AsParallel().Where(x => x.Text.Contains(s)))
                {
                    _completionWindow.CompletionList.CompletionData.Add(item);
                }
                if (_completionWindow.CompletionList.CompletionData.Count == 0)
                {
                    return;
                }
                _completionWindow.Show();
                _completionWindow.Closed += delegate { _completionWindow = null; };
            }
            Update(text);
        }

        private class MyErrorListener : BaseErrorListener
        {
            private readonly TextMarkerService _textMarkerService;
            private readonly List<MarkerPosition> _markers;
            public MyErrorListener(TextMarkerService service, ref List<MarkerPosition> markers)
            {
                _textMarkerService = service;
                _markers = markers;
            }

            public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException recognitionException)
            {
                _markers.Add(new MarkerPosition(line, charPositionInLine));
            }
        }

        private struct MarkerPosition
        {
            public Int32 Line;
            public Int32 Position;

            public MarkerPosition(int line, int position)
            {
                Line = line;
                Position = position;
            }
        }

        private List<NewFolding> CreateNewFoldings(String text, ref List<MarkerPosition> markers)
        {
            List<NewFolding> newFoldings = null;
            try
            {
                using (var reader = new StringReader(text))
                {
                    var antlrInputStream = new AntlrInputStream(reader);
                    var lexer = new LuaLexer(antlrInputStream);
                    var tokens = new CommonTokenStream(lexer);
                    var parser = new LuaParser(tokens) { BuildParseTree = true };
                    parser.RemoveErrorListeners();
                    parser.AddErrorListener(new MyErrorListener(_textMarkerService, ref markers));
                    var tree = parser.block();
                    var visitor = new LuaVisitor();
                    newFoldings = visitor.Visit(tree);
                    Interlocked.Exchange(ref _syntaxErrors, parser.NumberOfSyntaxErrors);
                }
            }
            catch (Exception e)
            {
                // MessageBox.Show(e.ToString(), "NodeMCU Studio 2015", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                //On error resume next
            }

            return newFoldings ?? new List<NewFolding>();
        }

        private void TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && _completionWindow != null)
            {
                if (!Char.IsLetterOrDigit(e.Text[0]))
                {
                    _completionWindow.CompletionList.RequestInsertion(e);
                }
            }
        }

        private void OnObjectExplorerItemDoubleClick(object sender, RoutedEventArgs e)
        {
            var folding = ObjectExplorerListBox.SelectedItem as NewFolding;
            if (folding != null) _viewModel.Editor.CaretOffset = folding.StartOffset;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate
            {
                _viewModel.Editor.TextArea.Caret.BringCaretToView();
                _viewModel.Editor.TextArea.Caret.Show();
                Keyboard.Focus(_viewModel.Editor);
            }));
        }

        private class LuaVisitor : LuaBaseVisitor<List<NewFolding>>
        {
            public override List<NewFolding> VisitFunctiondefinition(LuaParser.FunctiondefinitionContext context)
            {
                var funcName = context.funcname().GetText();
                var newFolding = new NewFolding
                {
                    StartOffset = context.Start.StartIndex,
                    EndOffset = context.Stop.StopIndex + 1,
                    Name = funcName
                };
                var foldings = new List<NewFolding> { newFolding };
                var children = base.VisitFunctiondefinition(context);
                if (children != null)
                {
                    foldings.AddRange(children);
                }
                return foldings;
            }

            protected override List<NewFolding> AggregateResult(List<NewFolding> aggregate, List<NewFolding> nextResult)
            {
                var foldings = new List<NewFolding>();
                if (aggregate != null)
                {
                    foldings.AddRange(aggregate);
                }

                if (nextResult != null)
                {
                    foldings.AddRange(nextResult);
                }
                return foldings;
            }
        }

        private void Editor_OnTextChanged(object sender, EventArgs e)
        {
            CurrentTabItem.Text = _viewModel.Editor.Text;
            Update(CurrentTabItem.Text);
            CurrentTabItem.IsEdited = true;
        }

        private void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel.Editor == null || CurrentTabItem == null) return;

            Boolean oldIsEdited = CurrentTabItem.IsEdited;
            _viewModel.Editor.Text = CurrentTabItem.Text;
            Update(CurrentTabItem.Text);
            CurrentTabItem.IsEdited = oldIsEdited;
        }

        private void OnCloseTab(object sender, RoutedEventArgs e)
        {
            if (CurrentTabItem.IsEdited)
            {
                var result = MessageBox.Show(
                        String.Format("File {0} not saved. Save it?", CurrentTabItem.FileName), "NodeMCU Studio 2015",
                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (result == MessageBoxResult.Yes)
                {
                    SaveFile();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            var button = sender as Button;
            if (button != null)
            {
                var index = (Int32)button.Tag;
                if (_viewModel.TabItems.Count == 1)
                {
                    Update("");
                }
                _viewModel.TabItems.RemoveAt(index);
                var i = 0;
                foreach (var item in _viewModel.TabItems)
                {
                    item.Index = i++;
                }
            }
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var text = CommandTextBox.Text;
            CommandTextBox.Text = "";
            DoSerialPortAction(() =>
            {
                ExecuteWaitAndRead(text);
            }, () =>
            {
                CommandTextBox.Focus();
            });
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            foreach (var item in _viewModel.TabItems)
            {
                if (item.IsEdited)
                {
                    var result = MessageBox.Show(
                        String.Format("File {0} not saved. Save it?", item.FileName), "NodeMCU Studio 2015",
                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                    if (result == MessageBoxResult.Yes)
                    {
                        File.WriteAllText(item.FilePath, item.Text);
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
            }
        }

    }

}