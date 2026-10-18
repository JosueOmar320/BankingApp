namespace Banking.Application.Dtos;

/// <summary>Position of a page inside a paginated list.</summary>
/// <param name="Number">1-based number of this page.</param>
/// <param name="Size">Maximum number of items per page.</param>
/// <param name="TotalItems">Number of items across all pages.</param>
/// <param name="TotalPages">Number of pages; zero when the list is empty.</param>
public sealed record PageInfoDto(int Number, int Size, int TotalItems, int TotalPages);
