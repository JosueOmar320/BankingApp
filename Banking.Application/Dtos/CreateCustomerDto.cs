namespace Banking.Application.Dtos
{
    public class CreateCustomerDto
    {
        public required string FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly BirthDate { get; set; }
        public required string DocumentNumber { get; set; }
        public decimal MonthlyIncome { get; set; }
    }
}
