using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Common
{
    public static class TokenHelper
    {
        public static string GenerateToken(string value)
        {
            // Token oluşturma mantığı
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
            return token;
        }

        public static string DecodeToken(string token)
        {
            // Token çözme mantığı
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            return value;
        }
    }
}
