using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebCalcDb.Tests.IntegrationTests
{
	/// <summary>
	/// A test fixture which hosts the target project (project we wish to test) in an in-memory server.
	/// </summary>
	/// <typeparam name="TStartup">Target project's startup type</typeparam>
	public class TestFixture<TStartup> : IDisposable
	{
		private readonly TestServer _server;
		private ILogger<TestFixture<TStartup>> _logger;
		private ILoggerFactory _loggerFactory;

		public void SetupLogging(ILoggerFactory _loggerFactory)
		{
			this._loggerFactory = _loggerFactory;
			this._logger = _loggerFactory.CreateLogger<TestFixture<TStartup>>();
		}

		public TestFixture()
			: this(Path.Combine("src"))
		{
		}

		protected TestFixture(string relativeTargetProjectParentDir)
		{
			var startupAssembly = typeof(TStartup).GetTypeInfo().Assembly;
			// var contentRoot = GetProjectPath(relativeTargetProjectParentDir, startupAssembly); // Доступ к представлениям

			var builder = new WebHostBuilder()
				.ConfigureAppConfiguration((hostContext, configApp) => // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.1#configureappconfiguration
				{
					//TODO: сделать тут хорошо и надёжно определять пути к "appsettings*.json"
					//TODO: сейчас сделано через копирование "appsettings.json" при компиляции (свойства файла проекта), но почему-то не во всех случаях работает

					configApp.SetBasePath(Directory.GetCurrentDirectory());
					configApp.AddJsonFile("appsettings.json", optional: true); //  "appsettings.json"
					configApp.AddJsonFile(
						$"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", //  "appsettings.Development.json"
						optional: true);
//					configApp.AddEnvironmentVariables(prefix: "PREFIX_");
//					configApp.AddCommandLine(args);
				})
				.ConfigureLogging((hostContext, configLogging) => // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.1#configurelogging
				{
					configLogging.AddConsole();
					configLogging.AddDebug();
				})
				// .UseContentRoot(contentRoot) // Доступ к представлениям
				.UseContentRoot(@"W:\!  !  PROJECTS  !  !\!   ===  СПРАВОЧНИКИ ===\ASP dotNET CORE\WebCalcDb\WebCalcDb") // Доступ к представлениям
				.ConfigureServices(InitializeServices)
				.UseEnvironment("Development")
				.UseStartup(typeof(TStartup));


			_server = new TestServer(builder);

			Client = _server.CreateClient();
			Client.BaseAddress = new Uri("http://localhost");
		}

		public HttpClient Client { get; }

		public void Dispose()
		{
			Client.Dispose();
			_server.Dispose();
		}

		protected virtual void InitializeServices(IServiceCollection services)
		{
			var startupAssembly = typeof(TStartup).GetTypeInfo().Assembly;

			// Inject a custom application part manager. 
			// Overrides AddMvcCore() because it uses TryAdd().
			var manager = new ApplicationPartManager();
			manager.ApplicationParts.Add(new AssemblyPart(startupAssembly));
			manager.FeatureProviders.Add(new ControllerFeatureProvider());
			manager.FeatureProviders.Add(new ViewComponentFeatureProvider());

			services.AddSingleton(manager);
		}

		/// <summary>
		/// Gets the full path to the target project that we wish to test
		/// </summary>
		/// <param name="projectRelativePath">
		/// The parent directory of the target project.
		/// e.g. src, samples, test, or test/Websites
		/// </param>
		/// <param name="startupAssembly">The target project's assembly.</param>
		/// <returns>The full path to the target project.</returns>
		private static string GetProjectPath(string projectRelativePath, Assembly startupAssembly)
		{
			// Get name of the target project which we want to test
			var projectName = startupAssembly.GetName().Name;

			// Get currently executing test project path
			var applicationBasePath = System.AppContext.BaseDirectory;

			// Find the path to the target project
			var directoryInfo = new DirectoryInfo(applicationBasePath);
			do
			{
				directoryInfo = directoryInfo.Parent;

				var projectDirectoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, projectRelativePath));
				if (projectDirectoryInfo.Exists)
				{
					var projectFileInfo = new FileInfo(Path.Combine(projectDirectoryInfo.FullName, projectName, $"{projectName}.csproj"));
					if (projectFileInfo.Exists)
					{
						return Path.Combine(projectDirectoryInfo.FullName, projectName);
					}
				}
			}
			while (directoryInfo.Parent != null);

			throw new Exception($"Project root could not be located using the application root {applicationBasePath}.");
		}
	}
}