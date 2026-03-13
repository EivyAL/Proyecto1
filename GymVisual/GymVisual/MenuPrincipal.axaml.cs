using Avalonia.Controls;
using Avalonia.Interactivity;


namespace GymVisual;


public partial class MenuPrincipal : Window
{
    public MenuPrincipal()
    {
        InitializeComponent();
    }

    private void OnSociosClick(object sender, RoutedEventArgs e)
    {
        // Esto quita el texto de bienvenida y pone la tabla
    MainContent.Content = new ListaSocios();
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        var login = new MainWindow();
        login.Show();
        this.Close();
    }

    // Los demás botones se irán llenando conforme creemos los módulos
    private void OnInicioClick(object sender, RoutedEventArgs e) { }
    private void OnInventarioClick(object sender, RoutedEventArgs e) { }
    private void OnCajaClick(object sender, RoutedEventArgs e) { }
}