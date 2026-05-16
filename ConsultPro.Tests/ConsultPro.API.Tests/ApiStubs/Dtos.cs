namespace ConsultPro.API.DTOs;

public class ClienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Industria { get; set; }
    public string? PersonaContacto { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; }
    public int CantidadProyectos { get; set; }
}

public class ClienteUpsertDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Industria { get; set; }
    public string? PersonaContacto { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; }
}
