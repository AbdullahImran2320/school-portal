// Services/IPaymentService.cs + PaymentService.cs
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services
{
 

    

    public class PaymentService : IPaymentService
    {
        private readonly SchoolPortalDbContext _context;
        private readonly int _gracePeriodDay;
        private readonly decimal _lateFeeAmount;
        public PaymentService(SchoolPortalDbContext context, IConfiguration config)
        {
            _context = context;
            _gracePeriodDay = config.GetValue<int>("LateFeeSettings:GracePeriodDay");
            _lateFeeAmount = config.GetValue<decimal>("LateFeeSettings:LateFeeAmount");
        }

        private string GenerateReceiptNumber() =>
            $"RCPT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        public async Task<PaymentResultDto?> RecordLedgerPaymentAsync(int ledgerId, RecordPaymentDto dto)
        {
            var ledger = await _context.FeeLedgers.Include(l => l.Student).FirstOrDefaultAsync(l => l.LedgerId == ledgerId);
            if (ledger == null) return null;

            var now = DateTime.Now;
            if (!FeeCalculator.IsApplicableMonth(ledger.Student?.AdmissionDate ?? now, ledger.MonthNumber, ledger.Year))
                return null;

            var automaticOrExistingFine = FeeCalculator.GetLateFee(ledger, now, _gracePeriodDay, _lateFeeAmount);
            // Null means keep/use the automatic existing fine; a supplied value is
            // an explicit per-fee override (including 0). Validate against the
            // final fine, not against the old automatic value.
            var selectedFine = dto.FineAmount ?? automaticOrExistingFine;
            var outstandingBeforeDiscount = Math.Max(
                (ledger.DueAmount - ledger.DiscountAmount + selectedFine) - ledger.PaidAmount, 0);

            if (dto.DiscountAmount > outstandingBeforeDiscount)
                throw new ArgumentException("Discount cannot be greater than the current outstanding amount.");

            if (dto.AmountPaid + dto.DiscountAmount > outstandingBeforeDiscount)
                throw new ArgumentException("Payment plus discount cannot be greater than the current outstanding amount.");

            ledger.DiscountAmount += dto.DiscountAmount;
            if (dto.FineAmount.HasValue)
                ledger.ManualFineAmount = dto.FineAmount.Value;

            var lateFee = FeeCalculator.GetLateFee(ledger, now, _gracePeriodDay, _lateFeeAmount);

            var receiptNumber = GenerateReceiptNumber();
            var payment = new Payment
            {
                LedgerId = ledgerId,
                AmountPaid = dto.AmountPaid,
                PaymentMethod = dto.PaymentMethod,
                CollectedBy = dto.CollectedBy,
                ReceiptNumber = receiptNumber
            };
            _context.Payments.Add(payment);

            var effectiveDue = FeeCalculator.GetEffectiveDue(ledger, now, _gracePeriodDay, _lateFeeAmount);

            ledger.PaidAmount += dto.AmountPaid;
            ledger.Status = ledger.PaidAmount >= effectiveDue
                ? LedgerStatus.Paid
                : ledger.PaidAmount > 0
                    ? LedgerStatus.Partial
                    : LedgerStatus.Unpaid;

            await _context.SaveChangesAsync();

            return new PaymentResultDto
            {
                PaymentId = payment.PaymentId,
                ReceiptNumber = receiptNumber,
                AmountPaid = dto.AmountPaid,
                NewPaidTotal = ledger.PaidAmount,
                DueAmount = effectiveDue,
                LateFeeCharged = lateFee,
                Status = ledger.Status.ToString()
            };
        }

        public async Task<PaymentResultDto?> RecordChargePaymentAsync(int chargeId, RecordPaymentDto dto)
        {
            var charge = await _context.StudentCharges.FindAsync(chargeId);
            if (charge == null) return null;

            var outstandingBeforeDiscount = Math.Max(
                (charge.DueAmount - charge.DiscountAmount) - charge.PaidAmount, 0);

            if (dto.DiscountAmount > outstandingBeforeDiscount)
                throw new ArgumentException("Discount cannot be greater than the current outstanding amount.");

            if (dto.AmountPaid + dto.DiscountAmount > outstandingBeforeDiscount)
                throw new ArgumentException("Payment plus discount cannot be greater than the current outstanding amount.");

            charge.DiscountAmount += dto.DiscountAmount;

            var receiptNumber = GenerateReceiptNumber();
            var payment = new Payment
            {
                ChargeId = chargeId,
                AmountPaid = dto.AmountPaid,
                PaymentMethod = dto.PaymentMethod,
                CollectedBy = dto.CollectedBy,
                ReceiptNumber = receiptNumber
            };
            _context.Payments.Add(payment);

            var effectiveDue = charge.DueAmount - charge.DiscountAmount;
            charge.PaidAmount += dto.AmountPaid;
            charge.Status = charge.PaidAmount >= effectiveDue
                ? ChargeStatus.Paid
                : charge.PaidAmount > 0
                    ? ChargeStatus.Partial
                    : ChargeStatus.Unpaid;
            await _context.SaveChangesAsync();

            return new PaymentResultDto
            {
                PaymentId = payment.PaymentId,
                ReceiptNumber = receiptNumber,
                AmountPaid = dto.AmountPaid,
                NewPaidTotal = charge.PaidAmount,
                // Net of discount, same meaning as DueAmount on the ledger-payment
                // response — this used to return the raw gross amount instead.
                DueAmount = effectiveDue,
                Status = charge.Status.ToString()
            };
        }
    }
}