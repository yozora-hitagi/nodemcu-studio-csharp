using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace NodeMCU_Studio_2015
{
    public sealed class XshdToSyntaxHighlightingConverter : IValueConverter
    {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var path = value as String;
                if (path == null) return null;

                var streamResourceInfo = Application.GetResourceStream(new Uri(path, UriKind.Relative));
                if (streamResourceInfo != null)
                {
                    using (var reader = new XmlTextReader(streamResourceInfo.Stream))
                    {
                        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
                return null;
            }


            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
    }
}
