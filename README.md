# TodoTask API

API REST para gestionar tareas, hecha con **.NET 10**, **C# (Minimal APIs)**, **Entity Framework Core** y **SQLite**.

Cada tarea tiene título, descripción, estado (`Pending`, `InProgress`, `Done`), responsable y fecha límite.

## Cómo ejecutarlo

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) (y, solo si quieres gestionar migraciones a mano, `dotnet tool install -g dotnet-ef`).

```bash
# Arrancar la API (crea y migra la base SQLite todo.db automáticamente en Development)
dotnet run --project src/TodoApi          # http://localhost:5204

# Ejecutar los tests
dotnet test

# (opcional) aplicar migraciones manualmente / crear una nueva
dotnet ef database update --project src/TodoApi
dotnet ef migrations add NombreMigracion --project src/TodoApi --output-dir Data/Migrations
```

### Interfaz visual (Scalar)

Con la API en marcha, abre **`http://localhost:5204/scalar/v1`** (`dotnet run` con el perfil `http` la abre sola). Es una referencia interactiva de los cinco endpoints, con sus parámetros, esquemas y respuestas, y permite ejecutar cada petición con "Test Request". Se genera a partir de la especificación OpenAPI (`http://localhost:5204/openapi/v1.json`).

Ambas rutas solo existen en entorno **Development**; en otros entornos devuelven 404.

### Endpoints

| Método | Ruta | Descripción | Respuestas |
|---|---|---|---|
| `POST` | `/api/tasks` | Crear tarea | 201 (+`Location`), 400 |
| `GET` | `/api/tasks` | Listar con filtros y paginación | 200, 400 |
| `GET` | `/api/tasks/{id}` | Ver una tarea | 200, 404 |
| `PUT` | `/api/tasks/{id}` | Editar (reemplazo completo) | 200, 400, 404 |
| `DELETE` | `/api/tasks/{id}` | Borrar | 204, 404 |

Parámetros del listado: `status` (`Pending`, `InProgress`, `Done`), `assignee` (sin distinguir mayúsculas), `page` (≥ 1, por defecto 1) y `pageSize` (1–100, por defecto 20).

```bash
curl -X POST http://localhost:5204/api/tasks -H "Content-Type: application/json" \
  -d '{"title":"Escribir README","description":"Con decisiones","assignee":"ana","dueDate":"2030-01-01T00:00:00Z"}'

curl "http://localhost:5204/api/tasks?status=Pending&assignee=ana&page=1&pageSize=10"

curl -X PUT http://localhost:5204/api/tasks/1 -H "Content-Type: application/json" \
  -d '{"title":"Escribir README","assignee":"ana","status":"Done"}'

curl -X DELETE http://localhost:5204/api/tasks/1
```

Respuesta del listado:

```json
{ "items": [ { "id": 1, "title": "...", "status": "Pending", "assignee": "ana", "dueDate": "2030-01-01T00:00:00Z", "...": "..." } ],
  "page": 1, "pageSize": 10, "totalCount": 1, "totalPages": 1 }
```

Los errores usan `ProblemDetails` (RFC 7807, `application/problem+json`). Los de validación incluyen un objeto `errors` por campo.

## Decisiones

- **Minimal APIs + un solo proyecto de API.** El dominio es pequeño; separar en capas (Clean Architecture, repositorios) añadiría ficheros sin aportar claridad. Los endpoints viven en `Endpoints/TaskEndpoints.cs`.
- **DTOs separados de la entidad** (`CreateTaskRequest`, `UpdateTaskRequest`, `TaskResponse`, `PagedResult<T>`): el contrato HTTP no queda atado al modelo de datos.
- **Validación con DataAnnotations + `AddValidation()`** (nativo de .NET 10) en lugar de FluentValidation: menos dependencias y suficiente para estas reglas (título y responsable obligatorios, longitudes máximas, fecha límite no pasada al **crear**). Al **editar** se permiten fechas pasadas, para poder tocar tareas ya vencidas.
- **Errores coherentes:** 201 con `Location`, 204 al borrar, 400 en validación o petición mal formada, 404 si no existe, 500 genérico sin filtrar detalles internos (`IExceptionHandler`).
- **`PUT` como reemplazo completo** (no `PATCH`): más simple y predecible para el cliente.
- **Estado como texto** en JSON y en base de datos (`"InProgress"`), legible y robusto frente a reordenar el enum. Valores numéricos o desconocidos se rechazan.
- **Fechas siempre en UTC.** SQLite no guarda zona horaria, así que un convertidor las marca como UTC al leer (se serializan con `Z`).
- **Paginación y filtros:** `pageSize` máximo 100, orden estable (fecha límite ascendente con las tareas sin fecha al final, y luego `Id`). Índices en `Status` y `Assignee`.
- **Migraciones EF Core** en `Data/Migrations`, aplicadas automáticamente solo en Development. En otros entornos deben aplicarse de forma explícita (`dotnet ef database update`).
- **Tests de integración** con `WebApplicationFactory` contra la API real y una base SQLite temporal creada con las migraciones: validan el comportamiento HTTP de extremo a extremo y de paso que el esquema migrado funciona. Hay 20 casos (creación, validaciones, 404, filtros, paginación, orden, edición y borrado).
- **Flujo de git:** una rama por tarea (`feat/…`, `test/…`, `fix/…`, `docs/…`) a partir de `main`, fusionada con `--no-ff` para conservar el historial por tarea. Commits en español, siguiendo Conventional Commits.

