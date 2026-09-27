using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>Base for controllers whose mutation endpoints return null from the Application layer to signal a bad request.</summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<T> OkOrBadRequest<T>(T? value) where T : class =>
        value is null ? BadRequest() : Ok(value);

    protected static bool TryParseOptionalCurrency(string? rawCurrency, out Currency? currency)
    {
        if (string.IsNullOrEmpty(rawCurrency))
        {
            currency = null;
            return true;
        }

        if (!EnumParser.TryParseEnum<Currency>(rawCurrency, out var parsed))
        {
            currency = null;
            return false;
        }

        currency = parsed;
        return true;
    }
}
