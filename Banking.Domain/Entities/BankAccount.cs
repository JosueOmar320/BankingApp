using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Domain.Entities
{
    public class BankAccount
    {
        public int BankAccountId { get; set; }
        public required string AccountNumber { get; set; }
        public int CustomerId { get; set; }
        public decimal Balance { get; set; } 
        public string Currency { get; set; } = "USD";
        public DateTime CreatedAt { get; set; }
    }
}
