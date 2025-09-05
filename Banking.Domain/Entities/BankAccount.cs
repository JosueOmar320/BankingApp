using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Domain.Entities
{
    public class BankAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BankAccountId { get; set; }

        [Required]
        public required string AccountNumber { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public decimal Balance { get; set; } 

        [Required]
        public string Currency { get; set; } = "USD";

        [Required]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
    }
}
