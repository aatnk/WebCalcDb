using System;
using WebCalcDb.Controllers;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using static WebCalcDb.Controllers.CalculationsController;
using Xunit.Loging;

namespace WebCalcDb.Tests
{
	public class UnitTests: BaseTest
	{
		// const string _connection = "Server=(localdb)\\mssqllocaldb;Database=developer;Trusted_Connection=True;MultipleActiveResultSets=true";
		private readonly ILogger<UnitTests> _logger;
		private readonly ILoggerFactory _loggerFactory;

		public UnitTests(ITestOutputHelper output)
			: base(output)
		{
			var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
			_loggerFactory = serviceProvider.GetService<ILoggerFactory>();
			_loggerFactory.AddProvider(new LoggerProvider(this));
			_logger = _loggerFactory.CreateLogger<UnitTests>();
		}

		KeyValuePair<Dto.COperationDto, Dto.CResultDto>[] opsSuccess = new KeyValuePair<Dto.COperationDto, Dto.CResultDto>[4] {
			new KeyValuePair<Dto.COperationDto, Dto.CResultDto>(new Dto.COperationDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)1 }, new Dto.CResultDto { Result = 3.25+2 } ),
			new KeyValuePair<Dto.COperationDto, Dto.CResultDto>(new Dto.COperationDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)2 }, new Dto.CResultDto { Result = 3.25-2 } ),
			new KeyValuePair<Dto.COperationDto, Dto.CResultDto>(new Dto.COperationDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)3 }, new Dto.CResultDto { Result = 3.25*2 } ),
			new KeyValuePair<Dto.COperationDto, Dto.CResultDto>(new Dto.COperationDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)4 }, new Dto.CResultDto { Result = 3.25/2 } ),
		};

		KeyValuePair<Dto.COperationDto, Dto.CResultDto>[] opsFail = new KeyValuePair<Dto.COperationDto, Dto.CResultDto>[2] {
			new KeyValuePair<Dto.COperationDto, Dto.CResultDto>(new Dto.COperationDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)0 }, new Dto.CResultDto { Result = double.NaN } ),
			new KeyValuePair<Dto.COperationDto, Dto.CResultDto>(new Dto.COperationDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)5 }, new Dto.CResultDto { Result = double.NaN } ),
		};

		[Fact]
		public void MathOpsSuccess()
		{
			// Arrange
			var repo = new OperationMemRepo("");
			// var repo = new OperationBdRepo(connection);
			var controller = new CalculationsController(repo, _loggerFactory.CreateLogger<CalculationsController>());

			foreach (var op in opsSuccess)
			{
				// Act
				var result = controller.Post(op.Key);

				// Assert
				AssertPost(op.Value, 200, result); // 200 OK
			}
		}

		[Fact]
		public void MathOpsFail()
		{
			// Arrange
			var repo = new OperationMemRepo("");
			// var repo = new OperationBdRepo(connection);
			var controller = new CalculationsController(repo, _loggerFactory.CreateLogger<CalculationsController>());

			foreach (var op in opsFail)
			{
				// Act
				var result = controller.Post(op.Key);

				// Assert
				AssertPost(op.Value, 400, result);  // 400 Bad Request
			}
		}

		private static void AssertPost(Dto.CResultDto expected, int expectedHttpCode, IActionResult actual)
		{
			Assert.NotNull(actual);
			Assert.IsType<ObjectResult>(actual);

			var actualResult = actual as ObjectResult;
			Assert.Equal(expectedHttpCode, actualResult.StatusCode); // 200 OK, 400 Bad Request
			Assert.NotNull(actualResult.Value);
			Assert.IsType<Dto.CResultDto>(actualResult.Value);
			Assert.Equal(expected.Result, (actualResult.Value as Dto.CResultDto).Result);
		}


		///*
		//// How to write output from a unit test? https://stackoverflow.com/questions/4786884/how-to-write-output-from-a-unit-test/49767548#49767548
		//// Unit Test code called supresses Debug.WriteLine and Trace.WriteLine https://stackoverflow.com/questions/9642171/unit-test-code-called-supresses-debug-writeline-and-trace-writeline
		//// Создание тестового DB-контекста в тестах с использованием xUnit https://habr.com/post/265501/
		[Fact]
		public void CheckConsoleOutput()
		{
			Console.WriteLine("01 = Console.WriteLine World");
			Trace.WriteLine("02 = Trace.WriteLine the World");
			Debug.WriteLine("03 = Debug.WriteLine WOrld");
			Output.WriteLine("04 = Output.WriteLine the World");
			// TestContext.WriteLine("Message..."); 
			Debugger.Log(0, "1", "05 = Debugger.Log  the World");
			Debugger.Break();
			Assert.IsType<bool>(true);
		}
		//*/
	}

	/*
	public class PrimeWebDefaultRequestShould
	{
		private readonly TestServer _server;
		private readonly HttpClient _client;

		public PrimeWebDefaultRequestShould()
		{
			// Arrange
			_server = new TestServer(new WebHostBuilder()
			   .UseStartup<Startup>());
			_client = _server.CreateClient();
		}

		[Fact]
		public async Task ReturnHelloWorld()
		{
			// Act
			var response = await _client.GetAsync("/");
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
			// Assert
			Assert.Equal("Hello World!", responseString);
		}
	}
	//*/
}
