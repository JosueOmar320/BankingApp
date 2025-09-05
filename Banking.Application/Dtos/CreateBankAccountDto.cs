using Banking.Domain.Entities;

namespace Banking.Application.Dtos
{
    public class CreateBankAccountDto
    {
        public int CustomerId { get; set; }
        public decimal InitialDeposit { get; set; } 
    }
}
