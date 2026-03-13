using Avalonia.Controls;
using Avalonia.Interactivity;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System;

namespace GymVisual;

public partial class ListaSocios : UserControl
{
    public ListaSocios()
    {
        InitializeComponent();
        CargarSocios();
    }

    private static HashSet<string> GetSociosColumns(MySqlConnection conn)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new MySqlCommand("SHOW COLUMNS FROM socios", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            cols.Add(reader.GetString("Field"));
        }
        return cols;
    }

    private void CargarSocios(string filtro = "")
    {
        var lista = new List<Socio>();
        try
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cols = GetSociosColumns(conn);

            var selectCols = new List<string>();
            if (cols.Contains("id_socio")) selectCols.Add("id_socio");
            if (cols.Contains("clave")) selectCols.Add("clave");
            if (cols.Contains("nombre")) selectCols.Add("nombre");
            if (cols.Contains("apellido_paterno")) selectCols.Add("apellido_paterno");
            if (cols.Contains("apellido_materno")) selectCols.Add("apellido_materno");
            if (cols.Contains("sexo")) selectCols.Add("sexo");
            if (cols.Contains("fecha_nacimiento")) selectCols.Add("fecha_nacimiento");
            if (cols.Contains("ocupacion")) selectCols.Add("ocupacion");
            if (cols.Contains("empresa")) selectCols.Add("empresa");
            if (cols.Contains("email")) selectCols.Add("email");
            if (cols.Contains("fecha_ingreso")) selectCols.Add("fecha_ingreso");
            if (cols.Contains("activo")) selectCols.Add("activo");
            if (cols.Contains("observaciones")) selectCols.Add("observaciones");
            if (cols.Contains("id_direccion")) selectCols.Add("id_direccion");
            if (cols.Contains("telefono")) selectCols.Add("telefono");

            if (selectCols.Count == 0)
                throw new InvalidOperationException("No se encontraron columnas válidas en la tabla 'socios'.");

            string sql = $"SELECT {string.Join(", ", selectCols)} FROM socios";
            if (!string.IsNullOrEmpty(filtro))
            {
                var where = new List<string>();
                if (cols.Contains("clave")) where.Add("clave LIKE @filtro");
                if (cols.Contains("nombre")) where.Add("nombre LIKE @filtro");
                if (cols.Contains("apellido_paterno")) where.Add("apellido_paterno LIKE @filtro");
                if (cols.Contains("apellido_materno")) where.Add("apellido_materno LIKE @filtro");
                if (cols.Contains("empresa")) where.Add("empresa LIKE @filtro");
                if (where.Count > 0)
                    sql += " WHERE " + string.Join(" OR ", where);
            }

            using var cmd = new MySqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(filtro))
                cmd.Parameters.AddWithValue("@filtro", "%" + filtro + "%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var socio = new Socio
                {
                    IdSocio = GetIntOrDefault(reader, "id_socio"),
                    Clave = GetString(reader, "clave"),
                    Nombre = GetString(reader, "nombre"),
                    ApellidoP = GetString(reader, "apellido_paterno"),
                    ApellidoM = GetString(reader, "apellido_materno"),
                    Sexo = GetString(reader, "sexo"),
                    FechaNacimiento = GetDateTimeOrNull(reader, "fecha_nacimiento"),
                    Ocupacion = GetString(reader, "ocupacion"),
                    Empresa = GetString(reader, "empresa"),
                    Email = GetString(reader, "email"),
                    FechaIngreso = GetDateTimeOrNull(reader, "fecha_ingreso"),
                    Activo = cols.Contains("activo") && GetBool(reader, "activo"),
                    Observaciones = GetString(reader, "observaciones"),
                    IdDireccion = GetIntOrNull(reader, "id_direccion"),
                    Telefono = GetString(reader, "telefono"),
                    Estatus = cols.Contains("activo") && GetBool(reader, "activo") ? "Activo" : "Inactivo"
                };
                lista.Add(socio);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al cargar: " + ex.Message);
        }

        GridSocios.ItemsSource = lista;
    }

    private static string GetString(MySqlDataReader reader, string columnName)
    {
        var idx = GetOrdinalSafe(reader, columnName);
        return idx >= 0 && !reader.IsDBNull(idx) ? reader.GetString(idx) : "";
    }

    private static bool GetBool(MySqlDataReader reader, string columnName)
    {
        var idx = GetOrdinalSafe(reader, columnName);
        return idx >= 0 && !reader.IsDBNull(idx) && reader.GetBoolean(idx);
    }

    private static int GetIntOrDefault(MySqlDataReader reader, string columnName)
    {
        var idx = GetOrdinalSafe(reader, columnName);
        return idx >= 0 && !reader.IsDBNull(idx) ? reader.GetInt32(idx) : 0;
    }

    private static int? GetIntOrNull(MySqlDataReader reader, string columnName)
    {
        var idx = GetOrdinalSafe(reader, columnName);
        return idx >= 0 && !reader.IsDBNull(idx) ? reader.GetInt32(idx) : null;
    }

    private static DateTime? GetDateTimeOrNull(MySqlDataReader reader, string columnName)
    {
        var idx = GetOrdinalSafe(reader, columnName);
        return idx >= 0 && !reader.IsDBNull(idx) ? reader.GetDateTime(idx) : null;
    }

    private static int GetOrdinalSafe(MySqlDataReader reader, string columnName)
    {
        try { return reader.GetOrdinal(columnName); }
        catch { return -1; }
    }

    public void OnNuevoSocioClick(object sender, RoutedEventArgs e)
    {
        var registro = new RegistroSocio();
        registro.Show();
        registro.Closed += (s, e) => CargarSocios(); // Recargar lista después de cerrar
    }

    public void OnEliminarClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var socio = (Socio)button.DataContext!;

        try 
        {
            using var conn = Database.GetConnection();
            conn.Open();
            string sql = "DELETE FROM socios WHERE clave = @clave";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@clave", socio.Clave);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al eliminar: " + ex.Message);
        }

        CargarSocios();
    }

    public void OnEditarClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var socio = (Socio)button.DataContext!;

        // Cargar todos los datos del socio desde la base de datos (incluye ocupacion/telefono, etc.)
        var socioCompleto = CargarSocioCompleto(socio.Clave);
        if (socioCompleto == null)
        {
            Console.WriteLine($"No se encontró socio con clave {socio.Clave}");
            return;
        }

        var registro = new RegistroSocio(socioCompleto);
        registro.Show();
        registro.Closed += (s, e) => CargarSocios();
    }

    private Socio? CargarSocioCompleto(string clave)
    {
        try
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cols = GetSociosColumns(conn);

            var selectCols = new List<string>();
            if (cols.Contains("id_socio")) selectCols.Add("id_socio");
            if (cols.Contains("clave")) selectCols.Add("clave");
            if (cols.Contains("nombre")) selectCols.Add("nombre");
            if (cols.Contains("apellido_paterno")) selectCols.Add("apellido_paterno");
            if (cols.Contains("apellido_materno")) selectCols.Add("apellido_materno");
            if (cols.Contains("sexo")) selectCols.Add("sexo");
            if (cols.Contains("fecha_nacimiento")) selectCols.Add("fecha_nacimiento");
            if (cols.Contains("ocupacion")) selectCols.Add("ocupacion");
            if (cols.Contains("empresa")) selectCols.Add("empresa");
            if (cols.Contains("email")) selectCols.Add("email");
            if (cols.Contains("telefono")) selectCols.Add("telefono");
            if (cols.Contains("fecha_ingreso")) selectCols.Add("fecha_ingreso");
            if (cols.Contains("activo")) selectCols.Add("activo");
            if (cols.Contains("observaciones")) selectCols.Add("observaciones");
            if (cols.Contains("id_direccion")) selectCols.Add("id_direccion");

            if (selectCols.Count == 0)
                return null;

            string sql = $"SELECT {string.Join(", ", selectCols)} FROM socios WHERE clave = @clave";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@clave", clave);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new Socio
            {
                IdSocio = GetIntOrDefault(reader, "id_socio"),
                Clave = GetString(reader, "clave"),
                Nombre = GetString(reader, "nombre"),
                ApellidoP = GetString(reader, "apellido_paterno"),
                ApellidoM = GetString(reader, "apellido_materno"),
                Sexo = GetString(reader, "sexo"),
                FechaNacimiento = GetDateTimeOrNull(reader, "fecha_nacimiento"),
                Ocupacion = GetString(reader, "ocupacion"),
                Empresa = GetString(reader, "empresa"),
                Email = GetString(reader, "email"),
                Telefono = GetString(reader, "telefono"),
                FechaIngreso = GetDateTimeOrNull(reader, "fecha_ingreso"),
                Activo = cols.Contains("activo") && GetBool(reader, "activo"),
                Observaciones = GetString(reader, "observaciones"),
                IdDireccion = GetIntOrNull(reader, "id_direccion"),
                Estatus = cols.Contains("activo") && GetBool(reader, "activo") ? "Activo" : "Inactivo"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al cargar socio completo: " + ex.Message);
            return null;
        }
    }

    private void OnBuscarClick(object sender, RoutedEventArgs e)
    {
        string filtro = TxtBuscar.Text?.Trim() ?? "";
        CargarSocios(filtro);
    }
}