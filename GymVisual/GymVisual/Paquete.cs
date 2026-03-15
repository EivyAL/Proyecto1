using System;

namespace GymVisual;

public class Paquete
{
    public int Id { get; set; }
    public string Clave { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";

    public decimal ImporteTotal { get; set; }
    public decimal ImportePorDia { get; set; }

    public int NumeroDias { get; set; }
    public int NumeroMeses { get; set; }
    public bool AplicaDiasMes { get; set; }

    public DateTime? VigenciaInicio { get; set; }
    public DateTime? VigenciaFin { get; set; }

    public override string ToString() => $"{Nombre} ({Clave}) - {ImporteTotal:C} / {NumeroDias} días";
}
