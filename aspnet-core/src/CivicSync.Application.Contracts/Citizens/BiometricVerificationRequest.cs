namespace CivicSync.Application.Contracts.Citizens;

public class BiometricVerificationRequest
{
    public string Method { get; set; } = string.Empty;
    public string DeviceLabel { get; set; } = string.Empty;

    public string Descriptor { get; set; } = string.Empty;
}
