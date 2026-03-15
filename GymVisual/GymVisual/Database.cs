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

    public static void EnsureSchema()
    {
        using var conn = GetConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();

        // Socios (clientes)
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS socios (
            id INT AUTO_INCREMENT PRIMARY KEY,
            clave_socio VARCHAR(100) NOT NULL,
            nombre VARCHAR(200) DEFAULT NULL,
            apellido VARCHAR(200) DEFAULT NULL,
            apellido_m VARCHAR(200) DEFAULT NULL,
            sexo VARCHAR(50) DEFAULT NULL,
            fecha_nacimiento DATE NULL,
            email VARCHAR(200) DEFAULT NULL,
            ocupacion VARCHAR(200) DEFAULT NULL,
            empresa VARCHAR(200) DEFAULT NULL,
            telefono VARCHAR(100) DEFAULT NULL,
            fecha_ingreso DATE NULL,
            activo TINYINT(1) NOT NULL DEFAULT 1,
            observaciones TEXT DEFAULT NULL,
            id_direccion INT NULL,
            foto LONGBLOB DEFAULT NULL,
            estatus VARCHAR(100) DEFAULT NULL
        )";
        cmd.ExecuteNonQuery();

        // Paquetes / membresías
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS paquetes (
            id INT AUTO_INCREMENT PRIMARY KEY,
            clave VARCHAR(100) NOT NULL,
            nombre VARCHAR(255) NOT NULL,
            importe_total DECIMAL(12,2) NOT NULL DEFAULT 0,
            numero_meses INT NOT NULL DEFAULT 0,
            numero_dias INT NOT NULL DEFAULT 0,
            aplica_dias_mes TINYINT(1) NOT NULL DEFAULT 0
        )";
        cmd.ExecuteNonQuery();

        // Productos
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS productos (
            id INT AUTO_INCREMENT PRIMARY KEY,
            clave VARCHAR(64) NOT NULL,
            codigo_barras VARCHAR(128) NOT NULL,
            descripcion VARCHAR(255) NOT NULL,
            costo DECIMAL(10,2) NOT NULL DEFAULT 0,
            precio_venta DECIMAL(10,2) NOT NULL DEFAULT 0,
            iva VARCHAR(32) DEFAULT NULL,
            stock INT NOT NULL DEFAULT 0,
            departamento VARCHAR(128) DEFAULT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )";
        cmd.ExecuteNonQuery();

        // Ventas
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ventas (
            id INT AUTO_INCREMENT PRIMARY KEY,
            fecha DATETIME NOT NULL,
            socio_id INT NULL,
            total DECIMAL(12,2) NOT NULL,
            pagado DECIMAL(12,2) NOT NULL,
            cambio DECIMAL(12,2) NOT NULL,
            forma_pago VARCHAR(32) NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS venta_items (
            id INT AUTO_INCREMENT PRIMARY KEY,
            venta_id INT NOT NULL,
            tipo VARCHAR(20) NOT NULL,
            producto_id INT NULL,
            paquete_id INT NULL,
            clave VARCHAR(64) NOT NULL,
            descripcion VARCHAR(255) NOT NULL,
            cantidad INT NOT NULL,
            precio DECIMAL(12,2) NOT NULL
        )";
        cmd.ExecuteNonQuery();

        // Membresías (opcional)
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS membresias (
            id_membresia INT AUTO_INCREMENT PRIMARY KEY,
            id_socio INT NULL,
            clave_socio VARCHAR(100) NOT NULL,
            clave_paquete VARCHAR(100) NOT NULL,
            fecha_inicio DATE NOT NULL,
            fecha_vencimiento DATE NOT NULL,
            importe_total DECIMAL(10,2) NOT NULL,
            creado_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )";
        cmd.ExecuteNonQuery();
    }
}
