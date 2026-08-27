using SchoolPortal.API.DTOs;

namespace SchoolPortal.API.Services
{
    public interface IPaymentService
    {
        Task<PaymentResultDto?> RecordLedgerPaymentAsync(int ledgerId, RecordPaymentDto dto);
        Task<PaymentResultDto?> RecordChargePaymentAsync(int chargeId, RecordPaymentDto dto);
    }
}
