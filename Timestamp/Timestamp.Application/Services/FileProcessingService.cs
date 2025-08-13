using System.Globalization;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Timestamp.Application.DTOs;
using Timestamp.Application.Interfaces;
using Timestamp.Domain.Models;

namespace Timestamp.Application.Services
{
    public class FileProcessingService : IFileProcessingService
    {
        private readonly IApplicationDbContext _context;

        public FileProcessingService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ProcessFileAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var fileName = file.FileName;
            var csvRows = await ParseAndValidateCsv(file, cancellationToken);

            var dataValues = csvRows.Select(r => r.Value).ToList();
            var minDate = csvRows.Min(r => r.Date);
            var maxDate = csvRows.Max(r => r.Date);

            var newResult = new Results
            {
                FileName = fileName,
                StartTime = minDate,
                DeltaTime = (decimal)(maxDate - minDate).TotalSeconds,
                AverageExecutionTime = csvRows.Average(r => r.ExecutionTime),
                AverageValue = dataValues.Average(),
                MaxValue = dataValues.Max(),
                MinValue = dataValues.Min(),
                MedianValue = CalculateMedian(dataValues)
            };

            foreach (var row in csvRows)
            {
                var valueItem = new Values();
                valueItem.Date = row.Date;
                valueItem.ExecutionTime = row.ExecutionTime;
                valueItem.Value = row.Value;
                newResult.ValuesList.Add(valueItem);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingResult = await _context.Results
                    .Include(r => r.ValuesList)
                    .FirstOrDefaultAsync(r => r.FileName == fileName, cancellationToken);

                if (existingResult != null)
                {
                    _context.Values.RemoveRange(existingResult.ValuesList);
                    _context.Results.Remove(existingResult);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                _context.Results.Add(newResult);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<List<CsvRowDto>> ParseAndValidateCsv(IFormFile file, CancellationToken cancellationToken)
        {
            var records = new List<CsvRowDto>();
            using var reader = new StreamReader(file.OpenReadStream());
            var csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = false
            };

            using var csv = new CsvReader(reader, csvConfig);
            int rowCount = 0;

            await foreach (var record in csv.GetRecordsAsync<dynamic>(cancellationToken))
            {
                rowCount++;
                if (rowCount > 10000)
                    throw new ArgumentException("Файл содержит более 10 000 строк.");

                if (!DateTime.TryParse(record.Field1, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime date))
                {
                    throw new ArgumentException($"Ошибка парсинга даты в строке {rowCount}.");
                }

                if (!decimal.TryParse(record.Field2, CultureInfo.InvariantCulture, out decimal executionTime))
                {
                    throw new ArgumentException($"Ошибка парсинга времени выполнения в строке {rowCount}.");
                }

                if (!decimal.TryParse(record.Field3, CultureInfo.InvariantCulture, out decimal value))
                {
                    throw new ArgumentException($"Ошибка парсинга значения в строке {rowCount}.");
                }

                if (date > DateTime.UtcNow || date < new DateTime(2000, 1, 1))
                    throw new ArgumentException($"Некорректная дата в строке {rowCount}.");

                if (executionTime < 0)
                    throw new ArgumentException($"Время выполнения не может быть отрицательным в строке {rowCount}.");

                if (value < 0)
                    throw new ArgumentException($"Значение не может быть отрицательным в строке {rowCount}.");

                records.Add(new CsvRowDto { Date = date, ExecutionTime = executionTime, Value = value });
            }


            if (rowCount < 1)
                throw new ArgumentException("Файл не должен быть пустым.");

            return records;
        }

        private decimal CalculateMedian(List<decimal> numbers)
        {
            var sortedNumbers = numbers.OrderBy(n => n).ToList();
            int mid = sortedNumbers.Count / 2;
            return sortedNumbers.Count % 2 == 0
                ? (sortedNumbers[mid - 1] + sortedNumbers[mid]) / 2
                : sortedNumbers[mid];
        }
    }
}
