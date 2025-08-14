using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Timestamp.Application.Services;
using Timestamp.Infrastructure;

namespace Timestamp.Tests.Services
{
    [TestFixture]
    public class FileProcessingServiceTests
    {
        private ApplicationDbContext _context;
        private FileProcessingService _service;
        private SqliteConnection _connection;

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _service = new FileProcessingService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }

        [Test]
        public async Task ProcessFile_ValidCsv_ShouldWork()
        {
            var csvContent = "2024-01-15T10:30:00.000Z;1.5;42.7\n2024-01-15T10:31:00.000Z;2.1;38.9";
            var file = CreateFile("test.csv", csvContent);

            Assert.DoesNotThrowAsync(() => _service.ProcessFileAsync(file, CancellationToken.None));

            var results = await _context.Results.CountAsync();
            var values = await _context.Values.CountAsync();
            Assert.That(results, Is.EqualTo(1));
            Assert.That(values, Is.EqualTo(2));
        }

        [Test]
        public void ProcessFile_InvalidDate_ThrowsError()
        {
            var csvContent = "invalid-date;1.5;42.7";
            var file = CreateFile("test.csv", csvContent);

            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => _service.ProcessFileAsync(file, CancellationToken.None));

            Assert.That(ex.Message, Does.Contain("Ошибка парсинга даты"));
        }

        [Test]
        public void ProcessFile_NegativeValue_ThrowsError()
        {
            var csvContent = "2024-01-15T10:30:00.000Z;1.5;-42.7";
            var file = CreateFile("test.csv", csvContent);

            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => _service.ProcessFileAsync(file, CancellationToken.None));

            Assert.That(ex.Message, Does.Contain("не может быть отрицательным"));
        }

        [Test]
        public void ProcessFile_EmptyFile_ThrowsError()
        {
            var file = CreateFile("empty.csv", "");

            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => _service.ProcessFileAsync(file, CancellationToken.None));

            Assert.That(ex.Message, Does.Contain("не должен быть пустым"));
        }

        [Test]
        public void ProcessFile_TooManyRows_ThrowsError()
        {
            var csvContent = string.Join("\n",
                Enumerable.Range(1, 10001)
                          .Select(i => $"2024-01-15T10:30:{i % 60:00}.000Z;1.5;42.7"));

            var file = CreateFile("large.csv", csvContent);

            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => _service.ProcessFileAsync(file, CancellationToken.None));

            Assert.That(ex.Message, Does.Contain("более 10 000 строк"));
        }

        [Test]
        public async Task ProcessFile_CalculatesAggregates()
        {
            var csvContent = "2024-01-15T10:30:00.000Z;1.0;10.0\n2024-01-15T10:31:00.000Z;2.0;20.0\n2024-01-15T10:32:00.000Z;3.0;30.0";
            var file = CreateFile("test.csv", csvContent);

            await _service.ProcessFileAsync(file, CancellationToken.None);

            var savedResult = await _context.Results.FirstOrDefaultAsync();

            Assert.That(savedResult, Is.Not.Null);
            Assert.That(savedResult.DeltaTime, Is.EqualTo(120m));
            Assert.That(savedResult.AverageExecutionTime, Is.EqualTo(2.0m));
            Assert.That(savedResult.AverageValue, Is.EqualTo(20.0m));
            Assert.That(savedResult.MedianValue, Is.EqualTo(20.0m));
            Assert.That(savedResult.MaxValue, Is.EqualTo(30.0m));
            Assert.That(savedResult.MinValue, Is.EqualTo(10.0m));
        }

        private IFormFile CreateFile(string fileName, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var file = new Mock<IFormFile>();

            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.Length).Returns(bytes.Length);
            file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(bytes));

            return file.Object;
        }
    }
}
