using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Timestamp.Domain.Models
{
    public class Values
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal ExecutionTime { get; set; }
        public decimal Value { get; set; }
        public int ResultsId { get; set; }
        [ForeignKey("ResultsId")]
        public Results Results { get; set; }
    }
}
