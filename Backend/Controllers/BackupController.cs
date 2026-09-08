// Controllers/BackupController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.API.Services;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/backups")]
    [Authorize(Roles = "Admin")]
    public class BackupController : ControllerBase
    {
        private readonly IBackupService _backupService;
        public BackupController(IBackupService backupService) => _backupService = backupService;

        [HttpGet]
        public ActionResult<List<BackupInfo>> List() => Ok(_backupService.ListBackups());

        [HttpPost]
        public async Task<ActionResult<BackupInfo>> CreateNow()
        {
            var info = await _backupService.CreateBackupNowAsync();
            return Ok(info);
        }

        [HttpGet("{fileName}/download")]
        public IActionResult Download(string fileName)
        {
            var path = _backupService.GetBackupFilePath(fileName);
            if (path == null) return NotFound();

            return PhysicalFile(path, "application/octet-stream", fileName);
        }
    }
}
