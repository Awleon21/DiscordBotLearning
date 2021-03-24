using Newtonsoft.Json;

public struct AmongUsConfig
{
    [JsonProperty("token")]
    public string Token { get; private set; }

    [JsonProperty("prefix")]
    public string CommandPrefix { get; private set; }
}