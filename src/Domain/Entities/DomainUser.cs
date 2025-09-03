namespace Domain.Entities;

public class DomainUser(string id, string username)
{
    public string Id { get; private set; } = id;
    
    public string Username { get; private set; } = username;
    public List<Task>? AssignedTasks { get; } = [];
    
    public List<Task>? CreatedTasks { get; } = [];
    
}
