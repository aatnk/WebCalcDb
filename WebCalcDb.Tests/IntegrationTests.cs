using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.TestHost; // Microsoft.AspNetCore.TestHost.dll
using Microsoft.Extensions.DependencyInjection;

using System.Threading.Tasks;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Xunit;

using WebCalcDb.Tests.IntegrationTests;

using static WebCalcDb.Controllers.CalculationsController;
using Xunit.Loging;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using WebCalcDb.Models;
//// Тестирование логики контроллера в ASP.NET Core - Тестирование интеграции
//// aspnetcore-2.1 https://docs.microsoft.com/ru-ru/aspnet/core/mvc/controllers/testing?view=aspnetcore-2.1#integration-testing
//// aspnetcore-2.0 https://docs.microsoft.com/ru-ru/aspnet/core/mvc/controllers/testing?view=aspnetcore-2.0#integration-testing
//// GITHUB https://github.com/aspnet/Docs/tree/master/aspnetcore/mvc/controllers/testing/sample/TestingControllersSample/tests/TestingControllersSample.Tests
//// Создание тестового DB-контекста в тестах с использованием xUnit https://habr.com/post/265501/
namespace WebCalcDb.Tests
{
	public class ApiIntegrationTests : BaseTest, IClassFixture<TestFixture<WebCalcDb.Startup>>
	{
		internal class NewIdeaDto
		{
			public NewIdeaDto(string name, string description, int sessionId)
			{
				Name = name;
				Description = description;
				SessionId = sessionId;
			}

			public string Name { get; set; }
			public string Description { get; set; }
			public int SessionId { get; set; }
		}

