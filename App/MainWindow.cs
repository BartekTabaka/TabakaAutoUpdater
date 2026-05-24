using App.Updater;

namespace App
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnAktualizacja_Click(object sender, EventArgs e)
        {
            using var form = new UpdaterForm();
            form.ShowDialog(this);
        }
    }
}
