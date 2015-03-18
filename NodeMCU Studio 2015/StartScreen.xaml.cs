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
            SynchronizationContext context = SynchronizationContext.Current;

            new Task(() => {
                Thread.Sleep(3000);

                context.Post(_ =>
                {
                    Hide();
                    PowerfulLuaEditor editor = new PowerfulLuaEditor(this);
                    editor.Show();
                }, null);
                
            }).Start();
        }
    }
}
