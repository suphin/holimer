
namespace Ekomers.Data.Services.IServices
{
    public interface IUserService
    {
        void AddUserActivityLog(string ControllerName, string ActionName, string Parameters, string Info, string UserName);
    }
}
