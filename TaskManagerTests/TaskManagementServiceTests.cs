using Moq;
using Xunit;
using TaskManagementServices.Model;
using TaskManagementServices.Model.Interfaces;
using TaskManagementServices.Services;

namespace TaskManagerTests
{
    public class TaskManagementServiceTests
    {
        private readonly Mock<ITaskRepository> _repo;
        private readonly Mock<IUserService> _users;
        private readonly Mock<INotificationService> _notif;
        private readonly Mock<IAuditService> _audit;

        private readonly TaskManagementService _service;

        public TaskManagementServiceTests()
        {
            _repo = new Mock<ITaskRepository>();
            _users = new Mock<IUserService>();
            _notif = new Mock<INotificationService>();
            _audit = new Mock<IAuditService>();

            // За замовчуванням користувач активний
            _users.Setup(u => u.IsActiveUser(It.IsAny<int>())).Returns(true);

            _service = new TaskManagementService(
                _repo.Object, _users.Object, _notif.Object, _audit.Object);
        }

        // ============================
        // R1 — Inactive user
        // ============================
        [Fact]
        public void R1_ShouldThrow_WhenUserIsInactive()
        {
            _users.Setup(u => u.IsActiveUser(1)).Returns(false);

            Assert.Throws<InvalidOperationException>(() =>
                _service.CreateTask(1, "Task", "Low", DateTime.UtcNow.AddDays(1)));
        }

        // ============================
        // R2 — Empty title
        // ============================
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void R2_ShouldThrow_WhenTitleIsEmpty(string invalidTitle)
        {
            Assert.Throws<ArgumentException>(() =>
                _service.CreateTask(1, invalidTitle, "Low", DateTime.UtcNow.AddDays(1)));
        }

        // ============================
        // R3 — Deadline in the past
        // ============================
        [Fact]
        public void R3_ShouldThrow_WhenDeadlineInPast()
        {
            Assert.Throws<ArgumentException>(() =>
                _service.CreateTask(1, "Test", "Low", DateTime.UtcNow.AddDays(-1)));
        }

        // ============================
        // R4 — Invalid priority
        // ============================
        [Fact]
        public void R4_ShouldThrow_WhenPriorityIsInvalid()
        {
            Assert.Throws<ArgumentException>(() =>
                _service.CreateTask(1, "Test", "INVALID", DateTime.UtcNow.AddDays(1)));
        }

        // ============================
        // R5 — Successful creation
        // ============================
        [Fact]
        public void R5_ShouldSave_Notify_AndLog_WhenTaskCreated()
        {
            _service.CreateTask(1, "Test", "Low", DateTime.UtcNow.AddDays(1));

            _repo.Verify(r => r.Save(It.IsAny<TaskItem>()), Times.Once);
            _notif.Verify(n => n.NotifyCreated(1, "Test"), Times.Once);
            _audit.Verify(a => a.Log("CREATE", 1, "Test"), Times.Once);
        }

        // ============================
        // R6 — Incremental IDs
        // ============================
        [Fact]
        public void R6_ShouldAssignIncrementalIds()
        {
            var t1 = _service.CreateTask(1, "A", "Low", null);
            var t2 = _service.CreateTask(1, "B", "Low", null);

            Assert.Equal(1, t1.Id);
            Assert.Equal(2, t2.Id);
        }

        // ============================
        // R7 — CompleteTask returns false if missing or foreign
        // ============================
        [Fact]
        public void R7_ShouldReturnFalse_WhenTaskMissing()
        {
            _repo.Setup(r => r.FindTask(1)).Returns((TaskItem)null);

            var result = _service.CompleteTask(1, 1);

            Assert.False(result);
        }

        // ============================
        // R8 — Cannot complete an already completed task
        // ============================
        [Fact]
        public void R8_ShouldReturnFalse_WhenTaskAlreadyCompleted()
        {
            var task = new TaskItem
            {
                Id = 1,
                UserId = 1,
                Title = "X",
                IsCompleted = true
            };

            _repo.Setup(r => r.FindTask(1)).Returns(task);

            var result = _service.CompleteTask(1, 1);

            Assert.False(result);
        }

