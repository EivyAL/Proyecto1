using MySql.Data.MySqlClient;

// Datos de tu conexión local
string connectionString = "server=localhost;database=ProyectoPrueba;user=root;password=TU_CONTRASEÑA";

try
{
    using (MySqlConnection conn = new MySqlConnection(connectionString))
    {
        conn.Open();
        Console.WriteLine("¡Conexión exitosa a MySQL!");

        string sql = "SELECT nombre FROM Usuarios";
        MySqlCommand cmd = new MySqlCommand(sql, conn);

        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                Console.WriteLine("Usuario encontrado: " + reader["nombre"]);
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
