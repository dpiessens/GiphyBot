using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using GiphyDotNet.Manager;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Practices.Unity;
using Unity.WebApi;

namespace GiphyBot
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Json settings
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            // Web API configuration and services
            config.DependencyResolver = RegisterServices();

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                "DefaultApi",
                "api/{controller}/{id}",
                new { id = RouteParameter.Optional }
            );

            config.Services.Add(typeof(IExceptionLogger), new AiExceptionLogger());
        }

        public static UnityDependencyResolver RegisterServices()
        {
            var container = new UnityContainer();

            // register all your components with the container here
            // it is NOT necessary to register your controllers

            // e.g. container.RegisterType<ITestService, TestService>();
            var apiKey = System.Configuration.ConfigurationManager.AppSettings["Giphy.ApiKey"];
            var giphyInstance = new Giphy(apiKey);
            container.RegisterInstance(giphyInstance, new ExternallyControlledLifetimeManager());
            container.RegisterInstance(new TelemetryClient(TelemetryConfiguration.Active));
            return new UnityDependencyResolver(container);
        }

        public class AiExceptionLogger : ExceptionLogger
        {
            public override void Log(ExceptionLoggerContext context)
            {
                if (context != null && context.Exception != null)
                {//or reuse instance (recommended!). see note above 
                    var ai = new TelemetryClient();
                    ai.TrackException(context.Exception);
                }
                base.Log(context);
            }
        }
    }
}
