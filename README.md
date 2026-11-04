# Babbelbos

Babbelbos (werknaam) is een veilige gesprekspartner voor jonge kinderen en voor mensen met een beperking met een vergelijkbare ontwikkelingsleeftijd. Een deelnemer praat met een eigen kabouter die eerdere gesprekken onthoudt. Ouders, voogden en begeleiders richten deelnemers in, kunnen gesprekken (beperkt) inzien en krijgen een melding bij zorgsignalen.

Het is ook een leerproject: DDD, CQRS, Clean Architecture, SOLID, containers, Kubernetes en CI/CD, helemaal tot in productie.

## Documentatie

- [Application design](docs/01-application-design.md)
- [System prompt van de kabouter](docs/02-kabouter-system-prompt.md)
- [Prompts](prompts/README.md)
- [Architectuurbeslissingen (ADR)](docs/adr/)
- [CLAUDE.md](CLAUDE.md): regels voor AI-codeerassistenten (en een goede samenvatting voor mensen)

## Aan de slag

Nodig: [.NET 10 SDK](https://dotnet.microsoft.com/download) en Docker Desktop (voor PostgreSQL via Aspire en voor de integratietests).

```bash
dotnet build Babbelbos.slnx
dotnet test --solution Babbelbos.slnx
dotnet run --project src/Babbelbos.AppHost     # opent het Aspire-dashboard met de API en PostgreSQL
```

Probeer de API daarna met [`src/Babbelbos.Api/Babbelbos.Api.http`](src/Babbelbos.Api/Babbelbos.Api.http). Zonder verdere instellingen antwoordt een nep-LLM met steeds dezelfde vraag; het gesprek, de veiligheidscheck en de opslag werken wel echt.

### Een echt taalmodel gebruiken

De API-key staat nooit in de repo, alleen in je lokale user-secrets:

```bash
dotnet user-secrets set "Llm:Provider" "OpenAI" --project src/Babbelbos.Api
dotnet user-secrets set "Llm:OpenAI:ApiKey" "sk-..." --project src/Babbelbos.Api
dotnet user-secrets set "Llm:OpenAI:Model" "gpt-4.1-mini" --project src/Babbelbos.Api   # optioneel
```

Of lokaal met [Ollama](https://ollama.com): `Llm:Provider` = `Ollama`, `Llm:Ollama:Model` = bijvoorbeeld `qwen2.5:7b`.

### Wie praat er?

Inloggen bestaat nog niet (iteratie 4). Tot die tijd komen de tenant en de deelnemer uit `src/Babbelbos.Api/appsettings.Development.json`: een verzonnen deelnemer "Sam". Zet daar nooit echte gegevens in.

De API heeft `/health/live` en `/health/ready` voor Kubernetes-probes.

## Structuur

```
src/
  Babbelbos.Api/             ASP.NET Core host (endpoints, Wolverine, compositie)
  Babbelbos.AppHost/         Aspire: lokaal alles starten (API + PostgreSQL)
  Babbelbos.ServiceDefaults/ OpenTelemetry, health checks, resilience
  BuildingBlocks/            Gedeelde basis: AggregateRoot, Result, TenantId, ITenantContext
  Modules/<Module>/          Per bounded context: Domain, Application, Infrastructure, Contracts
    Conversations/           Gesprekken, berichten, antwoord van de kabouter, veiligheidscheck
    GnomeCatalog/            De kabouters, gelezen uit prompts/
prompts/                     Persona's, veiligheidsregels en vaste veiligheidsscripts (geversioneerd)
tests/
  Architecture.Tests/        Bewaakt de laagregels per module
  ...
docs/                        Design, prompt, ADR's
```
