using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serwer.Models;
using Serwer.Repositories;
using Serwer.Services;
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

            //await _taskQueue.AddTask(() => ProcessFile(fileRecord));

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

            // Proces przetwarzania pliku
            await ProcessFile(fileRecord);

            return Ok("File processed successfully.");
        }

        private async Task ProcessFile(FileRecord fileRecord)
        {
            Console.WriteLine("Starting ProcessFile method.");

            // Wczytaj wszystkie linie z pliku
            var lines = await System.IO.File.ReadAllLinesAsync(fileRecord.FilePath);
            var results = new ConcurrentBag<string>();

            // Utwórz zadania do przetworzenia każdej linii
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
                // Oczekiwanie na zakończenie wszystkich zadań
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
                // Konstrukcja ścieżki dla nowego pliku
                var directory = Path.GetDirectoryName(fileRecord.FilePath) ?? Directory.GetCurrentDirectory();
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileRecord.FileName);
                var fileExtension = Path.GetExtension(fileRecord.FileName);

                if (string.IsNullOrEmpty(fileNameWithoutExtension))
                    throw new InvalidOperationException("The file name without extension is empty.");

                if (string.IsNullOrEmpty(fileExtension))
                    fileExtension = ".txt";

                outputFilePath = Path.Combine(directory, $"{fileNameWithoutExtension}_reverse{fileExtension}");
                Console.WriteLine($"Output file path: {outputFilePath}");

                // Zapisz przetworzone linie do nowego pliku
                using (var writer = new StreamWriter(outputFilePath))
                {
                    foreach (var line in results)
                    {
                        await writer.WriteLineAsync(line);
                    }
                }
                Console.WriteLine("File written successfully.");

                // Dodaj nowy plik do bazy danych
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