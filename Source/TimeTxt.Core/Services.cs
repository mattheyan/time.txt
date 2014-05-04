using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.ServiceLocation;

namespace TimeTxt.Core
{
	public class Services
	{
		public static readonly IConfigurableServiceLocator Locator = new DictionaryServiceLocator();

		public static ILogWriter DefaultLogger 
		{
			get { return Locator.GetInstance<ILogWriter>(); }
		}

		private class DictionaryServiceLocator : IConfigurableServiceLocator
		{
			private readonly IDictionary<Type, object> defaultServices = new Dictionary<Type, object>();

			private readonly IDictionary<string, IDictionary<Type, object>> scopedServices = new Dictionary<string, IDictionary<Type, object>>();

			private IDictionary<Type, object> GetStorage(string key, bool create)
			{
				if (string.IsNullOrEmpty(key))
					return defaultServices;

				if (scopedServices.ContainsKey(key))
					return scopedServices[key];

				if (create)
					return scopedServices[key] = new Dictionary<Type, object>();

				throw new Exception(string.Format("Services have not been configured for '{0}'.", key));
			}

			private void UseService<TService>(TService service, string key)
			{
				var services = GetStorage(key, true);

				if (services.ContainsKey(typeof(TService)))
					throw new InvalidOperationException(string.Format("Service {0} is already implemented.", typeof(TService).Name));

				services[typeof(TService)] = service;
			}

			private object GetService(Type serviceType, string key)
			{
				var services = GetStorage(key, false);

				if (services.ContainsKey(serviceType))
					return services[serviceType];

				throw new Exception(string.Format("Service type {0} is not supported.", serviceType));
			}

			private IEnumerable<object> GetAllServices(Type serviceType)
			{
				if (defaultServices.ContainsKey(serviceType))
					yield return defaultServices[serviceType];

				foreach (var services in scopedServices)
					if (services.Value.ContainsKey(serviceType))
						yield return services.Value[serviceType];
			}

			private IEnumerable<TService> GetAllServices<TService>()
			{
				return GetAllServices(typeof (TService)).Cast<TService>();
			}

			void IConfigurableServiceLocator.UseService<TService>(TService service)
			{
				UseService(service, null);
			}

			void IConfigurableServiceLocator.UseService<TService>(TService service, string key)
			{
				UseService(service, key);
			}

			IEnumerable<TService> IServiceLocator.GetAllInstances<TService>()
			{
				return GetAllServices<TService>().Distinct();
			}

			IEnumerable<object> IServiceLocator.GetAllInstances(Type serviceType)
			{
				return GetAllServices(serviceType).Distinct();
			}

			TService IServiceLocator.GetInstance<TService>()
			{
				return (TService)GetService(typeof(TService), null);
			}

			TService IServiceLocator.GetInstance<TService>(string key)
			{
				return (TService)GetService(typeof(TService), key);
			}

			object IServiceLocator.GetInstance(Type serviceType, string key)
			{
				return GetService(serviceType, key);
			}

			object IServiceProvider.GetService(Type serviceType)
			{
				return GetService(serviceType, null);
			}

			object IServiceLocator.GetInstance(Type serviceType)
			{
				return GetService(serviceType, null);
			}
		}
	}

	public interface IConfigurableServiceLocator : IServiceLocator
	{
		void UseService<TService>(TService service);

		void UseService<TService>(TService service, string key);
	}
}
