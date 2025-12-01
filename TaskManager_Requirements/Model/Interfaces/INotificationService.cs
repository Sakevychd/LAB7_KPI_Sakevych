namespace TaskManagementServices.Model.Interfaces
{
    public interface INotificationService
    {
        void NotifyCreated(int userId, string title);
        void NotifyCompleted(int userId, string title);
    }

}
