using Banking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Dtos
{
    public class CustomerResponseDto
    {
        public int CustomerId { get; set; }
        public required string FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly BirthDate { get; set; }
        public required string DocumentNumber { get; set; }

        [EnumDataType(typeof(Gender))]
        public Gender Gender { get; set; }
        public decimal MonthlyIncome { get; set; }
    }
}
