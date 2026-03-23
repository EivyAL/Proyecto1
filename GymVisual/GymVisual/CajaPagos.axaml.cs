using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace GymVisual
{
    public partial class CajaPagos : UserControl
    {
        private ObservableCollection<CartItem> _carrito = new();
        private ObservableCollection<Paquete> _paquetes = new();
        private ObservableCollection<Product> _productos = new();
        private ObservableCollection<Socio> _socios = new();

        public CajaPagos()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // Si el XAML no se puede parsear, mostramos un error y evitamos el cierre de la app.
                StatusHelper.ShowError(this, "Error inicializando Caja/Pagos: " + ex.Message);
                return;
            }

            try
            {
                LoadData();
                GridCarrito.ItemsSource = _carrito;
                CmbFormaPago.SelectionChanged += OnFormaPagoChanged;
                UpdateTotals();
            }
            catch (Exception ex)
            {
                StatusHelper.ShowError(this, "Error al cargar Caja/Pagos: " + ex.Message);
            }
        }

        private void LoadData()
        {
            EnsureVentasTables();

            LoadSocios();
            LoadPaquetes();
            LoadProductos();
        }

        private void LoadSocios()
        {
            _socios.Clear();
            CmbSocio.ItemsSource = null;

            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                var queryAttempts = new[]
                {
                    "SELECT id_socio AS id, COALESCE(clave_socio, clave) AS clave_socio, nombre, apellido_paterno, apellido_materno FROM socios ORDER BY nombre",
                    "SELECT id AS id, COALESCE(clave_socio, clave) AS clave_socio, nombre, apellido_paterno, apellido_materno FROM socios ORDER BY nombre",
                    "SELECT id_socio AS id, clave, nombre, apellido_paterno, apellido_materno FROM socios ORDER BY nombre",
                    "SELECT id AS id, clave, nombre, apellido_paterno, apellido_materno FROM socios ORDER BY nombre"
                };

                foreach (var query in queryAttempts)
                {
                    try
                    {
                        LoadSociosWithQuery(conn, query);
                        break;
                    }
                    catch (MySql.Data.MySqlClient.MySqlException) { }
                }

                CmbSocio.ItemsSource = _socios;
                if (_socios.Count > 0)
                    CmbSocio.SelectedIndex = 0;
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                var msg = ex.Message;
                if (msg.Contains("Table") && msg.Contains("doesn't exist"))
                {
                    msg += "\n\nLa tabla 'socios' no existe. Crea la tabla con el script adecuado o ejecuta el módulo correspondiente.";
                }
                StatusHelper.ShowError(this, "Error cargando socios: " + msg);
            }
        }

        private void LoadSociosWithQuery(MySqlConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();

            int claveIndex = -1;
            int apellidoPaternoIndex = -1;
            int apellidoMaternoIndex = -1;

            try { claveIndex = reader.GetOrdinal("clave_socio"); } catch { }
            if (claveIndex < 0) try { claveIndex = reader.GetOrdinal("clave"); } catch { }

            try { apellidoPaternoIndex = reader.GetOrdinal("apellido_paterno"); } catch { }
            try { apellidoMaternoIndex = reader.GetOrdinal("apellido_materno"); } catch { }

            while (reader.Read())
            {
                var clave = string.Empty;
                if (claveIndex >= 0 && !reader.IsDBNull(claveIndex))
                    clave = reader.GetString(claveIndex);

                var apellidoP = string.Empty;
                if (apellidoPaternoIndex >= 0 && !reader.IsDBNull(apellidoPaternoIndex))
                    apellidoP = reader.GetString(apellidoPaternoIndex);

                var apellidoM = string.Empty;
                if (apellidoMaternoIndex >= 0 && !reader.IsDBNull(apellidoMaternoIndex))
                    apellidoM = reader.GetString(apellidoMaternoIndex);

                _socios.Add(new Socio
                {
                    IdSocio = reader.GetInt32("id"),
                    Clave = clave,
                    Nombre = reader.GetString("nombre"),
                    ApellidoP = apellidoP,
                    ApellidoM = apellidoM,
                });
            }
        }

        private void LoadPaquetes()
        {
            _paquetes.Clear();
            CmbPaquete.ItemsSource = null;

            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                var queryAttempts = new[]
                {
                    "SELECT id, clave, nombre, importe_total, numero_meses, numero_dias, aplica_dias_mes FROM paquetes ORDER BY nombre",
                    "SELECT id_paquete AS id, clave, nombre, importe_total, numero_meses, numero_dias, aplica_dias_mes FROM paquetes ORDER BY nombre"
                };

                foreach (var query in queryAttempts)
                {
                    try
                    {
                        LoadPaquetesWithQuery(conn, query);
                        break;
                    }
                    catch (MySql.Data.MySqlClient.MySqlException) { }
                }

                CmbPaquete.ItemsSource = _paquetes;
                if (_paquetes.Count > 0)
                    CmbPaquete.SelectedIndex = 0;
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                var msg = ex.Message;
                if (msg.Contains("Table") && msg.Contains("doesn't exist"))
                {
                    msg += "\n\nLa tabla 'paquetes' no existe. Crea la tabla con el script adecuado o ejecuta el módulo correspondiente.";
                }
                StatusHelper.ShowError(this, "Error cargando paquetes: " + msg);
            }
        }

        private void LoadPaquetesWithQuery(MySqlConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _paquetes.Add(new Paquete
                {
                    Id = reader.GetInt32("id"),
                    Clave = reader.GetString("clave"),
                    Nombre = reader.GetString("nombre"),
                    ImporteTotal = reader.GetDecimal("importe_total"),
                    NumeroMeses = reader.GetInt32("numero_meses"),
                    NumeroDias = reader.GetInt32("numero_dias"),
                    AplicaDiasMes = ReadBooleanFlexible(reader, "aplica_dias_mes"),
                });
            }
        }

        private static bool ReadBooleanFlexible(MySqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return false;

            var value = reader.GetValue(ordinal);
            return value switch
            {
                bool b => b,
                byte b => b != 0,
                sbyte sb => sb != 0,
                short s => s != 0,
                int i => i != 0,
                long l => l != 0,
                string s when bool.TryParse(s, out var bb) => bb,
                string s when int.TryParse(s, out var ii) => ii != 0,
                _ => false
            };
        }

        private void LoadProductos()
        {
            _productos.Clear();
            CmbProducto.ItemsSource = null;

            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                var queryAttempts = new[]
                {
                    "SELECT id, clave, codigo_barras, descripcion, precio_venta, stock FROM productos ORDER BY descripcion",
                    "SELECT id_producto AS id, clave, codigo_barras, descripcion, precio_venta, stock FROM productos ORDER BY descripcion"
                };

                foreach (var query in queryAttempts)
                {
                    try
                    {
                        LoadProductosWithQuery(conn, query);
                        break;
                    }
                    catch (MySql.Data.MySqlClient.MySqlException) { }
                }

                CmbProducto.ItemsSource = _productos;
                if (_productos.Count > 0)
                    CmbProducto.SelectedIndex = 0;
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                var msg = ex.Message;
                if (msg.Contains("Table") && msg.Contains("doesn't exist"))
                {
                    msg += "\n\nLa tabla 'productos' no existe. Crea la tabla con el script adecuado o ejecuta el módulo correspondiente.";
                }
                StatusHelper.ShowError(this, "Error cargando productos: " + msg);
            }
        }

        private void LoadProductosWithQuery(MySqlConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _productos.Add(new Product
                {
                    IdProducto = reader.GetInt32("id"),
                    Clave = reader.GetString("clave"),
                    CodigoBarras = reader.GetString("codigo_barras"),
                    Descripcion = reader.GetString("descripcion"),
                    PrecioVenta = reader.GetDecimal("precio_venta"),
                    Stock = reader.GetInt32("stock"),
                });
            }
        }

        private void EnsureVentasTables()
        {
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ventas (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        fecha DATETIME NOT NULL,
                        socio_id INT NULL,
                        total DECIMAL(12,2) NOT NULL,
                        pagado DECIMAL(12,2) NOT NULL,
                        cambio DECIMAL(12,2) NOT NULL,
                        forma_pago VARCHAR(32) NOT NULL
                    )";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS venta_items (
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
            }
            catch
            {
                // ignore
            }
        }

        private void OnAgregarClick(object? sender, RoutedEventArgs e)
        {
            var cantidad = (int)(NumCantidad.Value ?? 1);

            if (CmbPaquete.SelectedItem is Paquete paquete)
            {
                AddItemPaquete(paquete, cantidad);
            }
            else if (CmbProducto.SelectedItem is Product prod)
            {
                AddItemProducto(prod, cantidad);
            }
        }

        private void OnAgregarPaqueteClick(object? sender, RoutedEventArgs e)
        {
            var cantidad = (int)(NumCantidad.Value ?? 1);
            if (CmbPaquete.SelectedItem is Paquete paquete)
                AddItemPaquete(paquete, cantidad);
            UpdateProductoCantidadTexto();
        }

        private void OnAgregarProductoClick(object? sender, RoutedEventArgs e)
        {
            var cantidad = (int)(NumCantidad.Value ?? 1);
            if (CmbProducto.SelectedItem is Product prod)
                AddItemProducto(prod, cantidad);
            UpdateProductoCantidadTexto();
        }

        private void OnProductoSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateProductoCantidadTexto();
        }

        private void OnCantidadValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            UpdateProductoCantidadTexto();
        }

        private void UpdateProductoCantidadTexto()
        {
            if (CmbProducto.SelectedItem is Product prod)
            {
                var cantidad = (int)(NumCantidad.Value ?? 1);
                TxtProductoCantidad.Text = $"{prod.Descripcion} / {cantidad}";
            }
            else
            {
                TxtProductoCantidad.Text = "Producto / 1";
            }
        }

        private void AddItemPaquete(Paquete paquete, int cantidad)
        {
            AddToCart(new CartItem
            {
                Tipo = "Paquete",
                Clave = paquete.Clave,
                Descripcion = paquete.Nombre,
                Cantidad = cantidad,
                Precio = paquete.ImporteTotal,
                PaqueteId = paquete.Id
            });
        }

        private void AddItemProducto(Product prod, int cantidad)
        {
            AddToCart(new CartItem
            {
                Tipo = "Producto",
                Clave = prod.Clave,
                Descripcion = prod.Descripcion,
                Cantidad = cantidad,
                Precio = prod.PrecioVenta,
                ProductoId = prod.IdProducto
            });
        }

        private void AddToCart(CartItem item)
        {
            var existing = _carrito.FirstOrDefault(x => x.Tipo == item.Tipo && x.Clave == item.Clave);
            if (existing != null)
            {
                existing.Cantidad += item.Cantidad;
                GridCarrito.ItemsSource = null;
                GridCarrito.ItemsSource = _carrito;
            }
            else
            {
                _carrito.Add(item);
            }

            UpdateTotals();
        }

        private void OnEliminarItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CartItem item)
            {
                _carrito.Remove(item);
                UpdateTotals();
            }
        }

        private void UpdateTotals()
        {
            var total = _carrito.Sum(x => x.Subtotal);
            TxtTotal.Text = total.ToString("C", CultureInfo.CurrentCulture);

            var pagadoText = TxtPagado.Text ?? "0";
            if (decimal.TryParse(pagadoText, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out var pagado))
            {
                var cambio = pagado - total;
                TxtCambio.Text = cambio.ToString("C", CultureInfo.CurrentCulture);
            }
            else
            {
                TxtCambio.Text = 0m.ToString("C", CultureInfo.CurrentCulture);
            }
        }

        private void OnPagadoTextChanged(object? sender, RoutedEventArgs e)
        {
            UpdateTotals();
        }

        private void OnFormaPagoChanged(object? sender, SelectionChangedEventArgs e)
        {
            var selected = (CmbFormaPago.SelectedItem as ComboBoxItem)?.Content?.ToString();
            TxtNota.Text = selected == "TARJETA" ? "Pago con Tarjeta" : "Pago en efectivo";
        }

        private void OnLimpiarClick(object? sender, RoutedEventArgs e)
        {
            _carrito.Clear();
            GridCarrito.ItemsSource = null;
            GridCarrito.ItemsSource = _carrito;
            TxtPagado.Text = "0";
            UpdateTotals();
            NumCantidad.Value = 1;
            StatusHelper.Clear(this);
        }

        private void OnConfirmarPagoClick(object? sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtTotal.Text, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out var total))
                return;

            if (!decimal.TryParse(TxtPagado.Text, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out var pagado))
                return;

            var formaPago = (CmbFormaPago.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "EFECTIVO";
            var socio = CmbSocio.SelectedItem as Socio;

            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                using var tx = conn.BeginTransaction();
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;

                cmd.CommandText = "INSERT INTO ventas (fecha, socio_id, total, pagado, cambio, forma_pago) VALUES (@fecha, @socio, @total, @pagado, @cambio, @forma)";
                cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                cmd.Parameters.AddWithValue("@socio", (object?)socio?.IdSocio ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@total", total);
                cmd.Parameters.AddWithValue("@pagado", pagado);
                cmd.Parameters.AddWithValue("@cambio", pagado - total);
                cmd.Parameters.AddWithValue("@forma", formaPago);
                cmd.ExecuteNonQuery();

                var ventaId = (long)cmd.LastInsertedId;

                foreach (var item in _carrito)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = @"INSERT INTO venta_items (venta_id, tipo, producto_id, paquete_id, clave, descripcion, cantidad, precio)
                                        VALUES (@ventaId, @tipo, @productoId, @paqueteId, @clave, @descripcion, @cantidad, @precio)";
                    cmd.Parameters.AddWithValue("@ventaId", ventaId);
                    cmd.Parameters.AddWithValue("@tipo", item.Tipo);
                    cmd.Parameters.AddWithValue("@productoId", (object?)item.ProductoId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@paqueteId", (object?)item.PaqueteId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@clave", item.Clave);
                    cmd.Parameters.AddWithValue("@descripcion", item.Descripcion);
                    cmd.Parameters.AddWithValue("@cantidad", item.Cantidad);
                    cmd.Parameters.AddWithValue("@precio", item.Precio);
                    cmd.ExecuteNonQuery();

                    if (item.Tipo == "Producto" && item.ProductoId.HasValue)
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = "UPDATE productos SET stock = stock - @cantidad WHERE id = @id";
                        cmd.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        cmd.Parameters.AddWithValue("@id", item.ProductoId.Value);
                        cmd.ExecuteNonQuery();
                    }

                    if (item.Tipo == "Paquete" && socio != null && item.PaqueteId.HasValue)
                    {
                        var paquete = _paquetes.FirstOrDefault(p => p.Id == item.PaqueteId.Value);
                        if (paquete != null)
                        {
                            var inicio = DateTime.Now;
                            var fin = inicio.AddMonths(paquete.NumeroMeses);
                            cmd.Parameters.Clear();
                            cmd.CommandText = @"INSERT INTO membresias (socio_id, paquete_id, fecha_inicio, fecha_fin)
                                                VALUES (@socio, @paquete, @inicio, @fin)";
                            cmd.Parameters.AddWithValue("@socio", socio.IdSocio);
                            cmd.Parameters.AddWithValue("@paquete", paquete.Id);
                            cmd.Parameters.AddWithValue("@inicio", inicio);
                            cmd.Parameters.AddWithValue("@fin", fin);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                tx.Commit();
                StatusHelper.ShowSuccess(this, "Venta registrada con éxito.");
                OnLimpiarClick(this, new RoutedEventArgs());
                LoadProductos();
            }
            catch (Exception ex)
            {
                StatusHelper.ShowError(this, "Error procesando el pago: " + ex.Message);
            }
        }
    }

    public class CartItem
    {
        public string Tipo { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal => Precio * Cantidad;
        public int? ProductoId { get; set; }
        public int? PaqueteId { get; set; }
    }

    public static class StatusHelper
    {
        public static void Clear(UserControl control)
        {
            // No-op for now (puede ampliarse si querés mostrar status)
        }

        public static void ShowSuccess(UserControl control, string message)
        {
            ShowDialog(control, "Éxito", message);
        }

        public static void ShowError(UserControl control, string message)
        {
            ShowDialog(control, "Error", message);
        }

        private static void ShowDialog(UserControl control, string title, string message)
        {
            var owner = control.GetVisualRoot() as Window;

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(0, 12, 0, 0),
            };

            var panel = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    okButton
                }
            };

            var dlg = new Window
            {
                Title = title,
                Width = 360,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = panel
            };

            okButton.Click += (_, __) => dlg.Close();

            if (owner != null)
                dlg.ShowDialog(owner);
            else
                dlg.Show();
        }
    }
}
