using MySql.Data.MySqlClient;
using System;

namespace GymVisual;

public static class Database
{
    // Datos actualizados para tu base de datos GFLTR
    private static string host = "localhost";
    private static string database = "GFLTR"; 
    private static string user = "root";
    private static string password = "Mysql"; 

    private static string connStr = $"server={host};database={database};user={user};password={password};";

    public static MySqlConnection GetConnection()
    {
        return new MySqlConnection(connStr);
    }
}