namespace GjettLataBackend.Services;

public class RoomCleanupService : BackgroundService
{
    private readonly RoomManager _roomManager;

    public RoomCleanupService(RoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _roomManager.CleanupInactiveRooms();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}