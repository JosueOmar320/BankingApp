using Banking.Domain.Entities;
using Banking.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Banking.Application.Dtos
{
    public class CreateCustomerDto
    {
        public required string FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly BirthDate { get; set; }
        public required string DocumentNumber { get; set; }

        [RegularExpression("M|F|O", ErrorMessage = "Gender must be M, F, or O.")]
        public required string Gender { get; set; }

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
                CreatedAt = DateTime.UtcNow,
                Gender = Enum.Parse<Gender>(Gender, ignoreCase: true)
            };
        }
    }
}
