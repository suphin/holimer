using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;

namespace Intranet.Data.lib
{
    public class Crypto
    {
        public static string Sifrele(string sifrelenecek)
        {
            //sifrelenecek = Rot13(sifrelenecek);

            //SHA1CryptoServiceProvider sifre = new SHA1CryptoServiceProvider();
            //byte[] dizi = Encoding.UTF8.GetBytes(sifrelenecek);
            //byte[] aryHash = sifre.ComputeHash(dizi);
            //StringBuilder sb = new StringBuilder();
            //foreach (byte ba in aryHash)
            //{
            //    sb.Append(ba.ToString("x2").ToLower());

            //}
            //return Rot13(sb.ToString());


            string source = Rot13(sifrelenecek);
            using (SHA1 sha1Hash = SHA1.Create())
            {
                byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
                byte[] hashBytes = sha1Hash.ComputeHash(sourceBytes);
                string hash = BitConverter.ToString(hashBytes).Replace("-", String.Empty);
                return Rot13(hash);
            }
        }

        private static string Rot13(string gelenStr)
        {
            char[] array = gelenStr.ToCharArray();
            for (int i = 0; i < array.Length; i++)
            {
                int number = (int)array[i];
                if (number >= 'a' && number <= 'z')
                {
                    if (number > 'm')
                    {
                        number -= 13;
                    }
                    else
                    {
                        number += 13;
                    }
                }
                else if (number >= 'A' && number <= 'Z')
                {
                    if (number > 'M')
                    {
                        number -= 13;
                    }
                    else
                    {
                        number += 13;
                    }
                }
                array[i] = (char)number;
            }
            gelenStr = new string(array);
            return gelenStr;
        }
    }
}
