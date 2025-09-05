using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Dtos
{
    public class AccountTransactionSummaryDto
    {
        public required string AccountNumber { get; set; }
        public decimal StartingBalance { get; set; }
        public List<TransactionSummaryDto> Transactions { get; set; } = new();
        public decimal FinalBalance { get; set; }
    }
}
