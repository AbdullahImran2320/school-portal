// Controllers/SettingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        public SettingsController(SchoolPortalDbContext context) => _context = context;

        [HttpGet("challan")]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<ActionResult<ChallanSettingsDto>> GetChallanSettings()
        {
            var settings = await _context.ChallanSettings.FirstOrDefaultAsync();
            if (settings == null) return NotFound("Challan settings haven't been set up yet.");

            return Ok(new ChallanSettingsDto
            {
                AccountTitle = settings.AccountTitle,
                BankName = settings.BankName,
                AccountNumber = settings.AccountNumber,
                PaymentTermsLine1 = settings.PaymentTermsLine1,
                PaymentTermsLine2 = settings.PaymentTermsLine2
            });
        }

        [HttpPut("challan")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ChallanSettingsDto>> UpdateChallanSettings(UpdateChallanSettingsDto dto)
        {
            var settings = await _context.ChallanSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new ChallanSettings();
                _context.ChallanSettings.Add(settings);
            }

            settings.AccountTitle = dto.AccountTitle.Trim();
            settings.BankName = dto.BankName.Trim();
            settings.AccountNumber = dto.AccountNumber.Trim();
            settings.PaymentTermsLine1 = dto.PaymentTermsLine1.Trim();
            settings.PaymentTermsLine2 = dto.PaymentTermsLine2.Trim();

            await _context.SaveChangesAsync();

            return Ok(new ChallanSettingsDto
            {
                AccountTitle = settings.AccountTitle,
                BankName = settings.BankName,
                AccountNumber = settings.AccountNumber,
                PaymentTermsLine1 = settings.PaymentTermsLine1,
                PaymentTermsLine2 = settings.PaymentTermsLine2
            });
        }

        // The globally-editable parts of the roll number format — the
        // prefix letter(s) and how many digits the sequence pads to. The
        // rest of the format (each class's code, and each student's own
        // admission year) lives on the class/student rows themselves, not
        // here. Same singleton-row pattern as Challan Settings above.
        [HttpGet("roll-number")]
        [Authorize(Roles = "Admin,Accountant,Teacher")]
        public async Task<ActionResult<RollNumberSettingsDto>> GetRollNumberSettings()
        {
            var settings = await _context.RollNumberSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                // Created with defaults on first read, same as
                // StudentService.GetOrCreateRollNumberSettingsAsync — a
                // student can be created (and thus need these settings)
                // before the Admin ever visits this screen.
                settings = new RollNumberSettings();
                _context.RollNumberSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return Ok(new RollNumberSettingsDto { Prefix = settings.Prefix, SequenceDigits = settings.SequenceDigits });
        }

        [HttpPut("roll-number")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<RollNumberSettingsDto>> UpdateRollNumberSettings(UpdateRollNumberSettingsDto dto)
        {
            var settings = await _context.RollNumberSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new RollNumberSettings();
                _context.RollNumberSettings.Add(settings);
            }

            settings.Prefix = dto.Prefix.Trim();
            settings.SequenceDigits = dto.SequenceDigits;

            await _context.SaveChangesAsync();

            // Changing these does not retroactively reformat already-issued
            // roll numbers — same as changing ChallanSettings doesn't
            // reprint old challans. Existing students keep their current
            // codes; only newly generated/regenerated ones use the new
            // prefix/padding.
            return Ok(new RollNumberSettingsDto { Prefix = settings.Prefix, SequenceDigits = settings.SequenceDigits });
        }
    }
}
