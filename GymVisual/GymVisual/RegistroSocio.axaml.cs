using Avalonia.Controls;
using Avalonia.Interactivity;
using MySql.Data.MySqlClient;
using System;

namespace GymVisual;

public partial class RegistroSocio : Window
{
    public RegistroSocio()
    {
        InitializeComponent();
        TestConnection();
    }

    public void OnGuardarClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                // SQL para insertar en tu tabla 'socios' de GFLTR
                string query = @"INSERT INTO socios (clave, nombre, apellido_paterno, apellido_materno, email, ocupacion, telefono, fecha_ingreso, activo) 
                                 VALUES (@clave, @nom, @app, @apm, @email, @ocu, @tel, @fecha, 1)";
                
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@clave", TxtClave.Text);
                cmd.Parameters.AddWithValue("@nom", TxtNombre.Text);
                cmd.Parameters.AddWithValue("@app", TxtApp.Text);
                cmd.Parameters.AddWithValue("@apm", TxtApm.Text);
                cmd.Parameters.AddWithValue("@email", TxtEmail.Text);
                cmd.Parameters.AddWithValue("@ocu", TxtOcupacion.Text);
                cmd.Parameters.AddWithValue("@tel", TxtTelefono.Text);
                cmd.Parameters.AddWithValue("@fecha", DateTime.Now);

                cmd.ExecuteNonQuery();
                
                StatusMsg.Text = "✅ Socio registrado con éxito";
                LimpiarCampos();
            }
        }
        catch (Exception ex)
        {
            StatusMsg.Foreground = Avalonia.Media.Brushes.Red;
            StatusMsg.Text = "Error: " + ex.Message;
        }
    }

    private void TestConnection()
    {
        try
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                StatusMsg.Text = "✅ Conexión a la base de datos exitosa";
                StatusMsg.Foreground = Avalonia.Media.Brushes.Green;
            }
        }
        catch (Exception ex)
        {
            StatusMsg.Text = "❌ Error de conexión: " + ex.Message;
            StatusMsg.Foreground = Avalonia.Media.Brushes.Red;
        }
    }

    private void LimpiarCampos()
    {
        TxtClave.Text = TxtNombre.Text = TxtApp.Text = TxtApm.Text = TxtEmail.Text = TxtOcupacion.Text = TxtTelefono.Text = "";
    }
}