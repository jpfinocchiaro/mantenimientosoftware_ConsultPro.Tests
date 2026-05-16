namespace ConsultPro.API.Models;

public enum EstadoProyecto
{
    Planificado,
    EnEjecucion,
    Pausado,
    Finalizado,
    Cancelado
}

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Industria { get; set; }
    public string? PersonaContacto { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; }
    public ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}

public class Proyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public EstadoProyecto Estado { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
}
