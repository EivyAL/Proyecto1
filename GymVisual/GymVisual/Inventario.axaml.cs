using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace GymVisual
{
    public partial class Inventario : UserControl
    {
        private ObservableCollection<Product> _productos = new();
        private Product? _selectedProduct;
        private bool _useClaveAsKey;
        private string? _selectedProductOriginalClave;

        public Inventario()
        {
            InitializeComponent();
            LoadProductos();
        }

        private void LoadProductos()
        {
            _productos.Clear();

            try
            {
                EnsureProductosTable();

                using var conn = Database.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM productos";

                using var reader = cmd.ExecuteReader();
                var idOrdinal = -1;
                try
                {
                    idOrdinal = reader.GetOrdinal("id");
                }
                catch (IndexOutOfRangeException)
                {
                    idOrdinal = -1;
                }

                while (reader.Read())
                {
                    var prod = new Product
                    {
                        IdProducto = idOrdinal >= 0 ? reader.GetInt32(idOrdinal) : 0,
                        Clave = reader.GetString("clave"),
                        CodigoBarras = reader.GetString("codigo_barras"),
                        Descripcion = reader.GetString("descripcion"),
                        Costo = reader.GetDecimal("costo"),
                        PrecioVenta = reader.GetDecimal("precio_venta"),
                        Iva = reader.GetString("iva"),
                        Stock = reader.GetInt32("stock"),
                        Departamento = reader.GetString("departamento"),
                    };

                    _productos.Add(prod);
                }

                GridProductos.ItemsSource = _productos;
            }
            catch (Exception ex)
            {
                StatusMsg.Text = "Error cargando productos: " + ex.Message;
            }
        }

        private void OnProductSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (GridProductos.SelectedItem is Product prod)
            {
                _selectedProduct = prod;
                PopulateProductForm(prod);
            }
        }

        private void PopulateProductForm(Product prod)
        {
            TxtClave.Text = prod.Clave;
            TxtCodigo.Text = prod.CodigoBarras;
            TxtDescripcion.Text = prod.Descripcion;
            TxtCosto.Text = prod.Costo.ToString("0.00");
            TxtPrecio.Text = prod.PrecioVenta.ToString("0.00");
            TxtIva.Text = prod.Iva;
            TxtStock.Text = prod.Stock.ToString();
            SetDepartamentoSelection(prod.Departamento);
            StatusMsg.Text = string.Empty;

            _selectedProductOriginalClave = prod.Clave;
        }

        private void OnNuevoProductoClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _selectedProduct = null;
            _selectedProductOriginalClave = null;
            GridProductos.SelectedItem = null;
            ClearProductForm();
            StatusMsg.Text = string.Empty;
        }

        private void OnEditarProductoClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (GridProductos.SelectedItem is Product prod)
            {
                _selectedProduct = prod;
                PopulateProductForm(prod);
            }
            else
            {
                StatusMsg.Text = "Selecciona un producto para poder editarlo.";
            }
        }

        private void ClearProductForm()
        {
            TxtClave.Text = string.Empty;
            TxtCodigo.Text = string.Empty;
            TxtDescripcion.Text = string.Empty;
            TxtCosto.Text = string.Empty;
            TxtPrecio.Text = string.Empty;
            TxtIva.Text = string.Empty;
            TxtStock.Text = string.Empty;
            CmbDepartamento.SelectedIndex = -1;
        }

        private void SetDepartamentoSelection(string departamento)
        {
            foreach (var item in CmbDepartamento.Items)
            {
                if (item is ComboBoxItem cbi && cbi.Content is string text && string.Equals(text, departamento, StringComparison.OrdinalIgnoreCase))
                {
                    CmbDepartamento.SelectedItem = cbi;
                    return;
                }
            }

            CmbDepartamento.SelectedIndex = -1;
        }

        private void OnCodigoBarrasKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchByBarcode(TxtCodigoBarras.Text?.Trim());
            }
        }

        private void SearchByBarcode(string? codigoBarras)
        {
            if (string.IsNullOrWhiteSpace(codigoBarras))
            {
                StatusMsg.Text = "Ingrese un código de barras.";
                return;
            }

            var match = _productos.FirstOrDefault(p => string.Equals(p.CodigoBarras, codigoBarras, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                GridProductos.SelectedItem = match;
                var firstColumn = GridProductos.Columns.FirstOrDefault();
                if (firstColumn != null)
                {
                    GridProductos.ScrollIntoView(match, firstColumn);
                }

                // Keep track of the original clave for updates when the table does not have an ID column
                _selectedProductOriginalClave = match.Clave;

                TxtCodigoBarras.Text = string.Empty;
                StatusMsg.Text = string.Empty;
                return;
            }

            StatusMsg.Text = "Producto no encontrado.";
        }

        private void OnGuardarProductoClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            StatusMsg.Text = string.Empty;

            if (!decimal.TryParse(TxtCosto.Text, out var costo))
            {
                StatusMsg.Text = "Costo inválido.";
                return;
            }

            if (!decimal.TryParse(TxtPrecio.Text, out var precio))
            {
                StatusMsg.Text = "Precio inválido.";
                return;
            }

            if (precio <= costo)
            {
                StatusMsg.Text = "El precio de venta debe ser mayor al costo.";
                return;
            }

            if (!int.TryParse(TxtStock.Text, out var stock))
            {
                StatusMsg.Text = "Stock inválido.";
                return;
            }

            var clave = TxtClave.Text?.Trim() ?? string.Empty;
            var codigo = TxtCodigo.Text?.Trim() ?? string.Empty;
            var desc = TxtDescripcion.Text?.Trim() ?? string.Empty;
            var iva = TxtIva.Text?.Trim() ?? string.Empty;
            var dept = (CmbDepartamento.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(clave) || string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(desc))
            {
                StatusMsg.Text = "Clave, código de barras y descripción son obligatorios.";
                return;
            }

            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                if (_selectedProduct == null)
                {
                    cmd.CommandText = @"INSERT INTO productos (clave, codigo_barras, descripcion, costo, precio_venta, iva, stock, departamento)
                                        VALUES (@clave, @codigo, @descripcion, @costo, @precio, @iva, @stock, @departamento)";
                }
                else
                {
                    if (_useClaveAsKey && !string.IsNullOrWhiteSpace(_selectedProductOriginalClave))
                    {
                        cmd.CommandText = @"UPDATE productos SET clave = @clave, codigo_barras = @codigo, descripcion = @descripcion,
                                            costo = @costo, precio_venta = @precio, iva = @iva, stock = @stock, departamento = @departamento
                                            WHERE clave = @originalClave";
                        cmd.Parameters.AddWithValue("@originalClave", _selectedProductOriginalClave);
                    }
                    else
                    {
                        cmd.CommandText = @"UPDATE productos SET clave = @clave, codigo_barras = @codigo, descripcion = @descripcion,
                                            costo = @costo, precio_venta = @precio, iva = @iva, stock = @stock, departamento = @departamento
                                            WHERE id = @id";
                        cmd.Parameters.AddWithValue("@id", _selectedProduct.IdProducto);
                    }
                }

                cmd.Parameters.AddWithValue("@clave", clave);
                cmd.Parameters.AddWithValue("@codigo", codigo);
                cmd.Parameters.AddWithValue("@descripcion", desc);
                cmd.Parameters.AddWithValue("@costo", costo);
                cmd.Parameters.AddWithValue("@precio", precio);
                cmd.Parameters.AddWithValue("@iva", iva);
                cmd.Parameters.AddWithValue("@stock", stock);
                cmd.Parameters.AddWithValue("@departamento", dept);

                cmd.ExecuteNonQuery();

                LoadProductos();
                StatusMsg.Foreground = Brushes.Green;
                StatusMsg.Text = "Producto guardado correctamente.";

                // Reset selection/form
                _selectedProduct = null;
                TxtClave.Text = string.Empty;
                TxtCodigo.Text = string.Empty;
                TxtDescripcion.Text = string.Empty;
                TxtCosto.Text = string.Empty;
                TxtPrecio.Text = string.Empty;
                TxtIva.Text = string.Empty;
                TxtStock.Text = string.Empty;
                CmbDepartamento.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                StatusMsg.Foreground = Brushes.Red;
                StatusMsg.Text = "Error guardando producto: " + ex.Message;
            }
        }

        private void EnsureProductosTable()
        {
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS productos (
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

                // If the table exists but doesn't have an 'id' column, fall back to using 'clave' as key.
                cmd.CommandText = "SHOW COLUMNS FROM productos LIKE 'id'";
                var hasIdColumn = cmd.ExecuteScalar() != null;

                if (!hasIdColumn)
                {
                    try
                    {
                        cmd.CommandText = "ALTER TABLE productos ADD COLUMN id INT AUTO_INCREMENT PRIMARY KEY FIRST";
                        cmd.ExecuteNonQuery();
                        _useClaveAsKey = false;
                    }
                    catch
                    {
                        // Unable to add an id column (likely because another PK exists)
                        _useClaveAsKey = true;
                    }
                }
                else
                {
                    _useClaveAsKey = false;
                }
            }
            catch
            {
                // Ignore: errors will be surfaced on the actual load/save operations
                _useClaveAsKey = true;
            }
        }
    }
}
