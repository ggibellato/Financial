using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Exposes the aggregated list of Mensais bills and credit card invoices due within the next 5 days,
/// for the Web and WPF startup payment-due banners.
/// </summary>
[ApiController]
[Route("payments-due")]
public sealed class PaymentsDueController : ControllerBase
{
    private readonly IPaymentsDueService _paymentsDueService;

    public PaymentsDueController(IPaymentsDueService paymentsDueService)
    {
        _paymentsDueService = paymentsDueService ?? throw new ArgumentNullException(nameof(paymentsDueService));
    }

    /// <summary>Lists Mensais bills and credit card invoices due within the next 5 days.</summary>
    /// <returns>200 OK with the list of imminent payments (empty if none qualify).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentDueDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PaymentDueDTO>> GetPaymentsDue()
    {
        return Ok(_paymentsDueService.GetPaymentsDue());
    }
}
