using Avalonia.Controls;
using Avalonia.Interactivity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace GymVisual
{
    public partial class HistorialVentas : UserControl
    {
        private ObservableCollection<VentaResumen> _ventas = new();

        public HistorialVentas()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                StatusHelper.ShowError(this, "Error inicializando Historial de ventas: " + ex.Message);
                return;
            }

            GridVentas.ItemsSource = _ventas;
            DateFiltro.SelectedDate = DateTimeOffset.Now;

            try
            {
                LoadVentas();
            }
            catch (Exception ex)
            {
                StatusHelper.ShowError(this, "Error cargando historial de ventas: " + ex.Message);
            }
        }

        private void LoadVentas()
        {
            _ventas.Clear();

            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();

                var queries = new[]
                {
                    @"SELECT v.id, v.fecha, v.total, s.nombre, s.apellido_paterno AS apellido
                      FROM ventas v
                      LEFT JOIN socios s ON s.id_socio = v.socio_id
                      WHERE DATE(v.fecha) = @fecha
                      ORDER BY v.fecha DESC",
                    @"SELECT v.id, v.fecha, v.total, s.nombre, s.apellido_materno AS apellido
                      FROM ventas v
                      LEFT JOIN socios s ON s.id_socio = v.socio_id
                      WHERE DATE(v.fecha) = @fecha
                      ORDER BY v.fecha DESC",
                    @"SELECT v.id, v.fecha, v.total, s.nombre, s.apellido AS apellido
                      FROM ventas v
                      LEFT JOIN socios s ON s.id = v.socio_id
                      WHERE DATE(v.fecha) = @fecha
                      ORDER BY v.fecha DESC"
                };

                bool loaded = false;
                foreach (var q in queries)
                {
                    try
                    {
                        cmd.CommandText = q;
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@fecha", DateFiltro.SelectedDate?.Date ?? DateTime.Today);

                        using var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            _ventas.Add(new VentaResumen
                            {
                                Id = reader.GetInt32("id"),
                                Fecha = reader.GetDateTime("fecha"),
                                Total = reader.GetDecimal("total"),
                                Cliente = string.IsNullOrWhiteSpace(reader.GetString("nombre")) ? "(Sin socio)" : reader.GetString("nombre") + " " + reader.GetString("apellido")
                            });
                        }

                        GridVentas.ItemsSource = _ventas;
                        loaded = true;
                        break;
                    }
                    catch (MySql.Data.MySqlClient.MySqlException)
                    {
                        // intenta con otra variante de nombre/apellido
                        _ventas.Clear();
                    }
                }

                if (!loaded)
                {
                    throw new Exception("No se pudo cargar historial: esquema de 'socios' desconocido.");
                }
            }
            catch (Exception ex)
            {
                // Proporcionar algo de contexto si falta alguna tabla en la base de datos.
                var msg = ex.Message;
                if (msg.Contains("Table") && msg.Contains("doesn't exist"))
                {
                    msg += "\n\nAsegúrate de tener las tablas: ventas, venta_items y socios.";
                }
                StatusHelper.ShowError(this, "Error cargando historial de ventas: " + msg);
            }
        }

        private void OnBuscarClick(object? sender, RoutedEventArgs e)
        {
            LoadVentas();
        }

        private void OnActualizarClick(object? sender, RoutedEventArgs e)
        {
            LoadVentas();
        }

        private void OnEliminarVentaClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is VentaResumen venta)
            {
                // Anular la venta (no se pide confirmación para simplificar)

                try
                {
                    using var conn = Database.GetConnection();
                    conn.Open();

                    using var tx = conn.BeginTransaction();
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;

                    cmd.CommandText = "SELECT producto_id, paquete_id, cantidad FROM venta_items WHERE venta_id = @ventaId";
                    cmd.Parameters.AddWithValue("@ventaId", venta.Id);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var productoId = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0);
                        var cantidad = reader.GetInt32(2);
                        if (productoId.HasValue)
                        {
                            using var cmd2 = conn.CreateCommand();
                            cmd2.Transaction = tx;
                            cmd2.CommandText = "UPDATE productos SET stock = stock + @cantidad WHERE id = @id";
                            cmd2.Parameters.AddWithValue("@cantidad", cantidad);
                            cmd2.Parameters.AddWithValue("@id", productoId.Value);
                            cmd2.ExecuteNonQuery();
                        }
                    }

                    cmd.Parameters.Clear();
                    cmd.CommandText = "DELETE FROM venta_items WHERE venta_id = @ventaId";
                    cmd.Parameters.AddWithValue("@ventaId", venta.Id);
                    cmd.ExecuteNonQuery();

                    cmd.Parameters.Clear();
                    cmd.CommandText = "DELETE FROM ventas WHERE id = @ventaId";
                    cmd.Parameters.AddWithValue("@ventaId", venta.Id);
                    cmd.ExecuteNonQuery();

                    tx.Commit();

                    LoadVentas();
                }
                catch (Exception ex)
                {
                    var dlg = new Window
                    {
                        Content = new TextBlock { Text = "Error anulando venta: " + ex.Message, Margin = new Avalonia.Thickness(16) },
                        Width = 380,
                        Height = 160,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    var owner = this.VisualRoot as Window;
                    if (owner != null)
                        dlg.ShowDialog(owner);
                    else
                        dlg.Show();
                }
            }
        }
    }

    public class VentaResumen
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Cliente { get; set; } = string.Empty;
    }
}
