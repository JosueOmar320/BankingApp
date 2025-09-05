using Banking.Domain.Entities;

namespace Banking.Application.Dtos
{
    public class CreateCustomerDto
    {
        public required string FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly BirthDate { get; set; }
        public required string DocumentNumber { get; set; }
        public decimal MonthlyIncome { get; set; }

        public Customer ToEntity()
        {
            return new Customer
            {
                FirstName = FirstName,
                LastName = LastName,
                BirthDate = BirthDate,
                DocumentNumber = DocumentNumber,
                MonthlyIncome = MonthlyIncome,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
