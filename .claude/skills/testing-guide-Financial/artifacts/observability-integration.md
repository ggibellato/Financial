> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Observability Integration (`Integrations/Observability/*.cs`, `Financial.Shared.Abstractions/Observability/*.cs`)

## What to test

- `AddObservability(configuration, serviceName:)` binds `ObservabilityOptions` with defaults
  (`Enabled=false`, `Backend=Jaeger`, `Endpoint=http://localhost:4317`) and with explicit
  values (`Langfuse` keys).
- `ITelemetryTracer` resolves to the no-op when disabled and to `OpenTelemetryTracer` when
  enabled; spans never throw either way (`ObservabilityDisabledTests.TelemetryTracer_ResolvesToUsableNoOp_WithObservabilityDisabled`).
- `OtlpExporterSettingsResolver`: endpoint/headers per backend; invalid backend name rejected
  (`BackendConfigurationTests`).
- `SerilogObservabilityExtensions.WriteToObservability`: sink added only when enabled.
- **Unreachable collector** (negative path, Integration through the host): with
  `Observability:Enabled=true` and an endpoint nobody listens on (`http://localhost:4319`),
  every endpoint still returns 200 and span creation never throws
  (`ObservabilityBackendUnreachableTests`).
- **Span convention on services**: covered per service in `application-services.md` with
  `RecordingTelemetryTracer`; the shared helpers `StartServiceSpan` / `MarkSuccess` /
  `MarkFailed` (`TelemetryTracerExtensions`, `TelemetrySpanExtensions`) get their own Unit test
  for attribute names (`TelemetryAttributeKeys`).
- **Trace correlation** (`EndToEndTraceTests` — in-process despite the name): one HTTP request
  → controller activity + service span + storage span share one `AmbientTraceId`.

## Layer assignment

- **Integration** for DI/options binding (real container), for the unreachable-collector host
  test (`ApiTestFactory().WithWebHostBuilder(b => b.UseSetting("Observability:Enabled", "true") …)`),
  and for trace correlation. The OTLP collector is an external provider and is never started in
  tests; the exporter's background retries are what the unreachable test proves harmless.
- **Unit** for `OtlpExporterSettingsResolver`, extension helpers, `NoOpTelemetryTracer`.
- Architecture rule: `ObservabilityIsolationRuleTests` pins that only composition roots and
  `Financial.Shared.Abstractions` consumers touch OpenTelemetry (`architecture-rule-tests.md`).
- No E2E; the smoke job runs with observability disabled.

## Setup pattern

```csharp
using Financial.Integrations.Observability;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[Fact]
public void AddObservability_BindsObservabilityOptionsFromConfiguration_WithDefaults()
{
    var provider = BuildServiceProvider(new Dictionary<string, string?>());

    var options = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

    options.Enabled.Should().BeFalse();
    options.Backend.Should().Be(ObservabilityBackend.Jaeger);
    options.Endpoint.Should().Be("http://localhost:4317");
}
```

(From `Tests/Financial.Observability.Tests/ObservabilityServiceCollectionExtensionsTests.cs`;
`BuildServiceProvider` is that file's in-memory-configuration helper.) For the host-level
negative test use `UseSetting`, not `ConfigureAppConfiguration` — `AddObservability` reads its
options inline while `Program.cs` executes, and factory configuration callbacks arrive too late
(comment in `ObservabilityBackendUnreachableTests.CreateFactory`). Pick a port other than 4317
so a real local Jaeger cannot make the test pass by accident.

## When to skip

- The OpenTelemetry SDK's exporter internals.
- Asserting exact span *timing* or exporter batch sizes.

## Examples from project

- `Tests/Financial.Observability.Tests/ObservabilityServiceCollectionExtensionsTests.cs`, `BackendConfigurationTests.cs`, `OtlpExporterSettingsResolverTests.cs`, `SerilogObservabilityExtensionsTests.cs` — Integration/Unit.
- `Tests/Financial.Api.Tests/ObservabilityDisabledTests.cs`, `ObservabilityBackendUnreachableTests.cs` — Integration through the host.
- `Tests/Financial.Api.Tests/EndToEndTraceTests.cs` — Integration (single process; the class name predates this vocabulary).
- `Tests/Financial.Presentation.Tests/DependencyInjection/ObservabilityServiceRegistrationTests.cs` — Integration; WPF composition root.
