# Weather Service

Weather Service is a .NET 8 solution for weather data, forecasts, and live browser alerts with SignalR.

## Dependency Direction

The current solution follows this dependency direction:

```text
Weather.AppHost -> Weather.Api
Weather.Api -> Weather.Application, Weather.Infrastructure, Weather.Seeder
Weather.Seeder -> Weather.Infrastructure
Weather.Application -> Weather.Domain
Weather.Infrastructure -> Weather.Domain
```

## Layers

- `Weather.Domain`
  Holds the core entities, shared contracts, and repository/notification abstractions. It is the innermost layer and depends on nothing.

- `Weather.Application`
  Holds CQRS use cases and MediatR handlers. It depends only on `Weather.Domain`.

- `Weather.Infrastructure`
  Implements repositories and persistence with EF Core and PostgreSQL. It depends only on `Weather.Domain`.

- `Weather.Seeder`
  Imports weather data from the external Singapore source and stores it through infrastructure. It sits outside the core business layers.

- `Weather.Api`
  Contains the presentation layer and also acts as the executable composition root. It hosts controllers, SignalR, Swagger, migrations, startup seeding, and background polling.

- `Weather.AppHost`
  Uses .NET Aspire to orchestrate the local environment. It starts PostgreSQL and the API project.

## CQRS

CQRS is implemented in `Weather.Application` with MediatR.

- Commands change state, such as subscribing to alerts or evaluating alerts.
- Queries read data, such as weather, forecasts, history, and subscriptions.

## SignalR

Live alerts are pushed from the API through SignalR after alert evaluation creates a new triggered alert. The application layer stays decoupled from SignalR through `IAlertNotifier`, which lives in `Weather.Domain`, is implemented in the API layer, and is wired in the host.

## Architecture Note

Keeping everything inside `Weather.Api` avoids an extra host project, but it also means the API project serves as both the presentation layer and the composition root. That is practical, though less strict than separating the executable host from the API layer.

## Run

Use `Weather.AppHost` for local orchestration, or build from the CLI:

```bash
dotnet build Weather.AppHost/Weather.sln --configuration Release
```
