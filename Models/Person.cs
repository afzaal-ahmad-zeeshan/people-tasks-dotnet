namespace hello_dotnet.Models;

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public List<string> Hobbies { get; set; } = [];
    public List<TaskItem> Tasks { get; set; } = [];
}
