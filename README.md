# Babbelbos

Babbelbos (werknaam) is een veilige gesprekspartner voor jonge kinderen en voor mensen met een beperking met een vergelijkbare ontwikkelingsleeftijd. Een deelnemer praat met een eigen kabouter die eerdere gesprekken onthoudt. Ouders, voogden en begeleiders richten deelnemers in, kunnen gesprekken (beperkt) inzien en krijgen een melding bij zorgsignalen.

Het is ook een leerproject: DDD, CQRS, Clean Architecture, SOLID, containers, Kubernetes en CI/CD, helemaal tot in productie.

## Documentatie

- [Application design](docs/01-application-design.md)
- [System prompt van de kabouter](docs/02-kabouter-system-prompt.md)
- [Architectuurbeslissingen (ADR)](docs/adr/)
- [CLAUDE.md](CLAUDE.md): regels voor AI-codeerassistenten (en een goede samenvatting voor mensen)

## Aan de slag

Nodig: [.NET 10 SDK](https://dotnet.microsoft.com/download), Docker Desktop (voor PostgreSQL via Aspire) en later Ollama.

```bash
dotnet build Babbelbos.slnx
dotnet test --solution Babbelbos.slnx
dotnet run --project src/Babbelbos.AppHost     # opent het Aspire-dashboard
```

De API heeft `/health/live` en `/health/ready` voor Kubernetes-probes.

## Structuur

```
src/
  Babbelbos.Api/             ASP.NET Core host (endpoints, Wolverine, compositie)
  Babbelbos.AppHost/         Aspire: lokaal alles starten (API + PostgreSQL)
  Babbelbos.ServiceDefaults/ OpenTelemetry, health checks, resilience
  BuildingBlocks/            Gedeelde basis: AggregateRoot, Result, ITenantContext
  Modules/<Module>/          Per bounded context: Domain, Application, Infrastructure, Contracts
tests/
  Architecture.Tests/        Bewaakt de laagregels per module
  ...
docs/                        Design, prompt, ADR's
```
