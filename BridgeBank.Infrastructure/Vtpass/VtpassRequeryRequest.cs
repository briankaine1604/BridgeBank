using System.Text.Json.Serialization;

namespace BridgeBank.Infrastructure.Vtpass;

internal record VtpassRequeryRequest(
    [property: JsonPropertyName("request_id")] string RequestId);
