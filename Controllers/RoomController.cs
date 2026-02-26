using GjettLataBackend;
using Microsoft.AspNetCore.Mvc;
using GjettLataBackend.Models;
using GjettLataBackend.Services;
using Microsoft.AspNetCore.SignalR;
using NanoidDotNet;

[ApiController]
[Route("room")]
public class RoomController : ControllerBase
{
    private readonly RoomManager _rooms;
    private readonly GameEngineService _engine;
    private readonly IHubContext<RoomHub> _hub;

    public RoomController(RoomManager rooms, GameEngineService engine,  IHubContext<RoomHub> hub)
    {
        _rooms = rooms;
        _engine = engine;
        _hub = hub;
    }

    /* ============================= */
    /* CREATE ROOM */
    /* ============================= */

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var room = _rooms.CreateRoom();

        return Ok(_engine.ToDto(room));
    }

    /* ============================= */
    /* GET ROOM */
    /* ============================= */

    [HttpGet("{roomId}")]
    public IActionResult Get(string roomId)
    {
        var room = _rooms.GetRoom(roomId);
        if (room == null) return NotFound();

        return Ok(_engine.ToDto(room));
    }

    /* ============================= */
    /* JOIN ROOM */
    /* ============================= */

    [HttpPost("{roomId}/join")]
    public async Task<IActionResult> Join(string roomId, [FromBody] PlayerDto dto)
    {
        var room = _rooms.GetRoom(roomId);
        if (room == null) return NotFound();

        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Color = dto.Color,
            Score = 0
        };

        if (room.Players.Count == 0)
            room.Host = player;

        room.Players.Add(player);

        var message = $"{player.Name} ble med i spillet!";
        
        await _hub.Clients.All.SendAsync("ReceiveChat", player, message, true);
        
        await _hub.Clients.Group(roomId)
            .SendAsync("RoomState", _engine.ToDto(room));

        return Ok(new
        {
            newPlayer = player,
            room = _engine.ToDto(room)
        });
    }

    /* ============================= */
    /* SET GAMEMODE */
    /* ============================= */

    [HttpPost("{roomId}/gamemode")]
    public IActionResult SetMode(string roomId, [FromBody] GameMode mode)
    {
        var room = _rooms.GetRoom(roomId);
        if (room == null) return NotFound();

        room.CurrentGame?.GameMode = mode;

        return Ok(_engine.ToDto(room));
    }

    /* ============================= */
    /* START GAME */
    /* ============================= */

    [HttpPost("{roomId}/start")]
    public async Task<IActionResult> Start(string roomId)
    {
        var room = _rooms.GetRoom(roomId);
        if (room == null) return NotFound();
        
        if (room.CurrentGame == null)
        {
            room.CurrentGame = new Game();
        }

        await _engine.StartGame(room, roomId, 16);

        return Ok(_engine.ToDto(room));
    }
}