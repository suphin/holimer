using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography; 
namespace Ekomers.Common.Services
{
	

	public static class CryptoHelper
	{
		private static readonly string key = "12345678901234567890123456789012";
		private static readonly string iv = "1234567890123456";

		public static string Encrypt(string plainText)
		{
			using Aes aes = Aes.Create();

			aes.Key = Encoding.UTF8.GetBytes(key);
			aes.IV = Encoding.UTF8.GetBytes(iv);

			ICryptoTransform encryptor = aes.CreateEncryptor();

			byte[] inputBuffer = Encoding.UTF8.GetBytes(plainText);

			byte[] encrypted = encryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);

			return Convert.ToBase64String(encrypted);
		}

		public static string Decrypt(string cipherText)
		{
			using Aes aes = Aes.Create();

			aes.Key = Encoding.UTF8.GetBytes(key);
			aes.IV = Encoding.UTF8.GetBytes(iv);

			ICryptoTransform decryptor = aes.CreateDecryptor();

			byte[] cipherBuffer = Convert.FromBase64String(cipherText);

			byte[] decrypted = decryptor.TransformFinalBlock(cipherBuffer, 0, cipherBuffer.Length);

			return Encoding.UTF8.GetString(decrypted);
		}
	}
}
