using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serwer.Models;
using Serwer.Repositories;
using Serwer.Services;

namespace Serwer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FilesController : Controller
    {
        private readonly string basePath = "D:\\dev\\source\\repos\\Serwer\\Serwer\\NetworkDrive";
        private readonly FileRepository _fileRepository;
        private readonly UserRepository _userRepository;
        private readonly TaskQueue _taskQueue;

        public FilesController(FileRepository fileRepository, UserRepository userRepository, TaskQueue taskQueue)
        {
            _fileRepository = fileRepository;
            _userRepository = userRepository;
            _taskQueue = taskQueue;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var username = User.Identity.Name;
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var filePath = Path.Combine(basePath, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileRecord = new FileRecord
            {
                FileName = file.FileName,
                FilePath = filePath,
                UserId = user.Id
            };
            await _fileRepository.AddFileAsync(fileRecord);

            return Ok();
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> Download(int id)
        {
            var fileRecord = await _fileRepository.GetFileByIdAsync(id);
            if (fileRecord == null || !System.IO.File.Exists(fileRecord.FilePath))
            {
                return NotFound();
            }
            var fileBytes = await System.IO.File.ReadAllBytesAsync(fileRecord.FilePath);
            return File(fileBytes, "application/octet-stream", fileRecord.FileName);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var fileRecord = await _fileRepository.GetFileByIdAsync(id);
            if (fileRecord != null && System.IO.File.Exists(fileRecord.FilePath))
            {
                System.IO.File.Delete(fileRecord.FilePath);
                await _fileRepository.DeleteFileAsync(fileRecord.Id);
            }

            return Ok();
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            var username = User.Identity.Name;

            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return NotFound();
            }

            var files = await _fileRepository.GetUserFiles(user.Username);
            return Ok(files);
        }
    }
}
