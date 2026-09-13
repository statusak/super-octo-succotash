namespace Events.Service.Infrastructure.Config;

public class RedisSettings
{
    public string Servers { get; set; } = string.Empty;
    public string? Password { get; set; }
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 3000;
    public bool AbortOnConnectFail { get; set; } = false;
    public int ExpiryEventByIdMinutes { get; set; } = 10;
    public int ExpiryTop10EventsMinutes { get; set; } = 1;
}
