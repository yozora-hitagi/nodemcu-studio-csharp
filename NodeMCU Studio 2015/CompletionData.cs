using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace NodeMCU_Studio_2015
{
    class CompletionData : ICompletionData
    {
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            //var dotOffest = Text.IndexOf(".", StringComparison.Ordinal) + 1;
            //var segment = new TextSegment(){StartOffset = completionSegment.Offset - dotOffest, EndOffset = completionSegment.EndOffset, Length = completionSegment.Length + dotOffest};
            textArea.Document.Replace(completionSegment, this.Text);
        }

        public CompletionData(String text)
        {
            Text = text;
        }

        private static BitmapImage _methodImage;

        private static BitmapImage GetMethodImage()
        {
            return LazyInitializer.EnsureInitialized(ref _methodImage, () =>
            {
                var image = new BitmapImage();
                var rsi = Application.GetResourceStream(new Uri("Resources/method.gif", UriKind.Relative));
                if (rsi != null && rsi.Stream != null)
                {
                    image.StreamSource = rsi.Stream;
                }
                return image;
            });
        }

        public ImageSource Image
        {
            get { return GetMethodImage(); }
        }

        public string Text { get; private set; }

        public object Content
        {
            get { return Text; }
        }

        public object Description
        {
            get { return Text; }
        }

        public double Priority
        {
            get { return 0.0; }
        }
    }
}
