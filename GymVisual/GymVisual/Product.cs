using Avalonia.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GymVisual;

public class Product : INotifyPropertyChanged
{
    private int _stock;

    public int IdProducto { get; set; }
    public string Clave { get; set; } = "";
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Costo { get; set; }
    public decimal PrecioVenta { get; set; }
    public string Iva { get; set; } = "";
    public int Stock
    {
        get => _stock;
        set
        {
            if (_stock == value) return;
            _stock = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StockBrush));
        }
    }
    public string Departamento { get; set; } = "";

    public string DisplayName => $"{Clave} - {Descripcion} ({Stock} en stock)";
    public override string ToString() => DisplayName;

    public IBrush StockBrush => Stock < 5 ? Brushes.IndianRed : Brushes.LightGreen;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
