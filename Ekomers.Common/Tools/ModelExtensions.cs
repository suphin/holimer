using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Common.Tools
{
	public static class ModelExtensions
	{
		public static string DisplayName(this object model, string propertyName)
		{
			if (model == null || string.IsNullOrEmpty(propertyName))
				return string.Empty;

			var property = model.GetType().GetProperty(propertyName);

			if (property == null)
				return propertyName; // Property bulunamazsa, property adını döndür.

			var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>();

			return displayNameAttribute != null ? displayNameAttribute.DisplayName : propertyName;
		}
	}
}
