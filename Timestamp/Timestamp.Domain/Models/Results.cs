using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Timestamp.Domain.Models
{
    public class Results
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public decimal DeltaTime { get; set; }
        public DateTime StartTime { get; set; }
        public decimal AverageExecutionTime { get; set; }
        public decimal AverageValue { get; set; }
        public decimal MedianValue { get; set; }
        public decimal MaxValue { get; set; }
        public decimal MinValue { get; set; }
        public List<Values> ValuesList { get; set; } = new List<Values>();
    }
}
