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
                
                // Consulta ajustada a tu base de datos GFLTR
                string sql = "SELECT COUNT(*) FROM usuarios WHERE usuario=@u AND password=@p AND activo=TRUE";
                var cmd = new MySqlCommand(sql, conn);
                
                cmd.Parameters.AddWithValue("@u", UserBox.Text);
                cmd.Parameters.AddWithValue("@p", PassBox.Text);

                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) 
                {
                    StatusText.Text = "✅ Acceso Concedido";

                    // Lógica para abrir la ventana de Registro de Socios
                    var registro = new RegistroSocio();
                    registro.Show();

                    // Cerramos la ventana de Login
                    this.Close();
                } 
                else 
                {
                    StatusText.Text = "❌ Usuario o contraseña incorrectos";
                }
            } // Aquí se cierra el using
        } // Aquí se cierra el try
        catch (Exception ex) 
        {
            StatusText.Text = "Error de conexión: " + ex.Message;
        }
    }
}