        // ============================
        // R9 — Successful completion
        // ============================
        [Fact]
        public void R9_ShouldMarkCompleted_Notify_AndLog()
        {
            var task = new TaskItem
            {
                Id = 1,
                UserId = 1,
                Title = "X",
                IsCompleted = false
            };

            _repo.Setup(r => r.FindTask(1)).Returns(task);

            var result = _service.CompleteTask(1, 1);

            Assert.True(result);
            Assert.True(task.IsCompleted);

            _repo.Verify(r => r.Save(task), Times.Once);
            _notif.Verify(n => n.NotifyCompleted(1, "X"), Times.Once);
            _audit.Verify(a => a.Log("COMPLETE", 1, "X"), Times.Once);
        }

        // ============================
        // R10 — DeleteTask: false if missing or foreign
        // ============================
        [Fact]
        public void R10_ShouldReturnFalse_WhenTaskMissing()
        {
            _repo.Setup(r => r.FindTask(1)).Returns((TaskItem)null);

            var result = _service.DeleteTask(1, 1);

            Assert.False(result);
        }

        // ============================
        // R11 — Successful delete + log
        // ============================
        [Fact]
        public void R11_ShouldDeleteAndLog_WhenTaskExists()
        {
            var task = new TaskItem { Id = 1, UserId = 1, Title = "X" };

            _repo.Setup(r => r.FindTask(1)).Returns(task);

            var result = _service.DeleteTask(1, 1);

            Assert.True(result);

            _repo.Verify(r => r.Delete(1), Times.Once);
            _audit.Verify(a => a.Log("DELETE", 1, "X"), Times.Once);
        }

        // ============================
        // R12 — GetActiveTasks returns only not completed
        // ============================
        [Fact]
        public void R12_ShouldReturnOnlyNotCompletedTasks()
        {
            _repo.Setup(r => r.GetUserTasks(1)).Returns(new List<TaskItem>
            {
                new TaskItem { Id = 1, IsCompleted = false },
                new TaskItem { Id = 2, IsCompleted = true }
            });

            var tasks = _service.GetActiveTasks(1);

            Assert.Single(tasks);
            Assert.Equal(1, tasks[0].Id);
        }

        // ============================
        // R13 — GetOverdueTasks
        // ============================
        [Fact]
        public void R13_ShouldReturnOnlyOverdueNotCompletedTasks()
        {
            _repo.Setup(r => r.GetUserTasks(1)).Returns(new List<TaskItem>
            {
                new TaskItem { Id = 1, Deadline = DateTime.UtcNow.AddDays(-1), IsCompleted = false },
                new TaskItem { Id = 2, Deadline = DateTime.UtcNow.AddDays(1), IsCompleted = false },
                new TaskItem { Id = 3, Deadline = DateTime.UtcNow.AddDays(-1), IsCompleted = true }
            });

            var result = _service.GetOverdueTasks(1);

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        // ============================
        // R14 — UpdatePriority: false when missing/foreign
        // ============================
        [Fact]
        public void R14_ShouldReturnFalse_WhenTaskMissing()
        {
            _repo.Setup(r => r.FindTask(1)).Returns((TaskItem)null);

            var result = _service.UpdatePriority(1, 1, "Low");

            Assert.False(result);
        }

        // ============================
        // R15 — UpdatePriority: false invalid priority
        // ============================
        [Fact]
        public void R15_ShouldReturnFalse_WhenPriorityInvalid()
        {
            var task = new TaskItem { Id = 1, UserId = 1, Priority = "Low" };

            _repo.Setup(r => r.FindTask(1)).Returns(task);

            var result = _service.UpdatePriority(1, 1, "INVALID");

            Assert.False(result);
        }
    }
}
