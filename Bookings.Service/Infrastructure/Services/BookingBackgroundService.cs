namespace CSCourse.Infrastructure.Services;

public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;
    private readonly string _bootstrapServers;
    private readonly string[] _topics;

    public BookingBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BookingBackgroundService> logger,
        string bootstrapServers,
        IEnumerable<string> topics)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _bootstrapServers = bootstrapServers;
        _topics = topics.ToArray();
    }
 
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "booking-events-observer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topics);

        while (!stoppingToken.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(stoppingToken);
            if (consumeResult?.Message == null) continue;

            try
            {
                // Здесь нет IEventService и нет ReleaseSeatsAsync.
                // Можно логировать, писать в метрики, переотправлять, но не менять состояние мест.
                _logger.LogInformation(
                    "Booking.Service observed event: Topic={Topic}, Offset={Offset}",
                    consumeResult.Topic, consumeResult.Offset);

                consumer.Commit(consumeResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event in BookingBackgroundService");
            }
        }
    }
}
