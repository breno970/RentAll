namespace aluguel.Services;
public sealed class AuthMessageSenderOptions
{
    public string? SendGridKey { get; set; }
    public string? EmailFromAddress { get; set; }
    public string EmailFromName { get; set; } = "RentAll";
}
