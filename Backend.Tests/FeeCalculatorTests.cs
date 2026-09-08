// FeeCalculatorTests.cs
using SchoolPortal.API.Models;
using SchoolPortal.API.Services;
using Xunit;

namespace Backend.Tests
{
    public class FeeCalculatorTests
    {
        private static FeeLedger Ledger(LedgerStatus status = LedgerStatus.Unpaid, decimal? manualFine = null,
            decimal dueAmount = 5000, decimal discount = 0, int month = 6, int year = 2026)
        {
            return new FeeLedger
            {
                MonthNumber = month,
                Year = year,
                DueAmount = dueAmount,
                DiscountAmount = discount,
                Status = status,
                ManualFineAmount = manualFine
            };
        }

        // Grace day 10 means June's fee is due by June 10th — the 10th itself
        // is still on time, the 11th is the first overdue day.
        [Fact]
        public void GetLateFee_ReturnsZero_OnTheDueDateItself()
        {
            var ledger = Ledger();
            var now = new DateTime(2026, 6, 10);

            var fee = FeeCalculator.GetLateFee(ledger, now, gracePeriodDay: 10, lateFeeAmount: 200);

            Assert.Equal(0, fee);
        }

        [Fact]
        public void GetLateFee_AppliesFee_TheDayAfterDueDate()
        {
            var ledger = Ledger();
            var now = new DateTime(2026, 6, 11);

            var fee = FeeCalculator.GetLateFee(ledger, now, gracePeriodDay: 10, lateFeeAmount: 200);

            Assert.Equal(200, fee);
        }

        [Fact]
        public void GetLateFee_ReturnsZero_WhenLedgerAlreadyPaid_EvenIfPastDueDate()
        {
            var ledger = Ledger(status: LedgerStatus.Paid);
            var now = new DateTime(2026, 6, 25);

            var fee = FeeCalculator.GetLateFee(ledger, now, gracePeriodDay: 10, lateFeeAmount: 200);

            Assert.Equal(0, fee);
        }

        [Fact]
        public void GetLateFee_UsesManualFineAmount_InsteadOfConfiguredAmount_WhenSet()
        {
            var ledger = Ledger(manualFine: 750);
            var now = new DateTime(2026, 6, 25); // well past due

            var fee = FeeCalculator.GetLateFee(ledger, now, gracePeriodDay: 10, lateFeeAmount: 200);

            Assert.Equal(750, fee);
        }

        [Fact]
        public void GetLateFee_ManualFineOfZero_OverridesAutomaticFee_EvenWhenOverdue()
        {
            // A manual fine explicitly set to 0 means "waived", not "not set" —
            // this only works because ManualFineAmount is nullable and GetLateFee
            // checks HasValue rather than checking for a non-zero amount.
            var ledger = Ledger(manualFine: 0);
            var now = new DateTime(2026, 6, 25);

            var fee = FeeCalculator.GetLateFee(ledger, now, gracePeriodDay: 10, lateFeeAmount: 200);

            Assert.Equal(0, fee);
        }

        [Fact]
        public void GetEffectiveDue_SubtractsDiscountAndAddsLateFee()
        {
            var ledger = Ledger(dueAmount: 5000, discount: 500);
            var now = new DateTime(2026, 6, 25); // overdue -> +200

            var due = FeeCalculator.GetEffectiveDue(ledger, now, gracePeriodDay: 10, lateFeeAmount: 200);

            Assert.Equal(5000 - 500 + 200, due);
        }

        [Fact]
        public void IsApplicableMonth_False_ForMonthBeforeAdmission()
        {
            var admissionDate = new DateTime(2026, 6, 1);

            var applicable = FeeCalculator.IsApplicableMonth(admissionDate, ledgerMonth: 5, ledgerYear: 2026);

            Assert.False(applicable);
        }

        [Fact]
        public void IsApplicableMonth_True_ForAdmissionMonthItself()
        {
            var admissionDate = new DateTime(2026, 6, 15); // admitted mid-June

            var applicable = FeeCalculator.IsApplicableMonth(admissionDate, ledgerMonth: 6, ledgerYear: 2026);

            Assert.True(applicable); // still owes June's fee even though admitted on the 15th
        }

        [Fact]
        public void GetEffectiveStatus_ReturnsPaid_RegardlessOfDueDate()
        {
            var ledger = Ledger(status: LedgerStatus.Paid);
            var now = new DateTime(2026, 6, 25);

            var status = FeeCalculator.GetEffectiveStatus(ledger, now, gracePeriodDay: 10);

            Assert.Equal("Paid", status);
        }

        [Fact]
        public void GetEffectiveStatus_ReturnsOverdue_WhenPartiallyPaidPastDueDate()
        {
            var ledger = Ledger(status: LedgerStatus.Partial);
            var now = new DateTime(2026, 6, 25);

            var status = FeeCalculator.GetEffectiveStatus(ledger, now, gracePeriodDay: 10);

            Assert.Equal("Overdue", status);
        }
    }
}
