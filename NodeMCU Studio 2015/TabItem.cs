using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Resources;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using NodeMCU_Studio_2015.Annotations;
using NodeMCU_Studio_2015.Properties;

namespace NodeMCU_Studio_2015
{
    public sealed class TabItem : INotifyPropertyChanged
    {
        private String _title;
        private TextEditor _editor;
        private FoldingManager _manager;

        public TabItem()
        {
            _title = "";
        }

        private static IHighlightingDefinition _highlightingDefinition;

        private static IHighlightingDefinition GetHighlightingDefinition()
        {
            return LazyInitializer.EnsureInitialized(ref _highlightingDefinition, () =>
            {
                var streamResourceInfo = Application.GetResourceStream(new Uri("Resources/Lua.xshd", UriKind.Relative));
                if (streamResourceInfo != null)
                {
                    using (var reader = new XmlTextReader(streamResourceInfo.Stream))
                    {
                        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
                return null;
            });
        }

        // faked... should be never changed...
        public static IHighlightingDefinition HighlightingDefinition
        {
            get { return GetHighlightingDefinition(); }
            set { throw new NotImplementedException();} 
        }

        public FoldingManager Manager
        {
            get { return _manager; }
            set
            {
                if (value != _manager)
                {
                    _manager = value;
                    OnPropertyChanged("Manager");
                }
            }
        }

        public TextEditor Editor
        {
            get { return _editor; }
            set
            {
                if (value != _editor)
                {
                    _editor = value;
                    OnPropertyChanged("Editor");
                }
            }
        }

        public String Title
        {
            get { return _title; }
            set
            {
                if (value != _title)
                {
                    _title = value;
                    OnPropertyChanged("Title");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}