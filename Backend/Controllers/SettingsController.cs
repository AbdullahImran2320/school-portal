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
    }
}
