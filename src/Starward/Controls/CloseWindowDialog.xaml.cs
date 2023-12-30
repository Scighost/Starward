using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

public sealed partial class CloseWindowDialog : UserControl
{


    public CloseWindowOption CloseWindowOption { get; private set; } = CloseWindowOption.Hide;


    public CloseWindowDialog()
    {
        this.InitializeComponent();
    }


    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            if (element.Tag is "Hide")
            {
                CloseWindowOption = CloseWindowOption.Hide;
            }
            else if (element.Tag is "Exit")
            {
                CloseWindowOption = CloseWindowOption.Exit;
            }
            else if (element.Tag is "Close")
            {
                CloseWindowOption = CloseWindowOption.Close;
            }
        }
    }


}
