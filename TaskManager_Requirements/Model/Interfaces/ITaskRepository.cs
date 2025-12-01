namespace TaskManagementServices.Model.Interfaces
{
    public interface ITaskRepository
    {
        TaskItem FindTask(int taskId);
        List<TaskItem> GetUserTasks(int userId);
        void Save(TaskItem task);
        void Delete(int taskId);
    }

}
