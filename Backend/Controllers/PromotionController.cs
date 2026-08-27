using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Services;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/promotion")]
    [Authorize(Roles = "Admin")]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;
        public PromotionController(IPromotionService promotionService) => _promotionService = promotionService;

        [HttpPost("promote-year")]
        public async Task<ActionResult<PromotionResultDto>> PromoteYear(PromoteClassesDto dto)
        {
            var result = await _promotionService.PromoteAllAsync(dto);
            return Ok(result);
        }
    }
}