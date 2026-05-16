# ConsultPro.Tests

Repositorio con la suite de pruebas unitarias del backend de **ConsultPro**, sistema de gestión integral para consultoras desarrollado sobre ASP.NET Core 8 (C#) y Microsoft SQL Server.

Las pruebas aquí presentes corresponden al anexo técnico de la **Actividad 3 — Mantenimiento y Evolución de Software (VIU, 1ª convocatoria)** del alumno **Juan Pablo Finocchiaro** y verifican el comportamiento del controlador `ClientesController`.

## Estructura del repositorio

```
ConsultPro.Tests/
├── ConsultPro.API.Tests/
│   ├── ConsultPro.API.Tests.csproj
│   └── Controllers/
│       └── ClientesControllerTests.cs
├── .gitignore
└── README.md
```

## Stack utilizado

| Componente | Versión |
|------------|---------|
| .NET SDK | 8.0 |
| xUnit | 2.6.x |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.x |
| FluentAssertions | 6.12.x |

## Cómo ejecutar las pruebas

Requisitos previos: tener instalado el SDK de .NET 8.

```bash
# Clonar el repositorio
git clone https://github.com/<usuario>/ConsultPro.Tests.git
cd ConsultPro.Tests

# Restaurar dependencias
dotnet restore

# Ejecutar la suite completa
dotnet test

# Ejecutar con salida detallada
dotnet test --verbosity normal

# Ejecutar un único test
dotnet test --filter "FullyQualifiedName~PU_1"
```

## Catálogo de pruebas

La suite contiene cinco pruebas que cubren los principales métodos del controlador:

| ID | Método cubierto | Descripción | Caso de prueba relacionado |
|----|-----------------|-------------|---------------------------|
| PU-1 | `Create` | Alta de cliente con datos válidos devuelve 201 Created | PC-1 |
| PU-2 | `GetById` | Consulta por id inexistente devuelve NotFound | Cobertura adicional |
| PU-3 | `Get` | Listado sin filtro devuelve clientes ordenados por nombre | Cobertura adicional |
| PU-4 | `Get` | Filtro `q` realiza búsqueda case-insensitive (Theory con 4 inputs) | Cobertura adicional |
| PU-5 | `Delete` | Eliminación de cliente con proyectos activos devuelve BadRequest | Regla de negocio |

## Diseño de las pruebas

Cada test crea su propio `ApplicationDbContext` con proveedor InMemory utilizando un nombre de base de datos único (basado en `nameof(NombreDelTest)`), lo cual garantiza el aislamiento total entre tests y permite ejecutarlos en paralelo sin contaminación de datos.

Las aserciones se escriben con **FluentAssertions** para lograr mensajes de error más legibles y un estilo de assert más expresivo que el de `Assert.*` clásico de xUnit.

## Integración con CI/CD

Las pruebas están preparadas para integrarse a un pipeline de CI. Una configuración mínima para GitHub Actions sería:

```yaml
name: tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - run: dotnet restore
      - run: dotnet test --no-restore --logger "trx;LogFileName=test-results.trx"
```

## Autor

Juan Pablo Finocchiaro — Mantenimiento y Evolución de Software — VIU (Universidad Internacional de Valencia)
