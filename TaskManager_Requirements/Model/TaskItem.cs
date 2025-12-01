namespace TaskManagementServices.Model
{
    public class TaskItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string Title { get; set; }
        public string Priority { get; set; } // Low | Medium | High

        public bool IsCompleted { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Deadline { get; set; }
    }

}
