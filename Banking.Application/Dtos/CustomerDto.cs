using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Dtos
{
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public required string FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly BirthDate { get; set; }
        public required string DocumentNumber { get; set; }
        public decimal MonthlyIncome { get; set; }
    }
}
