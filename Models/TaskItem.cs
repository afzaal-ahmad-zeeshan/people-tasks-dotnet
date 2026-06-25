namespace hello_dotnet.Models;

public enum TaskStatus
{
    Draft,
    Review,
    Done
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TaskStatus Status { get; set; } = TaskStatus.Draft;
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
}
