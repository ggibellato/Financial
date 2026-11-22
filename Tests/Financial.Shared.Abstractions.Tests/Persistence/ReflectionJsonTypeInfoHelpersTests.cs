using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Financial.Shared.Abstractions.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Shared.Abstractions.Tests.Persistence;

[Trait("Category", "Unit")]
public class ReflectionJsonTypeInfoHelpersTests
{
    private sealed class PrivatelyBuilt
    {
        private PrivatelyBuilt()
        {
        }

        public string? Name { get; private set; }

        public int Count { get; private set; }

        public string Label => $"{Name}:{Count}";
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                typeInfo =>
                {
                    if (typeInfo.Kind != JsonTypeInfoKind.Object)
                    {
                        return;
                    }

                    ReflectionJsonTypeInfoHelpers.EnablePrivateConstructor(typeInfo.Type, typeInfo);
                    foreach (var property in typeInfo.Properties)
                    {
                        ReflectionJsonTypeInfoHelpers.WirePropertySetter(typeInfo.Type, property);
                    }
                }
            }
        }
    };

    [Fact]
    public void Deserialize_TypeWithPrivateConstructorAndPrivateSetters_FillsSettablePropertiesCaseInsensitivelyAndSkipsReadOnlyOnes()
    {
        var result = JsonSerializer.Deserialize<PrivatelyBuilt>("""{"name":"Mercado","count":3,"label":"ignored"}""", Options);

        using var _ = new AssertionScope();
        result!.Name.Should().Be("Mercado");
        result.Count.Should().Be(3);
    }

    [Fact]
    public void RemoveSetterlessProperties_KeepsPrivatelySettablePropertiesAndDropsComputedOnes()
    {
        var typeInfo = new DefaultJsonTypeInfoResolver().GetTypeInfo(typeof(PrivatelyBuilt), new JsonSerializerOptions());

        ReflectionJsonTypeInfoHelpers.RemoveSetterlessProperties(typeInfo);

        typeInfo.Properties.Select(property => property.Name)
            .Should().BeEquivalentTo(nameof(PrivatelyBuilt.Name), nameof(PrivatelyBuilt.Count));
    }

    [Fact]
    public void EnablePrivateConstructor_WhenAFactoryIsAlreadySet_KeepsIt()
    {
        var typeInfo = JsonTypeInfo.CreateJsonTypeInfo(typeof(PrivatelyBuilt), new JsonSerializerOptions());
        Func<object> existing = () => throw new InvalidOperationException();
        typeInfo.CreateObject = existing;

        ReflectionJsonTypeInfoHelpers.EnablePrivateConstructor(typeof(PrivatelyBuilt), typeInfo);

        typeInfo.CreateObject.Should().BeSameAs(existing);
    }
}
