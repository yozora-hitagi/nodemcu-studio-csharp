using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit;

namespace NodeMCU_Studio_2015
{
    public sealed class CustomTextEditor : TextEditor
    {
        public CustomTextEditor()
        {
            MainWindow.ViewModel.TabItems[MainWindow.ViewModel.CurrentTabItemIndex].Editor = this;
            if (OnInit != null) OnInit(this, null);
        }

        public event EventHandler OnInit;
    }
}
