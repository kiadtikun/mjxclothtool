using System.Windows;

namespace mjxClothTool.Views;

public partial class AccountsWindow : Window
{
    public AccountsWindow()
    {
        InitializeComponent();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
