using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Antlr4.Runtime;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using MessageBox = System.Windows.MessageBox;

namespace NodeMCU_Studio_2015
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private readonly List<string> _keywords = new List<string>();
        private readonly List<string> _methods = new List<string>();
        private readonly List<string> _snippets = new List<string>();
        public static ViewModel ViewModel;
        private readonly IList<ICompletionData> _completionDatas;
        private readonly TaskScheduler _uiThreadScheduler;

        public MainWindow()
        {
            InitializeComponent();

            Utilities.ResourceToList("Resources/keywords.setting", _keywords);
            Utilities.ResourceToList("Resources/methods.setting", _methods);
            Utilities.ResourceToList("Resources/snippets.setting", _snippets);

            ViewModel = DataContext as ViewModel;

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

        private void OnNew(object sender, RoutedEventArgs args)
        {
            CreateTab(null);
        }

        private void OnCut()
        {
        }

        private void OnCopy()
        {
        }

        private void OnPaste()
        {
        }

        private void OnOpen(object sender, RoutedEventArgs args)
        {
            
        }

        private void CreateTab(string fileName)
        {
            try
            {
                var tabItem = new TabItem
                {
                    Title = fileName ?? "[new]"
                };
                ViewModel.TabItems.Add(tabItem);
                ViewModel.CurrentTabItemIndex = ViewModel.TabItems.Count - 1;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Create file failed. Retry?", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    CreateTab(fileName);
            }
        }

        private void OnEditorLoaded(object sender, RoutedEventArgs e)
        {
            var editor = e.Source as TextEditor;
            if (editor == null) return;

            CurrentEditor = editor;
            editor.TextArea.TextEntered += TextEntered;
            editor.TextArea.TextEntering += TextEntering;

            if (CurrentManager != null)
            {
                FoldingManager.Uninstall(CurrentManager);
            }
            CurrentManager = FoldingManager.Install(editor.TextArea);

            Update(CurrentEditor.Text);
        }

        private TextEditor CurrentEditor 
        {
            get
            {
                return ViewModel.TabItems[ViewModel.CurrentTabItemIndex].Editor;
            }
            set { ViewModel.TabItems[ViewModel.CurrentTabItemIndex].Editor = value; }
        }

        private FoldingManager CurrentManager
        {
            get
            {
                return ViewModel.TabItems[ViewModel.CurrentTabItemIndex].Manager;
            }
            set { ViewModel.TabItems[ViewModel.CurrentTabItemIndex].Manager = value; }
        }

        private CompletionWindow _completionWindow;

        private void TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ".")
            {
                _completionWindow = new CompletionWindow(CurrentEditor.TextArea);
                foreach (var item in _completionDatas)
                {
                    _completionWindow.CompletionList.CompletionData.Add(item);
                }
                _completionWindow.Show();
                _completionWindow.Closed += delegate { _completionWindow = null; };
            }

            var text = CurrentEditor.Text;
            Update(text);
        }

        private void Update(string text)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                var newFoldings = CreateNewFoldings(text);
                new Task(() =>
                {
                    ViewModel.Functions.Clear();
                    foreach (var folding in newFoldings)
                    {
                        ViewModel.Functions.Add(folding);
                    }
                    CurrentManager.UpdateFoldings(newFoldings, -1);
                }).Start(_uiThreadScheduler);
            });
        }

        private static List<NewFolding> CreateNewFoldings(String text)
        {
            List<NewFolding> newFoldings;
            using (var reader = new StringReader(text))
            {
                var antlrInputStream = new AntlrInputStream(reader);
                var lexer = new LuaLexer(antlrInputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new LuaParser(tokens) { BuildParseTree = true};
                var tree = parser.block();
                var visitor = new LuaVisitor();
                newFoldings = visitor.Visit(tree);
            }

            return newFoldings ?? (newFoldings = new List<NewFolding>());
        }

        private class LuaVisitor : LuaBaseVisitor<List<NewFolding>>
        {
            public override List<NewFolding> VisitFunctiondefinition(LuaParser.FunctiondefinitionContext context)
            {
                var funcName = context.funcname().GetText();
                var name = String.Format("{0} Line:{1}", funcName, context.Start.Line);
                var newFolding = new NewFolding() { StartOffset = context.Start.StartIndex, EndOffset = context.Stop.StopIndex + 1, Name = name};
                var foldings = new List<NewFolding> {newFolding};
                foldings.AddRange(base.VisitFunctiondefinition(context));
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
            if (folding != null) CurrentEditor.CaretOffset = folding.StartOffset;
            CurrentEditor.TextArea.Caret.BringCaretToView();
            CurrentEditor.TextArea.Caret.Show();
            Keyboard.Focus(CurrentEditor);
        }
    }
}