## Qué haría con más tiempo

- **Autenticación/autorización** y que el responsable sea un usuario real (entidad propia) y no un texto libre.
- **Concurrencia optimista** (token de versión) para que dos ediciones simultáneas no se pisen.
- **Ordenación configurable** (`sort=dueDate,-createdAt`), búsqueda por texto y filtro por rango de fechas.
- **Borrado lógico** y auditoría de cambios.
- **Validación estricta de `page`/`pageSize`:** hoy un valor no numérico (`?page=abc`) cae silenciosamente al valor por defecto fuera de Development; convendría devolver 400 como ya se hace con `status`.
- **Revisar avisos de seguridad de NuGet** (NU1902/NU1903) en dependencias transitivas (`SQLitePCLRaw`, `Microsoft.OpenApi`, `System.Security.Cryptography.Pkcs`) y fijar versiones corregidas.
- **Dockerfile y CI** (build + test en cada PR), logging estructurado, health checks y tests unitarios de las reglas de validación.
- Comparación de `assignee` con `ToLower()` impide usar el índice; con más volumen usaría una columna normalizada o una collation `NOCASE`.

## Cómo usé la IA

Trabajé con Claude Code (Claude Sonnet 5.5) dentro de VS Code, primero en modo plan y luego en implementación.

**Qué le pedí**
- Un plan para el proyecto a partir de los requisitos; aprobé el plan (con el remoto `https://github.com/edelkish/TodoTask.git` añadido) antes de que escribiera código.
- Implementarlo con reglas de proceso mías: **commits por tarea y no uno solo**, **mensajes en español y coherentes con lo implementado**, y **una rama por tarea desde `main`, fusionada de vuelta a `main`**. Esas dos últimas las fui añadiendo durante la ejecución y se aplicaron desde ese punto (el commit inicial del esqueleto quedó directamente en `main`).

- Una interfaz visual antes de hacer push: me propuso Scalar, Swagger UI o un panel propio; elegí **Scalar sobre el OpenAPI existente** por ser lo más barato. Lo implementó en tres tareas, cada una con su rama y commit.

**Qué aceptó/propuso y di por bueno**
- Arquitectura (Minimal APIs, DTOs, `ProblemDetails`, estado como texto, paginación con tope), la secuencia de tareas y la estrategia de tests de integración contra una base SQLite real.

**Qué se corrigió** (todo detectado ejecutando la API o los tests, no a ojo)
1. Un estado inválido en el cuerpo devolvía **500** en vez de 400: el manejador global capturaba el error de petición mal formada. Se corrigió para devolver 400.
2. Las fechas leídas de SQLite perdían la `Z` (zona UTC). Se añadieron convertidores.
3. **Bug de fondo:** fuera de Development, `?status=Foo` se **ignoraba en silencio** y se devolvían todas las tareas (en Development daba 400, lo que lo ocultó). Lo destapó un test que falló; se corrigió interpretando el estado a mano y se dejó el test como regresión.
4. Mi helper de tests enviaba `"status": null` y la API respondía 400 con razón; se corrigió el helper, no la API.
5. Detalles de herramientas: una versión con comodín en `dotnet add package` se colgó (se usó versión explícita), un servidor de prueba dejó el `.exe` bloqueado al recompilar (una vez más, y entonces las comprobaciones se hicieron con el binario viejo; se repitieron tras cerrarlo), una rama se creó antes de fusionar la anterior a `main` y hubo que reordenarla y un mensaje de error hablaba del "cuerpo" cuando el fallo era de la query.

**Lección:** el código generado compiló a la primera casi siempre, pero los fallos reales estaban en el comportamiento (códigos HTTP y entornos distintos). Por eso cada endpoint se probó en vivo con `curl` antes de hacer commit, y los tests se ejecutan contra la API real.
