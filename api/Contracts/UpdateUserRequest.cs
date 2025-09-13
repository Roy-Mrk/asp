namespace Api.Contracts;

public record UpdateUserRequest(string? Username, string? Password, bool? IsAdmin);

