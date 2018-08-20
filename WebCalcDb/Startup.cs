using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// requires 
// using RazorPagesMovie.Models;
using Microsoft.EntityFrameworkCore;

namespace WebCalcDb
{

	//// ASP.NET Core: Создание серверных служб для мобильных приложений https://habr.com/company/microsoft/blog/319482/

	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{

			string connection = Configuration.GetConnectionString("localdb");


			//// Добавление модели в приложение Razor Pages в ASP.NET Core
			//// https://docs.microsoft.com/ru-ru/aspnet/core/tutorials/razor-pages/model?view=aspnetcore-2.0
			//// requires 
			//// using RazorPagesMovie.Models;
			//// using Microsoft.EntityFrameworkCore;
			//services.AddDbContext<MovieContext>(options =>
			//	options.UseSqlServer(Configuration.GetConnectionString("MovieContext")));

			//// OR
			//// Подключение к базе данных в Razor Pages
			//// https://metanit.com/sharp/aspnet5/29.9.php
			//// Создание и вывод из базы данных в Razor Pages
			//// https://metanit.com/sharp/aspnet5/29.10.php
			//services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connection)); 


			//// OR
			//// Work with SQL Server LocalDB in ASP.NET Core
			/// https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/working-with-sql?view=aspnetcore-2.1&tabs=aspnetcore2x
			//services.AddDbContext<MvcMovieContext>(options =>
			//		options.UseSqlServer(Configuration.GetConnectionString("MvcMovieContext")));

			// https://metanit.com/sharp/aspnet5/29.9.php
			// https://docs.microsoft.com/ru-ru/ef/core/api/microsoft.extensions.dependencyinjection.entityframeworkservicecollectionextensions?f1url=https%3A%2F%2Fmsdn.microsoft.com%2Fquery%2Fdev15.query%3FappId%3DDev15IDEF1%26l%3DRU-RU%26k%3Dk(Microsoft.Extensions.DependencyInjection.EntityFrameworkServiceCollectionExtensions.AddDbContext%60%601)%3Bk(DevLang-csharp)%26rd%3Dtrue#methods

			services.AddMvc();

			//// Жизненный цикл зависимостей https://metanit.com/sharp/aspnet5/6.2.php
			//// Передача конфигурации через IOptions https://metanit.com/sharp/aspnet5/6.3.php
			//// Dependency Injection - Передача зависимостей https://metanit.com/sharp/aspnet5/6.4.php


			//			services.AddSingleton<IOperationRepo>(new OperationMemRepo(connection));
			services.AddSingleton<IOperationRepo>(new OperationBdRepo(connection));
			//// ИСПОЛЬЗОВАНИЕ: Singleton-объекты и scoped-сервисы
			//// https://docs.microsoft.com/ru-ru/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.0

			//// You can expose the IConfigurationRoot directly to the DI container using services.AddSingleton(Configuration).
			//// http://andrewlock.net/how-to-use-the-ioptions-pattern-for-configuration-in-asp-net-core-rc2/
			//// Доступ к зависимым инъецированным службам в MVC 6
			//// http://qaru.site/questions/2222129/accessing-dependency-injected-services-in-mvc-6
			//services.AddSingleton(Configuration);

			//// (!!) Доступ к зависимым инъецированным службам в MVC 6
			//// http://qaru.site/questions/2222129/accessing-dependency-injected-services-in-mvc-6
			//// Инъекция зависимостей с классами, отличными от класса контроллера
			//// http://askdev.info/questions/635074/dependency-injection-with-classes-other-than-a-controller-class
			// services.AddSingleton<IServiceCollection>(services);
			/*
					public class CalculationsController : Controller
					{
						private readonly IConfiguration _oCfg;
						public CalculationsController(IServiceCollection services)
						{
							_oCfg = ActivatorUtilities.CreateInstance<IConfiguration>(services);
							// etc...
						}
					}
			*/

		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseMvc();
		}
	}
}
