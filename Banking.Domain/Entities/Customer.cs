using Banking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Domain.Entities
{
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(150)]
        [MaxLength(150)]
        public required string FirstName { get; set; }

        [StringLength(150)]
        [MaxLength(150)]
        public string? LastName { get; set; }

        [Required]
        public DateOnly BirthDate { get; set; }

        [Required]
        public required string DocumentNumber { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Required]
        public decimal MonthlyIncome { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

    }
}
