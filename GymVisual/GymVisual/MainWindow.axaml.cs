using Avalonia.Controls;
using Avalonia.Interactivity;
using MySql.Data.MySqlClient;
using System;

namespace GymVisual; // Asegúrate que sea igual al x:Class del AXAML

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent(); // Esto es generado automáticamente por Avalonia
    }

    public void OnLoginClick(object sender, RoutedEventArgs e)
    {
        string connStr = "server=localhost;database=ProyectoPrueba;user=root;password=Mysql";
        try 
        {
            using (var conn = new MySqlConnection(connStr)) 
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT COUNT(*) FROM Usuarios WHERE nombre=@u AND id=@p", conn);
                
                // Estos nombres deben existir en el archivo AXAML (Paso 1)
                cmd.Parameters.AddWithValue("@u", UserBox.Text);
                cmd.Parameters.AddWithValue("@p", PassBox.Text);

                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) 
                {
                    StatusText.Text = "✅ Acceso Concedido";
                } 
                else 
                {
                    StatusText.Text = "❌ Datos incorrectos";
                }
            }
        } 
        catch (Exception ex) 
        {
            StatusText.Text = "Error: " + ex.Message;
        }
    }
}