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

        // How many times to re-read and retry a payment if another request
        // changed the same ledger/charge row in between our read and write.
        // Three is generous for a small school office — this only ever loops
        // more than once if two people are genuinely recording a payment on
        // the exact same fee at nearly the exact same moment.
        private const int MaxConcurrencyAttempts = 3;

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
            for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
            {
                // AsNoTracking: this is a detached snapshot, re-read fresh on
                // every attempt. It is safe to mutate locally below (same as
                // the original logic did) because it is never saved directly —
                // persistence happens via the conditional ExecuteUpdateAsync
                // further down.
                var ledger = await _context.FeeLedgers.AsNoTracking()
                    .Include(l => l.Student)
                    .FirstOrDefaultAsync(l => l.LedgerId == ledgerId);
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

                // Snapshot of exactly what we read — used below to detect whether
                // someone else changed this row before we get to write.
                var originalPaidAmount = ledger.PaidAmount;
                var originalDiscountAmount = ledger.DiscountAmount;
                var originalManualFineAmount = ledger.ManualFineAmount;

                ledger.DiscountAmount += dto.DiscountAmount;
                if (dto.FineAmount.HasValue)
                    ledger.ManualFineAmount = dto.FineAmount.Value;

                var lateFee = FeeCalculator.GetLateFee(ledger, now, _gracePeriodDay, _lateFeeAmount);
                var effectiveDue = FeeCalculator.GetEffectiveDue(ledger, now, _gracePeriodDay, _lateFeeAmount);

                var newPaidAmount = originalPaidAmount + dto.AmountPaid;
                var newStatus = newPaidAmount >= effectiveDue
                    ? LedgerStatus.Paid
                    : newPaidAmount > 0
                        ? LedgerStatus.Partial
                        : LedgerStatus.Unpaid;

                await using var transaction = await _context.Database.BeginTransactionAsync();

                // Conditional update: only takes effect if PaidAmount/DiscountAmount/
                // ManualFineAmount still match what we read above. If another request
                // recorded a payment on this same ledger row in between, rowsAffected
                // comes back 0 and we loop to re-read the now-current data and retry —
                // instead of silently overwriting that other payment's total.
                var rowsAffected = await _context.FeeLedgers
                    .Where(l => l.LedgerId == ledgerId
                             && l.PaidAmount == originalPaidAmount
                             && l.DiscountAmount == originalDiscountAmount
                             && l.ManualFineAmount == originalManualFineAmount)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(l => l.PaidAmount, newPaidAmount)
                        .SetProperty(l => l.DiscountAmount, ledger.DiscountAmount)
                        .SetProperty(l => l.ManualFineAmount, ledger.ManualFineAmount)
                        .SetProperty(l => l.Status, newStatus));

                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync();
                    continue;
                }

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
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new PaymentResultDto
                {
                    PaymentId = payment.PaymentId,
                    ReceiptNumber = receiptNumber,
                    AmountPaid = dto.AmountPaid,
                    NewPaidTotal = newPaidAmount,
                    DueAmount = effectiveDue,
                    LateFeeCharged = lateFee,
                    Status = newStatus.ToString()
                };
            }

            throw new InvalidOperationException(
                "This fee record was updated by someone else at the same time. Please refresh and try again.");
        }

        public async Task<PaymentResultDto?> RecordChargePaymentAsync(int chargeId, RecordPaymentDto dto)
        {
            for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
            {
                var charge = await _context.StudentCharges.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ChargeId == chargeId);
                if (charge == null) return null;

                var outstandingBeforeDiscount = Math.Max(
                    (charge.DueAmount - charge.DiscountAmount) - charge.PaidAmount, 0);

                if (dto.DiscountAmount > outstandingBeforeDiscount)
                    throw new ArgumentException("Discount cannot be greater than the current outstanding amount.");

                if (dto.AmountPaid + dto.DiscountAmount > outstandingBeforeDiscount)
                    throw new ArgumentException("Payment plus discount cannot be greater than the current outstanding amount.");

                var originalPaidAmount = charge.PaidAmount;
                var originalDiscountAmount = charge.DiscountAmount;

                var newDiscountAmount = originalDiscountAmount + dto.DiscountAmount;
                // Net of discount, same meaning as DueAmount on the ledger-payment
                // response — this used to return the raw gross amount instead.
                var effectiveDue = charge.DueAmount - newDiscountAmount;
                var newPaidAmount = originalPaidAmount + dto.AmountPaid;
                var newStatus = newPaidAmount >= effectiveDue
                    ? ChargeStatus.Paid
                    : newPaidAmount > 0
                        ? ChargeStatus.Partial
                        : ChargeStatus.Unpaid;

                await using var transaction = await _context.Database.BeginTransactionAsync();

                // Same conditional-update / retry-on-conflict pattern as the ledger
                // payment above — see the comment there for why this exists.
                var rowsAffected = await _context.StudentCharges
                    .Where(c => c.ChargeId == chargeId
                             && c.PaidAmount == originalPaidAmount
                             && c.DiscountAmount == originalDiscountAmount)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(c => c.PaidAmount, newPaidAmount)
                        .SetProperty(c => c.DiscountAmount, newDiscountAmount)
                        .SetProperty(c => c.Status, newStatus));

                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync();
                    continue;
                }

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
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new PaymentResultDto
                {
                    PaymentId = payment.PaymentId,
                    ReceiptNumber = receiptNumber,
                    AmountPaid = dto.AmountPaid,
                    NewPaidTotal = newPaidAmount,
                    DueAmount = effectiveDue,
                    Status = newStatus.ToString()
                };
            }

            throw new InvalidOperationException(
                "This charge record was updated by someone else at the same time. Please refresh and try again.");
        }
    }
}
