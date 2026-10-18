using Banking.Application.Interfaces;

namespace Banking.Tests.Support;

/// <summary>The caller of a use case under test; <see langword="null"/> means anonymous.</summary>
internal sealed class FakeCurrentCustomer : ICurrentCustomer
{
    public FakeCurrentCustomer(int? customerId)
    {
        CustomerId = customerId;
    }

    public int? CustomerId { get; set; }
}
