namespace Karami.Core.UseCase.DTOs;

public class TokenParameterDto
{
    public required string Key      { get; set; }
    public required string Issuer   { get; set; }
    public required string Audience { get; set; }
    public required int Expires     { get; set; }
}