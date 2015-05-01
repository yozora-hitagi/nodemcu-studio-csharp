using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using NodeMCU_Studio_2015.Annotations;

namespace NodeMCU_Studio_2015
{
    public sealed class ViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<TabItem> _tabItems;
        private SmartObservableCollection<NewFolding> _functions; 
        private Int32 _currentTabItemIndex;
        private TextEditor _editor;
        private FoldingManager _foldingManager;

        public ViewModel()
        {
            _tabItems = new ObservableCollection<TabItem>();
            _functions = new SmartObservableCollection<NewFolding>();
        }

        public Int32 CurrentTabItemIndex
        {
            get { return _currentTabItemIndex; }
            set
            {
                if (value != _currentTabItemIndex)
                {
                    _currentTabItemIndex = value;
                    OnPropertyChanged("CurrentTabItemIndex");
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

        public FoldingManager FoldingManager
        {
            get { return _foldingManager; }
            set
            {
                if (value != _foldingManager)
                {
                    _foldingManager = value;
                    OnPropertyChanged("FoldingManager");
                }
            }
        }

        public SmartObservableCollection<NewFolding> Functions
        {
            get { return _functions; }
            set
            {
                if (value != _functions)
                {
                    _functions = value;
                    OnPropertyChanged("Functions");
                }
            }
        } 

        public ObservableCollection<TabItem> TabItems
        {
            get { return _tabItems; }
            set
            {
                if (value != _tabItems)
                {
                    _tabItems = value;
                    OnPropertyChanged("TabItems");
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