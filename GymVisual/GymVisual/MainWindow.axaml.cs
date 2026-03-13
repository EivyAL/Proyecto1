using Avalonia.Controls;
using Avalonia.Interactivity;
using MySql.Data.MySqlClient; 
using System; 

namespace GymVisual;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void OnLoginClick(object sender, RoutedEventArgs e)
    {
        try 
        {
            using (var conn = Database.GetConnection()) 
            {
                conn.Open();
                
                string sql = "SELECT COUNT(*) FROM usuarios WHERE usuario=@u AND password=@p AND activo=TRUE";
                var cmd = new MySqlCommand(sql, conn);
                
                cmd.Parameters.AddWithValue("@u", UserBox.Text);
                cmd.Parameters.AddWithValue("@p", PassBox.Text);

                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) 
                {
                    StatusText.Text = "✅ Acceso Concedido";

                    // 1. Abrimos el Menú Principal (Este será el centro de todo)
                    var menu = new MenuPrincipal();
                    menu.Show();

                    // 2. Cerramos el Login (Ya no lo necesitamos)
                    this.Close();
                } 
                else 
                {
                    StatusText.Text = "❌ Usuario o contraseña incorrectos";
                }
            } 
        } 
        catch (Exception ex) 
        {
            StatusText.Text = "Error de conexión: " + ex.Message;
        }
    }
}