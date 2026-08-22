namespace Financial.Shared.Infrastructure.Configuration;

public static class RepositoryProviderResolver
{
    public static TEnum Resolve<TEnum>(string? providerValue, TEnum defaultValue) where TEnum : struct, Enum
    {
        var value = providerValue ?? defaultValue.ToString();

        if (!Enum.TryParse(value, ignoreCase: true, out TEnum provider))
        {
            throw new InvalidOperationException(
                $"Repository provider '{value}' is not supported. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        }

        return provider;
    }
}
