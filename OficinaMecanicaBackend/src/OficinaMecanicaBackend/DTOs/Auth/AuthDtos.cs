namespace OficinaMecanicaBackend.DTOs.Auth;

public record LoginDto(string Username, string Password);

public record TokenDto(string Token, DateTime ExpiresAt);
