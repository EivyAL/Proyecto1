using Avalonia.Controls;
using Avalonia.Interactivity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GymVisual;

public partial class RegistroPaquete : Window
{
    private bool _isEdit;
    private string _originalClave = "";

    public RegistroPaquete() : this(null) { }

    public RegistroPaquete(Paquete? paquete)
    {
        InitializeComponent();

        DtVigenciaInicio.SelectedDate = DateTime.Now.Date;
        DtVigenciaFin.SelectedDate = DateTime.Now.Date.AddYears(1);

        if (paquete != null)
        {
            _isEdit = true;
            _originalClave = paquete.Clave;

            TxtClave.Text = paquete.Clave;
            TxtNombre.Text = paquete.Nombre;
            TxtTipo.Text = paquete.Tipo;
            TxtImporte.Text = paquete.ImporteTotal.ToString("0.00");
            TxtImportePorDia.Text = paquete.ImportePorDia.ToString("0.00");
            TxtDias.Text = paquete.NumeroDias.ToString();
            TxtMeses.Text = paquete.NumeroMeses.ToString();
            ChkAplicaDiasMes.IsChecked = paquete.AplicaDiasMes;
            DtVigenciaInicio.SelectedDate = paquete.VigenciaInicio ?? DateTime.Now.Date;
            DtVigenciaFin.SelectedDate = paquete.VigenciaFin ?? DateTime.Now.Date.AddYears(1);

            TxtClave.IsEnabled = false;
        }
    }

    private void OnGuardarClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using var conn = Database.GetConnection();
            conn.Open();

            var columns = GetPaquetesColumns(conn);

            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["clave"] = TxtClave.Text?.Trim() ?? "",
                ["nombre"] = TxtNombre.Text?.Trim() ?? "",
                ["tipo_paquete"] = TxtTipo.Text?.Trim() ?? "",
                ["importe_total"] = decimal.TryParse(TxtImporte.Text, out var imp) ? imp : 0m,
                ["importe_por_dia"] = decimal.TryParse(TxtImportePorDia.Text, out var impDia) ? impDia : 0m,
                ["numero_dias"] = int.TryParse(TxtDias.Text, out var dias) ? dias : 0,
                ["numero_meses"] = int.TryParse(TxtMeses.Text, out var meses) ? meses : 0,
                ["aplica_dias_mes"] = ChkAplicaDiasMes.IsChecked == true ? 1 : 0,
                ["vigencia_inicio"] = DtVigenciaInicio.SelectedDate ?? (object)DBNull.Value,
                ["vigencia_fin"] = DtVigenciaFin.SelectedDate ?? (object)DBNull.Value
            };

            if (_isEdit)
            {
                var setClauses = new List<string>();
                foreach (var kv in values)
                {
                    if (kv.Key.Equals("clave", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!columns.Contains(kv.Key))
                        continue;

                    setClauses.Add($"{kv.Key} = @{kv.Key}");
                }

                if (setClauses.Count == 0)
                {
                    StatusMsg.Foreground = Avalonia.Media.Brushes.Red;
                    StatusMsg.Text = "No hay columnas disponibles para actualizar.";
                    return;
                }

                string updateSql = $"UPDATE paquetes SET {string.Join(", ", setClauses)} WHERE clave = @clave";
                using var cmd = new MySqlCommand(updateSql, conn);
                foreach (var kv in values)
                {
                    if (kv.Key.Equals("clave", StringComparison.OrdinalIgnoreCase) || !columns.Contains(kv.Key))
                        continue;

                    cmd.Parameters.AddWithValue($"@{kv.Key}", kv.Value);
                }
                cmd.Parameters.AddWithValue("@clave", _originalClave);
                cmd.ExecuteNonQuery();

                StatusMsg.Foreground = Avalonia.Media.Brushes.Green;
                StatusMsg.Text = "✅ Paquete actualizado.";
            }
            else
            {
                var insertCols = new List<string>();
                var insertParams = new List<string>();
                foreach (var kv in values)
                {
                    if (!columns.Contains(kv.Key))
                        continue;

                    insertCols.Add(kv.Key);
                    insertParams.Add($"@{kv.Key}");
                }

                if (insertCols.Count == 0)
                {
                    StatusMsg.Foreground = Avalonia.Media.Brushes.Red;
                    StatusMsg.Text = "No hay columnas disponibles para insertar.";
                    return;
                }

                string insertSql = $"INSERT INTO paquetes ({string.Join(", ", insertCols)}) VALUES ({string.Join(", ", insertParams)})";
                using var cmd = new MySqlCommand(insertSql, conn);
                foreach (var kv in values)
                {
                    if (!columns.Contains(kv.Key))
                        continue;

                    cmd.Parameters.AddWithValue($"@{kv.Key}", kv.Value);
                }

                cmd.ExecuteNonQuery();
                StatusMsg.Foreground = Avalonia.Media.Brushes.Green;
                StatusMsg.Text = "✅ Paquete creado.";
                LimpiarCampos();
            }
        }
        catch (Exception ex)
        {
            StatusMsg.Foreground = Avalonia.Media.Brushes.Red;
            StatusMsg.Text = "Error: " + ex.Message;
        }
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

    private void LimpiarCampos()
    {
        TxtClave.Text = TxtNombre.Text = TxtTipo.Text = TxtImporte.Text = TxtImportePorDia.Text = TxtDias.Text = TxtMeses.Text = "";
        ChkAplicaDiasMes.IsChecked = false;
        DtVigenciaInicio.SelectedDate = DateTime.Now.Date;
        DtVigenciaFin.SelectedDate = DateTime.Now.Date.AddYears(1);
        TxtClave.IsEnabled = true;
        _isEdit = false;
    }
}

