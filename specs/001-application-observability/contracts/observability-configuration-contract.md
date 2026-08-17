# Contract: Observability Configuration

The configuration surface `Financial.Api`, `Financial.App`, and `docker-compose.yml` MUST agree on, read exclusively by `Integrations/Observability` (Decision D8 — no other project reads these keys, including `Financial.Shared.Abstractions`, which stays pure contracts). Follows the existing `appsettings`/environment-variable convention already used for `Investment:Repository:Provider` / `CashFlow:Repository:Provider` (FR-013).

## Schema (`appsettings.json`)

```json
{
  "Observability": {
    "Enabled": false,
    "Backend": "Jaeger",
    "Endpoint": "http://localhost:4317",
    "Langfuse": {
      "PublicKey": "",
      "SecretKey": ""
    }
  }
}
```

| Key | Type | Required when | Notes |
|---|---|---|---|
| `Observability:Enabled` | `bool` | always | Default `false`. Committed `appsettings.json` and `docker-compose.yml` MUST ship this as `false` (Constitution Principle VIII). |
| `Observability:Backend` | `"Jaeger" \| "Langfuse"` | `Enabled=true` | Case-insensitive enum bind; unrecognized value fails fast at startup. |
| `Observability:Endpoint` | `string` (URL) | `Enabled=true` | OTLP endpoint. Local defaults: Jaeger `http://localhost:4317` (gRPC); Langfuse per its own OTLP ingestion path. |
| `Observability:Langfuse:PublicKey` / `:SecretKey` | `string` | `Enabled=true` and `Backend=Langfuse` | Combined into an OTLP Basic Auth header inside `Integrations/Observability`. Never logged, never a telemetry attribute. |

## Environment variable overrides

Same double-underscore convention already used in `docker-compose.yml`:

```
Observability__Enabled=true
Observability__Backend=Jaeger
Observability__Endpoint=http://jaeger:4317
```

## Consumers

- `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs` each call **one** method: `services.AddObservability(configuration, serviceName: "Financial.Api" | "Financial.App")` — defined in `Integrations/Observability`, referenced the same way `AddGoogleDriveFileClient()` already is. Neither composition root reads `Observability:*` keys itself.
- `docker-compose.yml` (base file): no `Observability` environment overrides — the shipped default (`Enabled=false`) applies.
- `docker-compose.observability.yml` (new, optional overlay): sets `Observability__Enabled=true` and `Observability__Backend=<Jaeger|Langfuse>` on the `app` service only when explicitly composed in via a Docker Compose profile.

## Backward/forward compatibility

Purely additive — a new, independent configuration key with a safe default, no change to any existing key's shape or meaning.
