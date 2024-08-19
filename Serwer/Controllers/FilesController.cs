using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serwer.Models;
using Serwer.Repositories;
using System.Collections.Concurrent;

namespace Serwer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FilesController : Controller
    {
        private readonly string basePath = "C:\\Users\\micha\\Desktop\\PRIRprojekt\\SerwerPrir\\Serwer\\NetworkDrive";
        private readonly FileRepository _fileRepository;
        private readonly UserRepository _userRepository;

        public FilesController(FileRepository fileRepository, UserRepository userRepository)
        {
            _fileRepository = fileRepository;
            _userRepository = userRepository;
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

        [HttpPost("reverse/{id}")]
        public async Task<IActionResult> ReverseFile(int id)
        {
            var fileRecord = await _fileRepository.GetFileByIdAsync(id);
            if (fileRecord == null || !System.IO.File.Exists(fileRecord.FilePath))
            {
                return NotFound("File not found.");
            }

            await ProcessFile(fileRecord);

            return Ok("File processed successfully.");
        }

        [HttpPost("largestnumber/{id}")]
        public async Task<IActionResult> FindLargestNumber(int id)
        {
            var fileRecord = await _fileRepository.GetFileByIdAsync(id);
            if (fileRecord == null || !System.IO.File.Exists(fileRecord.FilePath))
            {
                return NotFound("File not found.");
            }

            var largestNumber = await Task.Run(() => FindLargestNumberInFile(fileRecord));

            return Ok(new { LargestNumber = largestNumber });
        }

        private int FindLargestNumberInFile(FileRecord fileRecord)
        {
            Console.WriteLine("Starting FindLargestNumberInFile method.");
            var lines = System.IO.File.ReadAllLines(fileRecord.FilePath);

            var numLines = lines.Length;

            const int linesPerPart = 1;
            var parts = new List<string[]>();
            for (int i = 0; i < numLines; i += linesPerPart)
            {
                var part = lines.Skip(i).Take(linesPerPart).ToArray();
                parts.Add(part);
            }

            var maxNumbers = new ConcurrentBag<int>();
            var tasks = parts.Select(part => Task.Run(() =>
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"Processing part on thread {threadId}");
                var maxInPart = FindMaxInPart(part);
                maxNumbers.Add(maxInPart);
            })).ToArray();

            Console.WriteLine("All tasks have been added to the queue.");
            try
            {
                Task.WaitAll(tasks);
                Console.WriteLine("All tasks completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in Task.WhenAll: {ex.Message}");
            }

            return maxNumbers.Max();
        }

        private int FindMaxInPart(string[] part)
        {
            if (part == null || part.Length == 0)
                return int.MinValue;

            return part.Select(line => int.TryParse(line, out var num) ? num : int.MinValue).Max();
        }



        private async Task ProcessFile(FileRecord fileRecord)
        {
            Console.WriteLine("Starting ProcessFile method.");

            var lines = await System.IO.File.ReadAllLinesAsync(fileRecord.FilePath);
            var results = new ConcurrentBag<string>();

            var tasks = lines.Select(line => Task.Run(async () =>
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"Processing line on thread {threadId}");

                var processedLine = await ProcessLineAsync(line);
                results.Add(processedLine);
            })).ToArray();

            Console.WriteLine("All tasks have been added to the queue.");
            try
            {
                await Task.WhenAll(tasks);
                Console.WriteLine("All tasks completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in Task.WhenAll: {ex.Message}");
            }

            string outputFilePath = null;

            try
            {
                var directory = Path.GetDirectoryName(fileRecord.FilePath) ?? Directory.GetCurrentDirectory();
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileRecord.FileName);
                var fileExtension = Path.GetExtension(fileRecord.FileName);

                if (string.IsNullOrEmpty(fileNameWithoutExtension))
                    throw new InvalidOperationException("The file name without extension is empty.");

                if (string.IsNullOrEmpty(fileExtension))
                    fileExtension = ".txt";

                outputFilePath = Path.Combine(directory, $"{fileNameWithoutExtension}_reverse{fileExtension}");
                Console.WriteLine($"Output file path: {outputFilePath}");

                using (var writer = new StreamWriter(outputFilePath))
                {
                    foreach (var line in results)
                    {
                        await writer.WriteLineAsync(line);
                    }
                }
                Console.WriteLine("File written successfully.");

                var reversedFileRecord = new FileRecord
                {
                    FileName = $"{fileNameWithoutExtension}_reverse{fileExtension}",
                    FilePath = outputFilePath,
                    UserId = fileRecord.UserId
                };

                await _fileRepository.AddFileAsync(reversedFileRecord);
                Console.WriteLine("Reversed file record added to the database.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
            }
        }

        private Task<string> ProcessLineAsync(string line)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var reversedLine = new string(line.Reverse().ToArray());
            Console.WriteLine($"Processed line on thread {threadId}: {reversedLine}");
            return Task.FromResult(reversedLine);
        }

    }
}