		//// Вообще MS рекомендует переопределять DTO в тестах (NewDto), чтобы зафиксировать тестовую модель данных и исключить ложное согласование  
		//// Но мы так делать не будем тк итак очень много DTO
		readonly KeyValuePair<Dto.COperationInputDto, Dto.CResultDto>[] opsSuccess = new KeyValuePair<Dto.COperationInputDto, Dto.CResultDto>[] {
			new KeyValuePair<Dto.COperationInputDto, Dto.CResultDto>(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)1 }, new Dto.CResultDto { Result = 3.25+2 } ),
			new KeyValuePair<Dto.COperationInputDto, Dto.CResultDto>(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)2 }, new Dto.CResultDto { Result = 3.25-2 } ),
			new KeyValuePair<Dto.COperationInputDto, Dto.CResultDto>(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)3 }, new Dto.CResultDto { Result = 3.25*2 } ),
			new KeyValuePair<Dto.COperationInputDto, Dto.CResultDto>(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)4 }, new Dto.CResultDto { Result = 3.25/2 } ),

			new KeyValuePair<Dto.COperationInputDto, Dto.CResultDto>(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)3 }, new Dto.CResultDto { Result = 3.25*2 } ),
			new KeyValuePair<Dto.COperationInputDto, Dto.CResultDto>(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)4 }, new Dto.CResultDto { Result = 3.25/2 } ),
		};


		private readonly HttpClient _client;

		private readonly ILogger<ApiIntegrationTests> _logger;
		private readonly ILoggerFactory _loggerFactory;

		public ApiIntegrationTests(ITestOutputHelper output, TestFixture<WebCalcDb.Startup> fixture)
			: base(output)
		{
			//// Вопрос: Ядро Entity Framework: запросы журнала для одного экземпляра контекста db
			//// Entity Framework Core: Log queries for a single db context instance
			//// https://www.programmerz.ru/questions/25714/entity-framework-core-log-queries-for-a-single-db-context-instance-question
			//// https://stackoverflow.com/questions/43424095/how-to-unit-test-with-ilogger-in-asp-net-core
			//// (c) Ilya Chumakov, 2017
			var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
			_loggerFactory = serviceProvider.GetService<ILoggerFactory>();
			_loggerFactory.AddProvider(new LoggerProvider(this));
			_logger = _loggerFactory.CreateLogger<ApiIntegrationTests>();
			fixture.SetupLogging(_loggerFactory);

			_client = fixture.Client;
		}

		private async Task AssertResponseAsync<T>(T expected, int expectedHttpCode, HttpResponseMessage actual)
		{
			Assert.NotNull(actual);
			Assert.Equal(expectedHttpCode, (int)actual.StatusCode); // 200 OK, 400 Bad Request

			if (null!= expected)
			{
				Assert.NotNull(actual.Content);
				T actualData = await actual.Content.ReadAsJsonAsync<T>();
				Assert.NotNull(actualData);
				Assert.Equal(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(actualData) );
			}
			_logger.LogInformation($"          passed ({expectedHttpCode}).");
		}

		
		/*///

		[Fact]
		public async Task CreateSuccess()
		{
			// Arrange
			Assert.NotNull(_client);
			Assert.IsType<HttpClient>(_client);

			int i = 0;
			foreach (var op in opsSuccess)
			{

				_logger.LogInformation($"CreateSuccess() opsSuccess[{i}] => {JsonConvert.SerializeObject(op)}");
				Assert.NotNull(op.Key);
				Assert.IsType<Dto.COperationInputDto>(op.Key);
				Assert.NotNull(op.Value);
				Assert.IsType<Dto.CResultDto>(op.Value);

				// Act
				HttpResponseMessage actual = await _client.PostAsJsonAsync("/calculations", op.Key);

				// Assert
				await AssertResponseAsync(op.Value, 200, actual); // 200 OK
			}
		}
		/*///

		/*///
		[Fact]
		public async Task CreatePostReturnsBadRequestForMissingDescriptionValue()
		{
			// Arrange
			var newIdea = new NewIdeaDto("Name", "", 1);

			// Act
			var response = await _client.PostAsJsonAsync("/api/ideas/create", newIdea);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task CreatePostReturnsBadRequestForSessionIdValueTooSmall()
		{
			// Arrange
			var newIdea = new NewIdeaDto("Name", "Description", 0);

			// Act
			var response = await _client.PostAsJsonAsync("/api/ideas/create", newIdea);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task CreatePostReturnsBadRequestForSessionIdValueTooLarge()
		{
			// Arrange
			var newIdea = new NewIdeaDto("Name", "Description", 1000001);

			// Act
			var response = await _client.PostAsJsonAsync("/api/ideas/create", newIdea);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task CreatePostReturnsNotFoundForInvalidSession()
		{
			// Arrange
			var newIdea = new NewIdeaDto("Name", "Description", 123);

			// Act
			var response = await _client.PostAsJsonAsync("/api/ideas/create", newIdea);

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		[Fact]
		public async Task CreatePostReturnsCreatedIdeaWithCorrectInputs()
		{
			// Arrange
			var testIdeaName = Guid.NewGuid().ToString();
			var newIdea = new NewIdeaDto(testIdeaName, "Description", 1);

			// Act
			var response = await _client.PostAsJsonAsync("/api/ideas/create", newIdea);

			// Assert
			response.EnsureSuccessStatusCode();
			var returnedSession = await response.Content.ReadAsJsonAsync<BrainstormSession>();
			Assert.Equal(2, returnedSession.Ideas.Count);
			Assert.Contains(testIdeaName, returnedSession.Ideas.Select(i => i.Name).ToList());
		}

		[Fact]
		public async Task ForSessionReturnsNotFoundForBadSessionId()
		{
			// Arrange & Act
			var response = await _client.GetAsync("/api/ideas/forsession/500");

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		[Fact]
		public async Task ForSessionReturnsIdeasForValidSessionId()
		{
			// Arrange
			// var testSession = Startup.GetTestSession();

			// Act
			var response = await _client.GetAsync("/api/ideas/forsession/1");

			// Assert
			response.EnsureSuccessStatusCode();
			var ideaList = JsonConvert.DeserializeObject<List<NewIdeaDto>>(
				await response.Content.ReadAsStringAsync());
			var firstIdea = ideaList.First();
			Assert.Equal(testSession.Ideas.First().Name, firstIdea.Name);
		}
//*///
	}
}
