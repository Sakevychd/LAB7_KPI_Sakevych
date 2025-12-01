using TaskManagementServices.Model;
using TaskManagementServices.Model.Interfaces;

namespace TaskManagementServices.Services
{
    public class TaskManagementService
    {
        private readonly ITaskRepository _repo;
        private readonly IUserService _users;
        private readonly INotificationService _notif;
        private readonly IAuditService _audit;

        private int _nextId = 1;

        public TaskManagementService(
            ITaskRepository repo,
            IUserService users,
            INotificationService notif,
            IAuditService audit)
        {
            _repo = repo;
            _users = users;
            _notif = notif;
            _audit = audit;
        }

        public TaskItem CreateTask(int userId, string title, string priority, DateTime? deadline)
        {
            if (!_users.IsActiveUser(userId))
                throw new InvalidOperationException("Inactive user.");

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title required.");

            if (deadline.HasValue && deadline.Value < DateTime.UtcNow)
                throw new ArgumentException("Deadline cannot be in the past.");

            if (priority != "Low" && priority != "Medium" && priority != "High")
                throw new ArgumentException("Invalid priority.");

            var task = new TaskItem
            {
                Id = _nextId++,
                UserId = userId,
                Title = title,
                Priority = priority,
                Deadline = deadline
            };

            _repo.Save(task);
            _notif.NotifyCreated(userId, title);
            _audit.Log("CREATE", userId, title);

            return task;
        }

        public bool CompleteTask(int userId, int taskId)
        {
            var task = _repo.FindTask(taskId);
            if (task == null || task.UserId != userId)
                return false;

            if (task.IsCompleted)
                return false;

            task.IsCompleted = true;

            _repo.Save(task);
            _notif.NotifyCompleted(userId, task.Title);
            _audit.Log("COMPLETE", userId, task.Title);

            return true;
        }

        public bool DeleteTask(int userId, int taskId)
        {
            var task = _repo.FindTask(taskId);
            if (task == null || task.UserId != userId)
                return false;

            _repo.Delete(taskId);
            _audit.Log("DELETE", userId, task.Title);
            return true;
        }

        public List<TaskItem> GetActiveTasks(int userId)
        {
            return _repo.GetUserTasks(userId)
                        .Where(t => !t.IsCompleted)
                        .ToList();
        }

        public List<TaskItem> GetOverdueTasks(int userId)
        {
            return _repo.GetUserTasks(userId)
                        .Where(t => !t.IsCompleted &&
                                    t.Deadline.HasValue &&
                                    t.Deadline.Value < DateTime.UtcNow)
                        .ToList();
        }

        public bool UpdatePriority(int userId, int taskId, string newPriority)
        {
            var task = _repo.FindTask(taskId);

            if (task == null || task.UserId != userId)
                return false;

            if (newPriority != "Low" && newPriority != "Medium" && newPriority != "High")
                return false;

            task.Priority = newPriority;
            _repo.Save(task);

            _audit.Log("UPDATE_PRIORITY", userId, newPriority);
            return true;
        }
    }

}
