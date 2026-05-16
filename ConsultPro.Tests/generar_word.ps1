$ErrorActionPreference = 'Stop'

$rutaSalida = Join-Path $PSScriptRoot 'ResultadosPruebas.docx'
if (Test-Path $rutaSalida) { Remove-Item $rutaSalida -Force }

$word = New-Object -ComObject Word.Application
$word.Visible = $false

try {
    $doc = $word.Documents.Add()
    $sel = $word.Selection

    # Título
    $sel.Style = 'Título 1'
    $sel.TypeText('Resultados de pruebas - ClientesControllerTests')
    $sel.TypeParagraph()

    # Subtítulo / metadatos
    $sel.Style = 'Normal'
    $sel.Font.Italic = $true
    $sel.TypeText("Proyecto: ConsultPro.API.Tests   |   Framework: xUnit   |   Target: .NET 8.0")
    $sel.TypeParagraph()
    $sel.TypeText("Resultado global: 8 / 8 superados   |   Duración total: 1,89 s")
    $sel.Font.Italic = $false
    $sel.TypeParagraph()
    $sel.TypeParagraph()

    # Datos de la tabla (encabezados + filas)
    $filas = @(
        @('#',  'Test', 'Tiempo', 'Qué valida'),
        @('1',  'PU_1 Create_ConDatosValidos_DevuelveCreatedConCliente',           '762 ms', 'POST /api/clientes con datos válidos devuelve 201 Created y persiste el cliente'),
        @('2',  'PU_2 GetById_ConIdInexistente_DevuelveNotFound',                  '1 ms',   'GET /api/clientes/9999 devuelve 404 NotFound'),
        @('3',  'PU_3 Get_SinFiltro_DevuelveTodosOrdenadosPorNombre',              '7 ms',   'GET /api/clientes devuelve los clientes ordenados alfabéticamente (Alpha, Beta, Zeta)'),
        @('4',  'PU_4 Get_ConFiltro (q="banco", esperados=2)',                     '< 1 ms', 'Filtra por nombre: matchea "Banco Andino" y "Banco Sur"'),
        @('5',  'PU_4 Get_ConFiltro (q="BANCO", esperados=2)',                     '54 ms',  'Mismo resultado en mayúsculas: confirma búsqueda case-insensitive'),
        @('6',  'PU_4 Get_ConFiltro (q="tecnologia", esperados=1)',                '< 1 ms', 'Filtra por industria, no sólo por nombre'),
        @('7',  'PU_4 Get_ConFiltro (q="inexistente", esperados=0)',               '19 ms',  'Sin matches devuelve lista vacía (no error)'),
        @('8',  'PU_5 Delete_ClienteConProyectosActivos_DevuelveBadRequest',       '75 ms',  'Regla de negocio: no se puede borrar un cliente con proyectos no finalizados; devuelve 400 BadRequest')
    )

    $numFilas = $filas.Count
    $numCols = 4

    # Insertar tabla
    $range = $sel.Range
    $tabla = $doc.Tables.Add($range, $numFilas, $numCols)
    $tabla.Borders.Enable = $true
    $tabla.Range.Font.Size = 10
    $tabla.Range.Font.Name = 'Calibri'

    # Cargar contenido
    for ($i = 0; $i -lt $numFilas; $i++) {
        for ($j = 0; $j -lt $numCols; $j++) {
            $celda = $tabla.Cell($i + 1, $j + 1)
            $celda.Range.Text = $filas[$i][$j]
        }
    }

    # Formatear encabezado
    $encabezado = $tabla.Rows.Item(1)
    $encabezado.Range.Font.Bold = $true
    $encabezado.Shading.BackgroundPatternColor = 0xD9D9D9  # gris claro
    $encabezado.HeadingFormat = $true

    # Anchos de columna (puntos; 72 pt = 1 pulgada)
    $tabla.Columns.Item(1).PreferredWidthType = 1; $tabla.Columns.Item(1).PreferredWidth = 25
    $tabla.Columns.Item(2).PreferredWidthType = 1; $tabla.Columns.Item(2).PreferredWidth = 180
    $tabla.Columns.Item(3).PreferredWidthType = 1; $tabla.Columns.Item(3).PreferredWidth = 55
    $tabla.Columns.Item(4).PreferredWidthType = 1; $tabla.Columns.Item(4).PreferredWidth = 220

    # Mover cursor al final del documento
    $finDoc = $doc.Range()
    $finDoc.Collapse(0)  # wdCollapseEnd
    $finDoc.Select()
    $sel.TypeParagraph()

    # Sección "Observaciones"
    $sel.Style = 'Título 2'
    $sel.TypeText('Observaciones')
    $sel.TypeParagraph()

    $sel.Style = 'Normal'
    $sel.Font.Bold = $false

    $obs = @(
        'PU_1 tardó 762 ms y el resto milisegundos. Es el "calentamiento": el primer test inicializa EF Core, el proveedor InMemory y JIT-compila el código. Los siguientes lo reusan.',
        'PU_4 aparece 4 veces porque es un [Theory] con 4 [InlineData]. xUnit lo trata como 4 tests independientes y muestra los parámetros entre paréntesis.',
        'El orden en pantalla no es el orden de ejecución secuencial: xUnit corre tests en paralelo y los muestra a medida que terminan.'
    )

    foreach ($p in $obs) {
        $sel.Range.ListFormat.ApplyBulletDefault()
        $sel.TypeText($p)
        $sel.TypeParagraph()
    }
    $sel.Range.ListFormat.RemoveNumbers()

    # Guardar como .docx (formato 16 = wdFormatDocumentDefault)
    $doc.SaveAs([ref]$rutaSalida, [ref]16)
    $doc.Close()

    Write-Output "OK: $rutaSalida"
}
finally {
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
