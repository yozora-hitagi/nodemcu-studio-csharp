using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    public partial class MainWindow : Window, IDisposable
    {
        private static ViewModel _viewModel;
        private readonly IList<ICompletionData> _completionDatas;
        private readonly List<string> _keywords = new List<string>();
        private readonly List<string> _methods = new List<string>();
        private readonly List<string> _snippets = new List<string>();
        private readonly TaskScheduler _uiThreadScheduler;
        private CompletionWindow _completionWindow;

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
                if (dialog.ShowDialog() == true)
                {
                    CurrentTabItem.FilePath = dialog.FileName;
                    File.WriteAllText(CurrentTabItem.FilePath, _viewModel.Editor.Text);
                }
            }
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

        private void CreateTab(string fileName)
        {
            try
            {
                var tabItem = new TabItem
                {
                    FilePath = fileName
                };

                if (fileName != null)
                {
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
            editor.TextArea.TextEntered += TextEntered;
            editor.TextArea.TextEntering += TextEntering;

            _viewModel.FoldingManager = FoldingManager.Install(editor.TextArea);
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

        private void Update(string text)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                var newFoldings = CreateNewFoldings(text);
                new Task(() =>
                {
                    _viewModel.Functions.Clear();
                    foreach (var folding in newFoldings)
                    {
                        _viewModel.Functions.Add(folding);
                    }
                    _viewModel.FoldingManager.UpdateFoldings(newFoldings, -1);
                }).Start(_uiThreadScheduler);
            });
        }

        private static List<NewFolding> CreateNewFoldings(String text)
        {
            List<NewFolding> newFoldings = null;
            using (var reader = new StringReader(text))
            {
                var antlrInputStream = new AntlrInputStream(reader);
                var lexer = new LuaLexer(antlrInputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new LuaParser(tokens) {BuildParseTree = true};
                var tree = parser.block();
                var visitor = new LuaVisitor();
                newFoldings = visitor.Visit(tree);
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
            if (_viewModel.Editor != null && CurrentTabItem != null)
            {
                _viewModel.Editor.Text = CurrentTabItem.Text;
                Update(CurrentTabItem.Text);
            }
        }

        private void OnCloseTab(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Editor != null && CurrentTabItem != null && _viewModel.TabItems.Count >= 2)
            {
                _viewModel.Editor.Text = CurrentTabItem.Text;
                Update(CurrentTabItem.Text);
            } else if (_viewModel.Editor != null)
            {
                _viewModel.Editor.Text = "";
                Update("");
            }
            _viewModel.TabItems.RemoveAt(_viewModel.CurrentTabItemIndex);
        }
    }
}