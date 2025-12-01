namespace TaskManagementServices.Model.Interfaces
{
    public interface IAuditService
    {
        void Log(string action, int userId, string detail);
    }
}
