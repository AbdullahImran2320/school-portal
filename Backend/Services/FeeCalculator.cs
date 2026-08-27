// Services/FeeCalculator.cs
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services
{
    public static class FeeCalculator
    {
        // A monthly fee only exists from the student's admission month onward.
        // This also lets older databases safely hide any legacy Jan-Dec ledgers
        // that may have been generated before admission-date billing was fixed.
        public static bool IsApplicableMonth(DateTime admissionDate, int ledgerMonth, int ledgerYear)
        {
            return new DateTime(ledgerYear, ledgerMonth, 1) >=
                   new DateTime(admissionDate.Year, admissionDate.Month, 1);
        }

        // A month's due date is the grace day within that month — e.g.
        // August's fee is due by August 10th, not "sometime after August ends."
        // This is a deliberate change from the earlier month-has-fully-passed
        // rule, which was too lenient — a real school wants the late fee to
        // kick in mid-month, not wait for the month to be completely over.
        public static bool IsOverdue(int ledgerMonth, int ledgerYear, DateTime now, int gracePeriodDay)
        {
            var dueDate = new DateTime(ledgerYear, ledgerMonth, 1).AddDays(gracePeriodDay - 1);
            return now.Date > dueDate;
        }

        public static string GetEffectiveStatus(FeeLedger ledger, DateTime now, int gracePeriodDay)
        {
            if (ledger.Status == LedgerStatus.Paid) return "Paid";
            bool overdue = IsOverdue(ledger.MonthNumber, ledger.Year, now, gracePeriodDay);
            if (ledger.Status == LedgerStatus.Partial) return overdue ? "Overdue" : "Partial";
            return overdue ? "Overdue" : "Unpaid";
        }

        public static decimal GetLateFee(FeeLedger ledger, DateTime now, int gracePeriodDay, decimal lateFeeAmount)
        {
            if (ledger.Status == LedgerStatus.Paid) return 0;
            if (ledger.ManualFineAmount.HasValue) return ledger.ManualFineAmount.Value;
            return IsOverdue(ledger.MonthNumber, ledger.Year, now, gracePeriodDay) ? lateFeeAmount : 0;
        }

        // What the student actually owes right now for this month:
        // component due, minus any standing discount, plus a late fee if applicable.
        public static decimal GetEffectiveDue(FeeLedger ledger, DateTime now, int gracePeriodDay, decimal lateFeeAmount)
        {
            var lateFee = GetLateFee(ledger, now, gracePeriodDay, lateFeeAmount);
            return ledger.DueAmount - ledger.DiscountAmount + lateFee;
        }
    }
}