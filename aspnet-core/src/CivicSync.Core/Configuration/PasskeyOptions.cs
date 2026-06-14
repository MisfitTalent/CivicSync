namespace CivicSync.Core.Configuration;

public sealed class PasskeyOptions
{
    public const string SectionName = "Passkey";

    public string RelyingPartyId { get; set; } = "localhost";

    public string RelyingPartyName { get; set; } = "CivicSync Ledger";

    public string[] AllowedOrigins { get; set; } =
    [
        "http://localhost:5173",
        "https://localhost:5173",
        "http://127.0.0.1:5173",
        "https://127.0.0.1:5173"
    ];
}
