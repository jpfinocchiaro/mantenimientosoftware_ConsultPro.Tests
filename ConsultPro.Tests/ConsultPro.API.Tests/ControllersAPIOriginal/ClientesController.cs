using ConsultPro.API.Data;
using ConsultPro.API.DTOs;
using ConsultPro.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultPro.API.Controllers;

[ApiController]
[Authorize]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ClientesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteDto>>> Get([FromQuery] string? q)
    {
        var query = _db.Clientes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.ToLower();
            query = query.Where(c => c.Nombre.ToLower().Contains(t) || (c.Industria != null && c.Industria.ToLower().Contains(t)));
        }

        return await query
            .OrderBy(c => c.Nombre)
            .Select(c => new ClienteDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Industria = c.Industria,
                PersonaContacto = c.PersonaContacto,
                Email = c.Email,
                Activo = c.Activo,
                CantidadProyectos = c.Proyectos.Count
            })
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> GetById(int id)
    {
        var c = await _db.Clientes
            .Where(c => c.Id == id)
            .Select(c => new ClienteDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Industria = c.Industria,
                PersonaContacto = c.PersonaContacto,
                Email = c.Email,
                Activo = c.Activo,
                CantidadProyectos = c.Proyectos.Count
            })
            .FirstOrDefaultAsync();

        return c is null ? NotFound() : c;
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Create(ClienteUpsertDto dto)
    {
        var c = new Cliente
        {
            Nombre = dto.Nombre,
            Industria = dto.Industria,
            PersonaContacto = dto.PersonaContacto,
            Email = dto.Email,
            Activo = dto.Activo
        };
        _db.Clientes.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, (await GetById(c.Id)).Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ClienteUpsertDto dto)
    {
        var c = await _db.Clientes.FindAsync(id);
        if (c is null) return NotFound();

        c.Nombre = dto.Nombre;
        c.Industria = dto.Industria;
        c.PersonaContacto = dto.PersonaContacto;
        c.Email = dto.Email;
        c.Activo = dto.Activo;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Clientes.Include(x => x.Proyectos).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        if (c.Proyectos.Any(p => p.Estado != EstadoProyecto.Finalizado))
            return BadRequest(new { error = "No se puede eliminar un cliente con proyectos activos." });

        _db.Clientes.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
