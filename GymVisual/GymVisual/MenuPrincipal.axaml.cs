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
        // Por ahora, como ya tienes la ventana de registro, podemos abrirla:
        var regSocio = new RegistroSocio();
        regSocio.Show();
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