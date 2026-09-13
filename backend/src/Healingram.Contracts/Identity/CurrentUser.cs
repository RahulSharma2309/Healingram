namespace Healingram.Contracts.Identity;

public sealed record CurrentUser(Guid Id, string Email, string Role, string? FullName);
