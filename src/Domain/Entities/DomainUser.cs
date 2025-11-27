namespace Domain.Entities;

public class DomainUser(string id, string username, string email)
{
    public string Id { get; private set; } = id;
    public string Username { get; private set; } = username;
    public string Email { get; private set; } = email;
    public List<Task>? AssignedTasks { get; } = [];
    
    public List<Task>? CreatedTasks { get; } = [];
    public List<Notification>? Notifications { get; } = [];
    
}
