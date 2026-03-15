using Avalonia.Controls;
using Avalonia.Interactivity;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System;

namespace GymVisual;

public partial class ListaPaquetes : UserControl
{
    public ListaPaquetes()
    {
        InitializeComponent();
        CargarPaquetes();
    }

    private static HashSet<string> GetPaquetesColumns(MySqlConnection conn)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new MySqlCommand("SHOW COLUMNS FROM paquetes", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            cols.Add(reader.GetString("Field"));
        return cols;
    }

    private void CargarPaquetes()
    {
        var lista = new List<Paquete>();
        try
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cols = GetPaquetesColumns(conn);

            var selectCols = new List<string>();
            if (cols.Contains("clave")) selectCols.Add("clave");
            if (cols.Contains("nombre")) selectCols.Add("nombre");
            if (cols.Contains("tipo_paquete")) selectCols.Add("tipo_paquete");
            if (cols.Contains("importe_total")) selectCols.Add("importe_total");
            if (cols.Contains("importe_por_dia")) selectCols.Add("importe_por_dia");
            if (cols.Contains("numero_dias")) selectCols.Add("numero_dias");
            if (cols.Contains("numero_meses")) selectCols.Add("numero_meses");
            if (cols.Contains("aplica_dias_mes")) selectCols.Add("aplica_dias_mes");
            if (cols.Contains("vigencia_inicio")) selectCols.Add("vigencia_inicio");
            if (cols.Contains("vigencia_fin")) selectCols.Add("vigencia_fin");

            if (selectCols.Count == 0)
                throw new InvalidOperationException("No se encontraron columnas válidas en la tabla 'paquetes'.");

            string sql = $"SELECT {string.Join(", ", selectCols)} FROM paquetes";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Paquete
                {
                    Clave = GetString(reader, "clave"),
                    Nombre = GetString(reader, "nombre"),
                    Tipo = GetString(reader, "tipo_paquete"),
                    ImporteTotal = GetDecimal(reader, "importe_total"),
                    ImportePorDia = GetDecimal(reader, "importe_por_dia"),
                    NumeroDias = GetIntOrDefault(reader, "numero_dias"),
                    NumeroMeses = GetIntOrDefault(reader, "numero_meses"),
                    AplicaDiasMes = GetBool(reader, "aplica_dias_mes"),
                    VigenciaInicio = GetDateTimeOrNull(reader, "vigencia_inicio"),
                    VigenciaFin = GetDateTimeOrNull(reader, "vigencia_fin")
                });
            }
            GridPaquetes.ItemsSource = lista;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al cargar paquetes: " + ex.Message);
        }
    }

    private static string GetString(MySqlDataReader reader, string columnName)
    {
        var idx = GetOrdinalSafe(reader, columnName);
        return idx >= 0 && !reader.IsDBNull(idx) ? reader.GetString(idx) : "";
    }

    private static decimal GetDecimal(MySqlDataReader reader, string columnName)
    {
        var idx = GetOrdinalSafe(reader, columnName);
        return idx >= 0 && !reader.IsDBNull(idx) ? reader.GetDecimal(idx) : 0m;
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

    public void OnNuevoPaqueteClick(object sender, RoutedEventArgs e)
    {
        var registro = new RegistroPaquete();
        registro.Show();
        registro.Closed += (s, e) => CargarPaquetes();
    }

    public void OnEditarPaqueteClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var paquete = (Paquete)button.DataContext!;

        var paqueteCompleto = CargarPaqueteCompleto(paquete.Clave);
        if (paqueteCompleto == null)
        {
            Console.WriteLine($"No se encontró paquete con clave {paquete.Clave}");
            return;
        }

        var registro = new RegistroPaquete(paqueteCompleto);
        registro.Show();
        registro.Closed += (s, e) => CargarPaquetes();
    }

    private Paquete? CargarPaqueteCompleto(string clave)
    {
        try
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cols = GetPaquetesColumns(conn);

            var selectCols = new List<string>();
            if (cols.Contains("clave")) selectCols.Add("clave");
            if (cols.Contains("nombre")) selectCols.Add("nombre");
            if (cols.Contains("tipo_paquete")) selectCols.Add("tipo_paquete");
            if (cols.Contains("importe_total")) selectCols.Add("importe_total");
            if (cols.Contains("importe_por_dia")) selectCols.Add("importe_por_dia");
            if (cols.Contains("numero_dias")) selectCols.Add("numero_dias");
            if (cols.Contains("numero_meses")) selectCols.Add("numero_meses");
            if (cols.Contains("aplica_dias_mes")) selectCols.Add("aplica_dias_mes");
            if (cols.Contains("vigencia_inicio")) selectCols.Add("vigencia_inicio");
            if (cols.Contains("vigencia_fin")) selectCols.Add("vigencia_fin");

            if (selectCols.Count == 0)
                return null;

            string sql = $"SELECT {string.Join(", ", selectCols)} FROM paquetes WHERE clave = @clave";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@clave", clave);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new Paquete
            {
                Clave = GetString(reader, "clave"),
                Nombre = GetString(reader, "nombre"),
                Tipo = GetString(reader, "tipo_paquete"),
                ImporteTotal = GetDecimal(reader, "importe_total"),
                ImportePorDia = GetDecimal(reader, "importe_por_dia"),
                NumeroDias = GetIntOrDefault(reader, "numero_dias"),
                NumeroMeses = GetIntOrDefault(reader, "numero_meses"),
                AplicaDiasMes = GetBool(reader, "aplica_dias_mes"),
                VigenciaInicio = GetDateTimeOrNull(reader, "vigencia_inicio"),
                VigenciaFin = GetDateTimeOrNull(reader, "vigencia_fin")
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al cargar paquete completo: " + ex.Message);
            return null;
        }
    }
}

