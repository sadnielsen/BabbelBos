# 0002 — Taalmodel achter een eigen port, met Microsoft.Extensions.AI als adapter

- Status: aangenomen
- Datum: 2026-11-01

## Context

De LLM moet uitwisselbaar zijn (OpenAI nu, misschien later Azure OpenAI in de EU, Mistral of lokaal Ollama). Tests mogen geen echte LLM aanroepen. En de veiligheid mag niet afhangen van één aanbieder.

## Besluit

- De applicatielaag kent alleen `IChatCompletionService` met eigen typen (`ChatRequest`, `ChatTurn`, `ChatCompletion`). Geen SDK-typen buiten Infrastructure; de architectuurtests bewaken dat.
- In Infrastructure vertaalt één adapter dit naar `Microsoft.Extensions.AI.IChatClient`. OpenAI en Ollama zijn daarmee alleen configuratie (`Llm:Provider`). Een nep-implementatie is de standaard: zonder instellingen gaat er geen data naar buiten.
- Telemetrie (OpenTelemetry) zonder gevoelige data: geen prompts of antwoorden in traces.
- Veiligheid zit vóór en na de LLM, los van de aanbieder: trefwoordregels op de invoer, vaste scripts bij High/Critical (de LLM wordt dan niet aangeroepen), controle op vorm en inhoud van het antwoord, en bij elke technische fout het vaste fallback-antwoord.

## Gevolgen

- Een nieuwe aanbieder is een nieuwe `case` in de registratie, geen wijziging in de applicatielaag.
- Prompt-evals (later) kunnen dezelfde port gebruiken om modellen te vergelijken.
- Kenmerken die alleen één aanbieder heeft (bijv. eigen moderatie-API) komen achter een eigen port, niet in de chat-port.
