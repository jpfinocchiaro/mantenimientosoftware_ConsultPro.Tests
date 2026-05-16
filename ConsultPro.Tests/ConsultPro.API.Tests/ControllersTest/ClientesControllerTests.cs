using ConsultPro.API.Controllers;
using ConsultPro.API.Data;
using ConsultPro.API.DTOs;
using ConsultPro.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControllersAPIOriginal;

/// <summary>
/// Suite de pruebas unitarias del ClientesController.
///
/// Cada prueba crea su propio DbContext en memoria con un nombre único de base
/// de datos para garantizar aislamiento entre ejecuciones y posibilitar correr
/// el suite en paralelo sin colisiones de datos.
/// </summary>
public class ClientesControllerTests
{
    /// <summary>
    /// Crea un ApplicationDbContext con proveedor InMemory.
    /// </summary>
    /// <param name="nombreDb">
    /// Identificador único de la base in-memory. Se usa <c>nameof(MiTest)</c>
    /// para que cada test tenga su propia instancia.
    /// </param>
    private static ApplicationDbContext CrearContextoEnMemoria(string nombreDb)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: nombreDb)
            .Options;
        return new ApplicationDbContext(options);
    }

    // -----------------------------------------------------------------
    // PU-1 - Create con datos válidos devuelve 201 Created
    // Cubre: PC-1 / HU-1, HU-2
    // -----------------------------------------------------------------
    [Fact]
    public async Task PU_1_Create_ConDatosValidos_DevuelveCreatedConCliente()
    {
        // Arrange
        using var db = CrearContextoEnMemoria(nameof(PU_1_Create_ConDatosValidos_DevuelveCreatedConCliente));
        var controller = new ClientesController(db);
        var dto = new ClienteUpsertDto
        {
            Nombre = "Banco Andino S.A.",
            Industria = "Banca",
            PersonaContacto = "María López",
            Email = "mlopez@bancoandino.com",
            Activo = true
        };

        // Act
        var resultado = await controller.Create(dto);

        // Assert
        var created = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var clienteDto = created.Value.Should().BeOfType<ClienteDto>().Subject;
        clienteDto.Nombre.Should().Be("Banco Andino S.A.");
        clienteDto.Email.Should().Be("mlopez@bancoandino.com");
        clienteDto.Activo.Should().BeTrue();

        // Verifica persistencia real en el contexto
        db.Clientes.Should().HaveCount(1);
        db.Clientes.Single().Nombre.Should().Be("Banco Andino S.A.");
    }

    // -----------------------------------------------------------------
    // PU-2 - GetById con id inexistente devuelve NotFound
    // -----------------------------------------------------------------
    [Fact]
    public async Task PU_2_GetById_ConIdInexistente_DevuelveNotFound()
    {
        // Arrange
        using var db = CrearContextoEnMemoria(nameof(PU_2_GetById_ConIdInexistente_DevuelveNotFound));
        db.Clientes.Add(new Cliente { Nombre = "Cliente Existente", Activo = true });
        await db.SaveChangesAsync();
        var controller = new ClientesController(db);

        // Act
        var resultado = await controller.GetById(id: 9999);

        // Assert
        resultado.Result.Should().BeOfType<NotFoundResult>();
    }

    // -----------------------------------------------------------------
    // PU-3 - Get sin filtro devuelve clientes ordenados por nombre
    // -----------------------------------------------------------------
    [Fact]
    public async Task PU_3_Get_SinFiltro_DevuelveTodosOrdenadosPorNombre()
    {
        // Arrange
        using var db = CrearContextoEnMemoria(nameof(PU_3_Get_SinFiltro_DevuelveTodosOrdenadosPorNombre));
        db.Clientes.AddRange(
            new Cliente { Nombre = "Zeta Corp",  Activo = true },
            new Cliente { Nombre = "Alpha S.A.", Activo = true },
            new Cliente { Nombre = "Beta SRL",   Activo = false }
        );
        await db.SaveChangesAsync();
        var controller = new ClientesController(db);

        // Act
        var resultado = await controller.Get(q: null);

        // Assert
        var lista = resultado.Value.Should().BeAssignableTo<IEnumerable<ClienteDto>>().Subject.ToList();
        lista.Should().HaveCount(3);
        lista.Select(c => c.Nombre).Should().ContainInOrder("Alpha S.A.", "Beta SRL", "Zeta Corp");
    }

    // -----------------------------------------------------------------
    // PU-4 - Get con filtro hace búsqueda case-insensitive
    //
    // Theory con cuatro combinaciones para cubrir:
    //  - match por Nombre
    //  - case-insensitive
    //  - match por Industria
    //  - sin resultados
    // -----------------------------------------------------------------
    [Theory]
    [InlineData("banco",       2)]   // matchea "Banco Andino" y "Banco Sur"
    [InlineData("BANCO",       2)]   // case-insensitive
    [InlineData("tecnologia",  1)]   // matchea por Industria
    [InlineData("inexistente", 0)]   // ningún resultado
    public async Task PU_4_Get_ConFiltro_HaceMatchCaseInsensitive(string filtro, int esperados)
    {
        // Arrange
        using var db = CrearContextoEnMemoria($"PU_4_FiltroTest_{filtro}");
        db.Clientes.AddRange(
            new Cliente { Nombre = "Banco Andino",   Industria = "Banca",      Activo = true },
            new Cliente { Nombre = "Banco Sur",      Industria = "Banca",      Activo = true },
            new Cliente { Nombre = "Tech Solutions", Industria = "Tecnologia", Activo = true }
        );
        await db.SaveChangesAsync();
        var controller = new ClientesController(db);

        // Act
        var resultado = await controller.Get(q: filtro);

        // Assert
        var lista = resultado.Value.Should().BeAssignableTo<IEnumerable<ClienteDto>>().Subject;
        lista.Should().HaveCount(esperados);
    }

    // -----------------------------------------------------------------
    // PU-5 - Delete de cliente con proyectos activos devuelve BadRequest
    //
    // Esta prueba protege una regla de negocio importante: no debe poder
    // eliminarse un cliente con proyectos en estados distintos de Finalizado.
    // -----------------------------------------------------------------
    [Fact]
    public async Task PU_5_Delete_ClienteConProyectosActivos_DevuelveBadRequest()
    {
        // Arrange
        using var db = CrearContextoEnMemoria(nameof(PU_5_Delete_ClienteConProyectosActivos_DevuelveBadRequest));
        var cliente = new Cliente
        {
            Nombre = "Cliente con Proyectos",
            Activo = true,
            Proyectos = new List<Proyecto>
            {
                new() { Nombre = "Proyecto en curso", Estado = EstadoProyecto.EnEjecucion }
            }
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        var controller = new ClientesController(db);

        // Act
        var resultado = await controller.Delete(cliente.Id);

        // Assert
        var bad = resultado.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new
        {
            error = "No se puede eliminar un cliente con proyectos activos."
        });

        // El cliente NO se eliminó de la base
        db.Clientes.Should().HaveCount(1);
    }
}
