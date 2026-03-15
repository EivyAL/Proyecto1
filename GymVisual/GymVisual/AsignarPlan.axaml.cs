using Avalonia.Controls;
using Avalonia.Interactivity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GymVisual;

public partial class AsignarPlan : Window
{
    private readonly Socio _socio;

    public AsignarPlan() : this(new Socio()) { }

    public AsignarPlan(Socio socio)
    {
        InitializeComponent();
        _socio = socio;
        TxtSocio.Text = string.IsNullOrWhiteSpace(socio.Clave)
            ? "(Socio no seleccionado)"
            : $"{socio.Clave} - {socio.Nombre} {socio.ApellidoP} {socio.ApellidoM}";
        DtInicio.SelectedDate = DateTime.Now.Date;
        CargarPaquetes();
    }

    private void CargarPaquetes()
    {
        var paquetes = new List<Paquete>();
        try
        {
            using var conn = Database.GetConnection();
            conn.Open();
            string sql = "SELECT clave, nombre, tipo_paquete, importe_total, numero_dias FROM paquetes";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                paquetes.Add(new Paquete
                {
                    Clave = reader.GetString("clave"),
                    Nombre = reader.GetString("nombre"),
                    Tipo = reader.GetString("tipo_paquete"),
                    ImporteTotal = reader.GetDecimal("importe_total"),
                    NumeroDias = reader.GetInt32("numero_dias")
                });
            }
        }
        catch (Exception ex)
        {
            StatusMsg.Text = "Error al cargar paquetes: " + ex.Message;
            StatusMsg.Foreground = Avalonia.Media.Brushes.Red;
        }

        CmbPaquetes.ItemsSource = paquetes;
        if (paquetes.Count > 0)
            CmbPaquetes.SelectedIndex = 0;
    }

    private void OnConfirmarClick(object sender, RoutedEventArgs e)
    {
        StatusMsg.Text = "";

        if (CmbPaquetes.SelectedItem is not Paquete paquete)
        {
            StatusMsg.Text = "Debe seleccionar un paquete.";
            return;
        }

        if (!DtInicio.SelectedDate.HasValue)
        {
            StatusMsg.Text = "Debe seleccionar una fecha de inicio.";
            return;
        }

        var inicio = DtInicio.SelectedDate.Value.Date;
        var vencimiento = inicio.AddDays(paquete.NumeroDias);

        try
        {
            using var conn = Database.GetConnection();
            conn.Open();

            EnsureMembresiasTable(conn);

            string sql = "INSERT INTO membresias (id_socio, clave_socio, clave_paquete, fecha_inicio, fecha_vencimiento, importe_total) " +
                         "VALUES (@id_socio, @clave_socio, @clave_paquete, @fecha_inicio, @fecha_vencimiento, @importe_total)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id_socio", _socio.IdSocio > 0 ? (object)_socio.IdSocio : DBNull.Value);
            cmd.Parameters.AddWithValue("@clave_socio", _socio.Clave);
            cmd.Parameters.AddWithValue("@clave_paquete", paquete.Clave);
            cmd.Parameters.AddWithValue("@fecha_inicio", inicio);
            cmd.Parameters.AddWithValue("@fecha_vencimiento", vencimiento);
            cmd.Parameters.AddWithValue("@importe_total", paquete.ImporteTotal);

            cmd.ExecuteNonQuery();

            StatusMsg.Foreground = Avalonia.Media.Brushes.Green;
            StatusMsg.Text = "✅ Plan asignado correctamente.";
        }
        catch (Exception ex)
        {
            StatusMsg.Foreground = Avalonia.Media.Brushes.Red;
            StatusMsg.Text = "Error al asignar plan: " + ex.Message;
        }
    }

    private void EnsureMembresiasTable(MySqlConnection conn)
    {
        // Crea la tabla si no existe y actualiza el esquema si faltan columnas.
        var createSql = @"CREATE TABLE IF NOT EXISTS membresias (
            id_membresia INT AUTO_INCREMENT PRIMARY KEY,
            id_socio INT NULL,
            clave_socio VARCHAR(100) NOT NULL,
            clave_paquete VARCHAR(100) NOT NULL,
            fecha_inicio DATE NOT NULL,
            fecha_vencimiento DATE NOT NULL,
            importe_total DECIMAL(10,2) NOT NULL,
            creado_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )";

        using (var cmd = new MySqlCommand(createSql, conn))
            cmd.ExecuteNonQuery();

        // En caso de que la tabla exista con esquema anterior, añade columnas faltantes.
        var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = new MySqlCommand("SHOW COLUMNS FROM membresias", conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                existingCols.Add(reader.GetString("Field"));
        }

        var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id_socio"] = "INT NULL",
            ["clave_socio"] = "VARCHAR(100) NOT NULL",
            ["clave_paquete"] = "VARCHAR(100) NOT NULL",
            ["fecha_inicio"] = "DATE NOT NULL",
            ["fecha_vencimiento"] = "DATE NOT NULL",
            ["importe_total"] = "DECIMAL(10,2) NOT NULL"
        };

        foreach (var kv in expected)
        {
            if (!existingCols.Contains(kv.Key))
            {
                using var alter = new MySqlCommand($"ALTER TABLE membresias ADD COLUMN {kv.Key} {kv.Value}", conn);
                alter.ExecuteNonQuery();
            }
        }
    }
}

