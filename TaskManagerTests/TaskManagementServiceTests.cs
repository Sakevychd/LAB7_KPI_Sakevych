using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            _service = new TaskManagementService(
                _repo.Object, _users.Object, _notif.Object, _audit.Object);
        }

    }


}
