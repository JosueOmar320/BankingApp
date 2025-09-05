using Banking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Dtos
{
    public class TransactionResponseDto
    {
        public required string AccountNumber { get; set; }

        [EnumDataType(typeof(TransactionType))]
        public TransactionType TransactionType { get; set; }

        public decimal Amount { get; set; }

        public decimal BalanceAfter { get; set; }
    }
}
