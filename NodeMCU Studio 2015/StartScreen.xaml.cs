using System.Threading;
using System.Threading.Tasks;

namespace NodeMCU_Studio_2015
{
    /// <summary>
    /// Interaction logic for StartScreen.xaml
    /// </summary>
    public partial class StartScreen
    {
        public StartScreen()
        {
            InitializeComponent();
            var context = SynchronizationContext.Current;

            new Task(() => {
              //  Thread.Sleep(1000);

                context.Post(_ =>
                {
                    var editor = new MainWindow();
                    editor.Show();
                    Close();
                }, null);
                
            }).Start();
        }
    }
}
