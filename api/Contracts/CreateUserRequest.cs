namespace Api.Contracts;

public record CreateUserRequest(string Username, string Password, bool? IsAdmin);

