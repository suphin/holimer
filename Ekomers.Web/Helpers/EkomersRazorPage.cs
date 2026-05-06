using AutoMapper;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Localization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ekomers.Web.Helpers
{
	public class Localizer
	{
		private readonly IStringLocalizer _localizer;

		public Localizer(IStringLocalizerFactory factory)
		{
			var type = typeof(SharedResource); // ortak resource class
			_localizer = factory.Create(type);
		}

		public string this[string key]
		{
			get => _localizer[key];
		}

		public string this[string key, params object[] args]
		{
			get => _localizer[key, args];
		}
	}

	public class SharedResource
	{
	}
	public abstract class MyRazorPage<TModel> : RazorPage<TModel>
	{
		private Localizer _localizer;

		public Localizer T
		{
			get
			{
				if (_localizer == null)
				{
					var factory = Context.RequestServices.GetService<IStringLocalizerFactory>();
					_localizer = new Localizer(factory);
				}

				return _localizer;
			}
		}
	}
}
