using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
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

        public static readonly RoutedUICommand DownloadCommand = new RoutedUICommand();
        public static readonly RoutedUICommand UploadCommand = new RoutedUICommand();

        public MainWindow()
        {
            InitializeComponent();

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

            // for a toolbar and menu bug...
            _disconnectedImage = Resources["DisconnectedImage"] as Image;
            _disconnectedImageMenuItem = Resources["DisconnectedImage"] as Image;

            _connectedImage = Resources["ConnectedIamge"] as Image;
            _connectedImageMenuItem = Resources["ConnectedIamge"] as Image;

            ConnectMenuItem.Icon = ConnectButton.Content = _connectedImage;

            SerialPort.GetInstance().IsOpenChanged += delegate (bool isOpen)
            {
                EnsureWorkInUiThread(() =>
                {
                    if (isOpen)
                    {
                        ConnectButton.Content = _connectedImage;
                        ConnectMenuItem.Icon = _connectedImageMenuItem;
                    }
                    else
                    {
                        ConnectButton.Content = _disconnectedImage;
                        ConnectMenuItem.Icon = _disconnectedImageMenuItem;
                    }
                });
            };

            SerialPort.GetInstance().IsWorkingChanged += delegate(bool isWorking)
            {
                EnsureWorkInUiThread(() =>
                {
                    ConnectButton.IsEnabled = CommandTextBox.IsEnabled = UploadButton.IsEnabled = DownloadButton.IsEnabled = !isWorking;
                    ConnectMenuItem.IsEnabled = UploadMenuItem.IsEnabled = DownloadMenuItem.IsEnabled = !isWorking;
                });
            };

            SerialPort.GetInstance().OnDataReceived += delegate(string data)
            {
                EnsureWorkInUiThread(() =>
                {
                    ConsoleTextEditor.AppendText(data);
                    ConsoleTextEditor.ScrollToEnd();
                });
            };

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

        private static void EnsureWorkInUiThread(Action action)
        {
            var dispatcher = System.Windows.Threading.Dispatcher.FromThread(Thread.CurrentThread);
            if (dispatcher != null)
            {
                action();
            } else
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
            CreateTab(null);
        }

        private void OnUploadExecuted(object sender, RoutedEventArgs args)
        {
            var window = new UploadWindow();
            var result = "";

            DoSerialPortAction(
                () =>
                {
                    ExecuteWaitAndRead("for k, v in pairs(file.list()) do", _ =>
                        ExecuteWaitAndRead("print(k)", __ => ExecuteWaitAndRead("end", str => { result = str; })));
                }, () => { window.FileListComboBox.ItemsSource = result.Replace("\r","").Split('\n'); });

            window.UploadButton.Click += delegate
            {
                window.Close();
                var s = window.FileListComboBox.SelectedItem as String;

                if (s == null)
                {
                    MessageBox.Show("No file selected!");
                    return;
                }

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
                                builder.Append(line);
                            });
                        }
                        catch (IgnoreMeException)
                        {
                            // ignore
                            break;
                        }
                    }
                    res = builder.ToString();
                    SerialPort.GetInstance()
                        .ExecuteAndWait("file.close()");

                }), () =>
                {
                    CreateTab(null);
                    CurrentTabItem.Text = res;
                });

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
                    catch (IgnoreMeException)
                    {
                        // Ignore me.
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(string.Format("Operation failed: {0}", exception));
                    }

                }
                SerialPort.GetInstance().FireIsWorkingChanged(false);
            });

            task.ContinueWith(_ => cleanup(), TaskScheduler.FromCurrentSynchronizationContext());
            task.Start();
        }

        private static void ExecuteWaitAndRead(string command, Action<string> callback)
        {
            var line = SerialPort.GetInstance().ExecuteWaitAndRead(command);
            if (line.Length == 2 /* \r and \n */ || line.Equals("stdin:1: open a file first\r\n"))
            {
                //MessageBox.Show(Resources.operation_failed);
                throw new IgnoreMeException();
            }
            callback(line);
        }

        private static void ExecuteWaitAndRead(string command)
        {
            ExecuteWaitAndRead(command, _ => { });
        }

        private static IEnumerable<String> ReadLinesFrom(String s)
        {
            using (var reader = new StringReader(s))
            {
                String line;
                while ((line = reader.ReadLine()) != null)
                    yield return line;
            }
        } 

        private void OnDownloadExecuted(object sender, RoutedEventArgs args)
        {
            if (_viewModel.TabItems.Count == 0)
            {
                MessageBox.Show("Open a file first!");
                return;
            }

            var filename = CurrentTabItem.FileName;
            if (filename == null)
            {
                if (!SaveFile())
                    return;
            }

            if (_syntaxErrors > 0)
            {
                if (
                    MessageBox.Show("Syntax errors found in this file. Download anyway?", "NodeMCU Studio 2015",
                        MessageBoxButton.YesNo) == MessageBoxResult.No)
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
                                        !SerialPort.GetInstance()
                                            .ExecuteAndWait(string.Format("file.writeline(\"{0}\")",
                                                Utilities.Escape(line)))))
                        {
                            SerialPort.GetInstance().ExecuteAndWait("file.close()");
                            MessageBox.Show("Download to device failed!");
                        }
                        else
                        {
                            MessageBox.Show(!SerialPort.GetInstance().ExecuteAndWait("file.close()")
                                ? "Download to device failed!"
                                : "Download to device succeeded!");
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
                    CreateTab(filename);
            }
        }

        private void OnSaveExecuted(object sender, RoutedEventArgs args)
        {
            SaveFile();
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
                MessageBox.Show("Please select a serial port or plug the device first!");
                return false;
            }
            else if (!SerialPort.GetInstance().Open(SerialPortComboBox.SelectedItem.ToString()))
            {
                MessageBox.Show("Cannot open serial port!");
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
            var ports = SerialPort.GetPortNames();
            SerialPortComboBox.ItemsSource = ports;
            if (ports.Length != 0)
            {
                SerialPortComboBox.IsEnabled = true;
            }
            else
            {
                var source = new string[1];
                source[0] = "No Device";
                SerialPortComboBox.ItemsSource = source;
                SerialPortComboBox.IsEnabled = false;
            }
            SerialPortComboBox.SelectedIndex = 0;
        }

        private void CreateTab(string fileName)
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
            }
            catch (Exception ex)
            {
                if (
                    MessageBox.Show(ex.Message, "Create file failed. Retry?", MessageBoxButton.YesNo,
                        MessageBoxImage.Error) == MessageBoxResult.Yes)
                    CreateTab(fileName);
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
            editor.TextArea.TextEntered += TextEntered;
            editor.TextArea.TextEntering += TextEntering;

            _viewModel.FoldingManager = FoldingManager.Install(editor.TextArea);

            _textMarkerService = new TextMarkerService(editor.Document);
            editor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            editor.TextArea.TextView.LineTransformers.Add(_textMarkerService);
            var services = editor.Document.ServiceProvider.GetService(typeof (IServiceContainer)) as IServiceContainer;
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
                _completionWindow = new CompletionWindow(_viewModel.Editor.TextArea) { CloseWhenCaretAtBeginning = true, StartOffset = index + 1 };
                foreach (var item in _completionDatas)
                {
                    _completionWindow.CompletionList.CompletionData.Add(item);
                }
                _completionWindow.Show();
                _completionWindow.Closed += delegate { _completionWindow = null; };
            }
            Update(text);
        }

        // Potential in multithread? 
        private void Update(string text)
        {
            if (_textMarkerService != null)
                _textMarkerService.RemoveAll(m => true);

            ThreadPool.QueueUserWorkItem(delegate
            {
                var newFoldings = CreateNewFoldings(text);
                EnsureWorkInUiThread(() =>
                {
                    _viewModel.Functions.Clear();
                    foreach (var folding in newFoldings)
                    {
                        _viewModel.Functions.Add(folding);
                    }
                    _viewModel.FoldingManager.UpdateFoldings(newFoldings, -1);
                });
            });
        }

        private class MyErrorListener : BaseErrorListener
        {
            private readonly TextMarkerService _textMarkerService;
            public MyErrorListener(TextMarkerService service)
            {
                _textMarkerService = service;
            }

            public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException recognitionException)
            {
                EnsureWorkInUiThread(() =>
                {
                    var offest = _viewModel.Editor.Document.GetOffset(line, charPositionInLine);
                    _textMarkerService.RemoveAll(m => true);
                    var marker = _textMarkerService.Create(offest, 1);
                    marker.MarkerTypes = TextMarkerTypes.SquigglyUnderline;
                    marker.MarkerColor = Colors.Red;
                });
                
            }
        }

        private List<NewFolding> CreateNewFoldings(String text)
        {
            List<NewFolding> newFoldings = null;
            try
            {
                using (var reader = new StringReader(text))
                {
                    var antlrInputStream = new AntlrInputStream(reader);
                    var lexer = new LuaLexer(antlrInputStream);
                    var tokens = new CommonTokenStream(lexer);
                    var parser = new LuaParser(tokens) {BuildParseTree = true};
                    parser.RemoveErrorListeners();
                    parser.AddErrorListener(new MyErrorListener(_textMarkerService));
                    var tree = parser.block();
                    var visitor = new LuaVisitor();
                    newFoldings = visitor.Visit(tree);
                    Interlocked.Exchange(ref _syntaxErrors, parser.NumberOfSyntaxErrors);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
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
                var foldings = new List<NewFolding> {newFolding};
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
        }

        private void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel.Editor == null || CurrentTabItem == null) return;

            _viewModel.Editor.Text = CurrentTabItem.Text;
            Update(CurrentTabItem.Text);
        }

        private void OnCloseTab(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var index = (Int32) button.Tag;
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
            });
        }
    }

    [Serializable]
    internal class IgnoreMeException : Exception
    {
    }
}