using System.Windows;

namespace AudioSpectrumPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"You clicked and entered {inputText1.Text}");
        }
    }
}
