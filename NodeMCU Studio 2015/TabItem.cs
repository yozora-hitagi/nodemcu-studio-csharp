using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using NodeMCU_Studio_2015.Annotations;

namespace NodeMCU_Studio_2015
{
    public sealed class TabItem : INotifyPropertyChanged
    {
        private String _filePath;
        private String _text;
        private Int32 _index;

        public TabItem()
        {
            _text = "";
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

        public Int32 Index
        {
            get { return _index; }
            set
            {
                if (value != _index)
                {
                    _index = value;
                    OnPropertyChanged("Index");
                }
            }
        }

        public String Text
        {
            get { return _text; }
            set
            {
                if (value != _text)
                {
                    _text = value;
                    OnPropertyChanged("Text");
                }
            }
        }


        public String FilePath
        {
            get { return _filePath; }
            set
            {
                if (value != _filePath)
                {
                    _filePath = value;
                    FileName = Path.GetFileName(_filePath);
                    OnPropertyChanged("FilePath");
                    OnPropertyChanged("FileName");
                }
            }
        }

        public String FileName { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}