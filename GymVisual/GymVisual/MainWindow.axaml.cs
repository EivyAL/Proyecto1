public void OnLoginClick(object sender, RoutedEventArgs e)
{
    try 
    {
        using (var conn = Database.GetConnection()) 
        {
            conn.Open();
            // Ajustado a tu tabla 'usuarios' y campos 'usuario'/'password'
            string sql = "SELECT COUNT(*) FROM usuarios WHERE usuario=@u AND password=@p AND activo=TRUE";
            var cmd = new MySqlCommand(sql, conn);
            
            cmd.Parameters.AddWithValue("@u", UserBox.Text);
            cmd.Parameters.AddWithValue("@p", PassBox.Text);

            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) 
            {
                StatusText.Text = "✅ Acceso Concedido";
                // Aquí podrías abrir la ventana del Menú Principal próximamente
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