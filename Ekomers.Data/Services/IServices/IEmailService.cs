using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
    public interface IEmailService
    {
        void SendEmail(string toEmail, string subject, string body);
    }
}
