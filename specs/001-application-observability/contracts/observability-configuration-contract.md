# Contract: Observability Configuration

This is the configuration surface `Financial.Api`, `Financial.App`, and `docker-compose.yml` MUST agree on. It follows the existing `appsettings`/environment-variable convention already used for `Investment:Repository:Provider` / `CashFlow:Repository:Provider` (FR-013).

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
| `Observability:Enabled` | `bool` | always | Default `false`. Committed `appsettings.json` and `docker-compose.yml` MUST ship this as `false` (Constitution Principle VIII — deployable by default). |
| `Observability:Backend` | `"Jaeger" \| "Langfuse"` | `Enabled=true` | Case-insensitive enum bind; unrecognized value fails fast at startup (see data-model.md). |
| `Observability:Endpoint` | `string` (URL) | `Enabled=true` | OTLP endpoint. Local defaults: Jaeger `http://localhost:4317` (gRPC) or `:4318` (HTTP); Langfuse per its own OTLP ingestion path. |
| `Observability:Langfuse:PublicKey` / `:SecretKey` | `string` | `Enabled=true` and `Backend=Langfuse` | Combined into an OTLP Basic Auth header. Never logged, never exposed as a telemetry attribute. |

## Environment variable overrides

Same double-underscore convention already used in `docker-compose.yml` for `Investment__Repository__Provider`:

```
Observability__Enabled=true
Observability__Backend=Jaeger
Observability__Endpoint=http://jaeger:4317
```

## Consumers

- `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs` both call `services.AddFinancialObservability(configuration, serviceName: "Financial.Api" | "Financial.App")` — the single call site each composition root needs (mirrors the existing `AddGoogleDriveFileClient()` / `AddFinancialInfrastructure(configuration)` calls already present in both).
- `docker-compose.yml` (base file): no `Observability` environment overrides — the shipped default (`Enabled=false` from `appsettings.json`) applies, so the base compose file needs no new environment variables to remain deployable per Constitution Principle VIII.
- `docker-compose.observability.yml` (new, optional overlay): sets `Observability__Enabled=true` and `Observability__Backend=<Jaeger|Langfuse>` on the `app` service only when explicitly composed in via a Docker Compose profile.

## Backward/forward compatibility

Adding this section to `appsettings.json` is purely additive — it introduces a new, independent configuration key with a safe default and does not change the shape or meaning of any existing key (`Investment:*`, `CashFlow:*`, `Cors:*`, `Logging:*`, etc.).
