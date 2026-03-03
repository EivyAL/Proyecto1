using MySql.Data.MySqlClient;

string connectionString = "server=localhost;database=ProyectoPrueba;user=root;password=Mysql";

try
{
    using (MySqlConnection conn = new MySqlConnection(connectionString))
    {
        conn.Open();
        Console.WriteLine("✅ ¡Conexión exitosa!");

        // 1. Insertar un nuevo usuario desde C#
        Console.Write("Escribe un nombre para guardar en la base de datos: ");
        string? nuevoNombre = Console.ReadLine();

        string insertSql = "INSERT INTO Usuarios (nombre) VALUES (@nom)";
        MySqlCommand insertCmd = new MySqlCommand(insertSql, conn);
        insertCmd.Parameters.AddWithValue("@nom", nuevoNombre);
        insertCmd.ExecuteNonQuery();
        Console.WriteLine("💾 ¡Nombre guardado con éxito!");

        // 2. Leer todos los nombres actuales
        Console.WriteLine("\n--- Lista actualizada en MySQL ---");
        string selectSql = "SELECT * FROM Usuarios";
        MySqlCommand selectCmd = new MySqlCommand(selectSql, conn);

        using (MySqlDataReader reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                Console.WriteLine($"ID: {reader["id"]} | Nombre: {reader["nombre"]}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("❌ Error: " + ex.Message);
}
