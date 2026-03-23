using System;

namespace GymVisual;

public class Socio
{
    public int IdSocio { get; set; }
    public string Clave { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string ApellidoP { get; set; } = "";
    public string ApellidoM { get; set; } = "";
    public string Sexo { get; set; } = "";
    public DateTime? FechaNacimiento { get; set; }
    public string Ocupacion { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime? FechaIngreso { get; set; }
    public bool Activo { get; set; }
    public string Observaciones { get; set; } = "";
    public int? IdDireccion { get; set; }
    public string Telefono { get; set; } = "";
    public string TelefonoEmergencia { get; set; } = "";
    public byte[]? Foto { get; set; }
    public string Estatus { get; set; } = "";

    public string DisplayName => $"{Clave} - {Nombre} {ApellidoP} {ApellidoM}".Trim();

    public override string ToString() => DisplayName;
}