using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Services;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize(Roles = "Admin,Accountant")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        public PaymentsController(IPaymentService paymentService) => _paymentService = paymentService;

        [HttpPost("ledger/{ledgerId}")]
        public async Task<ActionResult<PaymentResultDto>> PayLedger(int ledgerId, RecordPaymentDto dto)
        {
            try
            {
                var result = await _paymentService.RecordLedgerPaymentAsync(ledgerId, dto);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("charge/{chargeId}")]
        public async Task<ActionResult<PaymentResultDto>> PayCharge(int chargeId, RecordPaymentDto dto)
        {
            try
            {
                var result = await _paymentService.RecordChargePaymentAsync(chargeId, dto);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}