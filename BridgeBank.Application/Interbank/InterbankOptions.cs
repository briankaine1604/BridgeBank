namespace BridgeBank.Application.Interbank;

public class InterbankOptions
{
    public const string SectionName = "Interbank";

    public string PeerCode { get; set; } = string.Empty;
    public string PeerBaseUrl { get; set; } = string.Empty;
    public string PeerSecret { get; set; } = string.Empty;

    // Addresses allowed to call the inbound endpoint, as seen by Kestrel.
    public List<string> AllowedCallers { get; set; } = [];
}
