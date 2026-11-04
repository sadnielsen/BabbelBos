# 0001 — Start als modulaire monoliet

- Status: aangenomen
- Datum: 2026-11-01

## Context

Babbelbos heeft meerdere bounded contexts (Conversation, Safeguarding, Household, …) en moet later kunnen schalen. Het is ook een leerproject met één ontwikkelaar. Microservices vanaf dag één geven veel operationele last (meerdere deployments, netwerkfouten, gedistribueerde transacties) voordat er één feature werkt.

## Besluit

We starten met één deploybare API (`Babbelbos.Api`) waarin elke bounded context een eigen module is onder `src/Modules/<Module>/`, met de lagen Domain, Application, Infrastructure en Contracts. Modules praten alleen via `Contracts` en via berichten (Wolverine). Elke module krijgt een eigen database-schema. De laagregels worden afgedwongen met `tests/Architecture.Tests`.

## Gevolgen

- Eenvoudig te bouwen, testen en deployen; één pipeline.
- Modulegrenzen blijven zichtbaar en getest, zodat een module later een losse service kan worden.
- Discipline nodig: geen gedeelde tabellen of directe aanroepen tussen modules. De architectuurtests vangen het grootste deel op.
