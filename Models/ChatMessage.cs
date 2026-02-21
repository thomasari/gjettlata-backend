namespace GjettLataBackend.Models;

public class ChatMessage
{
    public ChatMessage()
    {}
    public ChatMessage(string message)
    {
        Sender = new Player
        {
            Id = "System",
            Name = "System",
            Color = "#b0b0b0",
            Score = 0
        };
        IsSystemMessage = true;
        Message = message;
    }
    
    public Player Sender { get; set; }
    public string Message { get; set; }
    public bool IsSystemMessage { get; set; }
}