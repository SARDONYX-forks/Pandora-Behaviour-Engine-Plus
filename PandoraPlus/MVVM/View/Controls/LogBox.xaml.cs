using System.Windows.Controls;

namespace Pandora.MVVM.View.Controls;

/// <summary>
/// Interaction logic for LogBox.xaml
/// </summary>
public partial class LogBox : UserControl
{
    public LogBox()
    {
        this.InitializeComponent();
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        this.LogTextBox.ScrollToEnd();
    }
}
