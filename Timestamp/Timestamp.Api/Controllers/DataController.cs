using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timestamp.Application.Interfaces;
using Timestamp.Application.Services;
using Timestamp.Infrastructure;

namespace Timestamp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DataController : ControllerBase
    {
        private readonly IFileProcessingService _fileProcessingService;
        private readonly ApplicationDbContext _context;

        public DataController(IFileProcessingService fileProcessingService, ApplicationDbContext context)
        {
            _fileProcessingService = fileProcessingService;
            _context = context;
        }

        /// <summary>
        /// Метод 1: Загрузка и обработка CSV файла
        /// </summary>
        /// <param name="file">CSV файл с данными в формате: Date;ExecutionTime;Value</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Результат обработки файла</returns>
        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadCsv(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Файл не был загружен или пустой.");
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Допустимы только CSV файлы.");
            }

            try
            {
                await _fileProcessingService.ProcessFileAsync(file, cancellationToken);
                return Ok(new
                {
                    message = "Файл успешно обработан и сохранен.",
                    fileName = file.FileName,
                    processedAt = DateTime.UtcNow
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Внутренняя ошибка сервера при обработке файла." });
            }
        }

        /// <summary>
        /// Метод 2: Получение списка результатов с фильтрацией
        /// </summary>
        /// <param name="fileName">Фильтр по имени файла (частичное совпадение)</param>
        /// <param name="startDateFrom">Начальная дата диапазона времени запуска</param>
        /// <param name="startDateTo">Конечная дата диапазона времени запуска</param>
        /// <param name="avgValueFrom">Минимальное значение среднего</param>
        /// <param name="avgValueTo">Максимальное значение среднего</param>
        /// <param name="avgExecTimeFrom">Минимальное значение среднего времени выполнения</param>
        /// <param name="avgExecTimeTo">Максимальное значение среднего времени выполнения</param>
        /// <returns>Список результатов, соответствующих фильтрам</returns>
        [HttpGet("results")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResults(
            [FromQuery] string? fileName = null,
            [FromQuery] DateTime? startDateFrom = null,
            [FromQuery] DateTime? startDateTo = null,
            [FromQuery] decimal? avgValueFrom = null,
            [FromQuery] decimal? avgValueTo = null,
            [FromQuery] decimal? avgExecTimeFrom = null,
            [FromQuery] decimal? avgExecTimeTo = null)
        {
            var query = _context.Results.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(fileName))
                query = query.Where(r => r.FileName.Contains(fileName));

            if (startDateFrom.HasValue)
                query = query.Where(r => r.StartTime >= startDateFrom.Value);

            if (startDateTo.HasValue)
                query = query.Where(r => r.StartTime <= startDateTo.Value);

            if (avgValueFrom.HasValue)
                query = query.Where(r => r.AverageValue >= avgValueFrom.Value);

            if (avgValueTo.HasValue)
                query = query.Where(r => r.AverageValue <= avgValueTo.Value);

            if (avgExecTimeFrom.HasValue)
                query = query.Where(r => r.AverageExecutionTime >= avgExecTimeFrom.Value);

            if (avgExecTimeTo.HasValue)
                query = query.Where(r => r.AverageExecutionTime <= avgExecTimeTo.Value);

            var results = await query
                .OrderBy(r => r.StartTime)
                .ToListAsync();

            return Ok(new
            {
                count = results.Count,
                data = results
            });
        }

        /// <summary>
        /// Метод 3: Получение последних 10 значений для заданного файла
        /// </summary>
        /// <param name="fileName">Имя файла для поиска</param>
        /// <returns>Последние 10 записей, отсортированные по времени запуска</returns>
        [HttpGet("values/{fileName}/latest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestValues(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("Имя файла не может быть пустым.");
            }

            var resultExists = await _context.Results
                .AnyAsync(r => r.FileName == fileName);

            if (!resultExists)
            {
                return NotFound(new
                {
                    error = $"Данные для файла '{fileName}' не найдены.",
                    fileName = fileName
                });
            }
            var values = await _context.Values
                .AsNoTracking()
                .Where(v => v.Results.FileName == fileName)
                .OrderByDescending(v => v.Date)
                .Take(10)
                .OrderBy(v => v.Date)
                .Select(v => new {
                    v.Id,
                    v.Date,
                    v.ExecutionTime,
                    v.Value
                }).ToListAsync();

            return Ok(new
            {
                fileName = fileName,
                count = values.Count,
                data = values
            });
        }

        /// <summary>
        /// Получение списка всех доступных файлов
        /// </summary>
        [HttpGet("files")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableFiles()
        {
            var files = await _context.Results
                .AsNoTracking()
                .Select(r => new {
                    r.FileName,
                    r.StartTime,
                    RecordsCount = r.ValuesList.Count()
                })
                .OrderBy(f => f.FileName)
                .ToListAsync();

            return Ok(new
            {
                count = files.Count,
                files = files
            });
        }
    }
}
