using Avalonia.Controls;
using Avalonia.Interactivity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GymVisual;

public partial class RegistroSocio : Window
{
    private bool _isEdit;
    private string _originalClave = "";
    private byte[]? _fotoBytes;

    public RegistroSocio() : this(null) { }

    public RegistroSocio(Socio? socio)
    {
        InitializeComponent();
        TestConnection();

        // Valores por defecto
        DtIngreso.SelectedDate = DateTime.Now.Date;
        ChkActivo.IsChecked = true;

        if (socio != null)
        {
            _isEdit = true;
            _originalClave = socio.Clave;

            TxtClave.Text = socio.Clave;
            TxtNombre.Text = socio.Nombre;
            TxtApp.Text = socio.ApellidoP;
            TxtApm.Text = socio.ApellidoM;
            if (!string.IsNullOrEmpty(socio.Sexo))
            {
                foreach (var item in CmbSexo.Items)
                {
                    if (item is ComboBoxItem cbItem && cbItem.Content?.ToString() == socio.Sexo)
                    {
                        CmbSexo.SelectedItem = cbItem;
                        break;
                    }
                }
            }
            DtNacimiento.SelectedDate = socio.FechaNacimiento;
            TxtEmail.Text = socio.Email;
            TxtOcupacion.Text = socio.Ocupacion;
            TxtEmpresa.Text = socio.Empresa;
            TxtTelefono.Text = socio.Telefono;
            DtIngreso.SelectedDate = socio.FechaIngreso;
            ChkActivo.IsChecked = socio.Activo;
            TxtObservaciones.Text = socio.Observaciones;
            TxtIdDireccion.Text = socio.IdDireccion?.ToString() ?? "";
            _fotoBytes = socio.Foto;
            if (_fotoBytes != null && _fotoBytes.Length > 0)
                LoadFoto(_fotoBytes);

            TxtClave.IsEnabled = false;
        }
    }

    public void OnGuardarClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using var conn = Database.GetConnection();
            conn.Open();

            var columns = GetSociosColumns(conn);
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["clave"] = TxtClave!.Text?.Trim() ?? "",
                ["nombre"] = TxtNombre!.Text?.Trim() ?? "",
                ["apellido_paterno"] = TxtApp!.Text?.Trim() ?? "",
                ["apellido_materno"] = TxtApm!.Text?.Trim() ?? "",
                ["sexo"] = (CmbSexo!.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                ["fecha_nacimiento"] = DtNacimiento!.SelectedDate ?? (object)DBNull.Value,
                ["email"] = TxtEmail!.Text?.Trim() ?? "",
                ["ocupacion"] = TxtOcupacion!.Text?.Trim() ?? "",
                ["empresa"] = TxtEmpresa!.Text?.Trim() ?? "",
                ["telefono"] = TxtTelefono!.Text?.Trim() ?? "",
                ["fecha_ingreso"] = DtIngreso!.SelectedDate ?? (object)DBNull.Value,
                ["activo"] = ChkActivo!.IsChecked == true ? 1 : 0,
                ["observaciones"] = TxtObservaciones!.Text?.Trim() ?? "",
                ["id_direccion"] = int.TryParse(TxtIdDireccion!.Text, out var idDir) ? idDir : (object)DBNull.Value,
                ["foto"] = _fotoBytes ?? (object)DBNull.Value
            };

            string? fkWarning = null;
            if (values.TryGetValue("id_direccion", out var idDirVal) && idDirVal is int idDirInt)
            {
                if (!ValidateForeignKey(conn, "socios", "id_direccion", idDirInt, out fkWarning))
                {
                    values["id_direccion"] = DBNull.Value;
                }
            }

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

                string updateSql = $"UPDATE socios SET {string.Join(", ", setClauses)} WHERE clave = @clave";
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
                StatusMsg.Text = "✅ Socio actualizado con éxito" + (fkWarning != null ? $" ({fkWarning})" : "");
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

                string insertSql = $"INSERT INTO socios ({string.Join(", ", insertCols)}) VALUES ({string.Join(", ", insertParams)})";
                using var cmd = new MySqlCommand(insertSql, conn);
                foreach (var kv in values)
                {
                    if (!columns.Contains(kv.Key))
                        continue;

                    cmd.Parameters.AddWithValue($"@{kv.Key}", kv.Value);
                }

                cmd.ExecuteNonQuery();

                StatusMsg.Foreground = Avalonia.Media.Brushes.Green;
                StatusMsg.Text = "✅ Socio registrado con éxito" + (fkWarning != null ? $" ({fkWarning})" : "");
                LimpiarCampos();
            }
        }
        catch (Exception ex)
        {
            StatusMsg.Foreground = Avalonia.Media.Brushes.Red;
            StatusMsg.Text = "Error: " + ex.Message;
        }
    }

    private bool ValidateForeignKey(MySqlConnection conn, string table, string column, int value, out string? warningMessage)
    {
        warningMessage = null;
        try
        {
            string schema = conn.Database;
            using var cmd = new MySqlCommand(@"SELECT REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
                                                  FROM information_schema.KEY_COLUMN_USAGE
                                                 WHERE TABLE_SCHEMA = @schema
                                                   AND TABLE_NAME = @table
                                                   AND COLUMN_NAME = @column
                                                   AND REFERENCED_TABLE_NAME IS NOT NULL", conn);
            cmd.Parameters.AddWithValue("@schema", schema);
            cmd.Parameters.AddWithValue("@table", table);
            cmd.Parameters.AddWithValue("@column", column);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return true; // no FK constraint found

            var refTable = reader.GetString("REFERENCED_TABLE_NAME");
            var refColumn = reader.GetString("REFERENCED_COLUMN_NAME");
            reader.Close();

            using var check = new MySqlCommand($"SELECT 1 FROM `{refTable}` WHERE `{refColumn}` = @val LIMIT 1", conn);
            check.Parameters.AddWithValue("@val", value);
            using var checkReader = check.ExecuteReader();
            if (checkReader.Read())
                return true;

            warningMessage = $"ID de dirección {value} no existe en {refTable}";
            return false;
        }
        catch
        {
            return true; // si algo falla, no bloqueamos la operación
        }
    }


    private static HashSet<string> GetSociosColumns(MySqlConnection conn)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new MySqlCommand("SHOW COLUMNS FROM socios", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            cols.Add(reader.GetString("Field"));
        return cols;
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
        TxtEmpresa.Text = TxtObservaciones.Text = TxtIdDireccion.Text = "";
        DtIngreso.SelectedDate = DateTime.Now.Date;
        ChkActivo.IsChecked = true;
        TxtClave.IsEnabled = true;
        _fotoBytes = null;
        ImgFoto.Source = null;
        _isEdit = false;
    }

    private void LoadFoto(byte[] bytes)
    {
        using var stream = new System.IO.MemoryStream(bytes);
        ImgFoto.Source = new Avalonia.Media.Imaging.Bitmap(stream);
    }

    public async void OnSeleccionarFotoClick(object sender, RoutedEventArgs e)
    {
#pragma warning disable CS0618 // OpenFileDialog is obsolete in newer Avalonia versions; using for compatibility.
        var dialog = new OpenFileDialog();
        dialog.Filters.Add(new FileDialogFilter { Name = "Imágenes", Extensions = { "png", "jpg", "jpeg" } });
        dialog.AllowMultiple = false;

        var result = await dialog.ShowAsync(this);
#pragma warning restore CS0618
        if (result != null && result.Length > 0)
        {
            var path = result[0];
            _fotoBytes = await System.IO.File.ReadAllBytesAsync(path);
            LoadFoto(_fotoBytes);
        }
    }
}
 