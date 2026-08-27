using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Services;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize(Roles = "Admin,Accountant,Teacher")]
    public class ResultsController : ControllerBase
    {
        private readonly IResultService _resultService;
        public ResultsController(IResultService resultService) => _resultService = resultService;

        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost("results")]
        public async Task<ActionResult<ResultDto>> Record(RecordResultDto dto)
        {
            return Ok(await _resultService.RecordResultAsync(dto));
        }

        [HttpGet("students/{studentId}/report-card/{examId}")]
        public async Task<ActionResult<ReportCardDto>> GetReportCard(int studentId, int examId)
        {
            var card = await _resultService.GetReportCardAsync(studentId, examId);
            if (card == null) return NotFound();
            return Ok(card);
        }
    }
}