using Banking.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Banking.Application.Dtos
{
    public class CreateBankAccountDto
    {
        public int CustomerId { get; set; }

        [Range(10, double.MaxValue, ErrorMessage = "Initial deposit must be greater than zero.")]
        public decimal InitialDeposit { get; set; } 
    }
}
