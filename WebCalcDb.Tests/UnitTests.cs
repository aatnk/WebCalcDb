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
using Xunit.Loging;
using System.Linq;

using static WebCalcDb.Controllers.CalculationsController;
using Newtonsoft.Json;
using System.Threading;
using WebCalcDb.Models;

namespace WebCalcDb.Tests
{
	//// ¬ообще MS рекомендует переопредел€ть DTO в тестах (NewDto), чтобы зафиксировать тестовую модель данных и исключить ложное согласование  
	using CGetTestData = CTestData<UnitTests.CGetParamsDtoTest>;
	using CPostTestData = CTestData<Dto.COperationInputDto>;

	public class CTestData<T>
	{
		public T DtoIn;
		public int expectedCode;
		public string expectedData;
		public CTestData(T DtoIn, int expectedCode, string expectedData)
		{
			this.DtoIn = DtoIn;
			this.expectedCode = expectedCode;
			this.expectedData = expectedData;
		}
	}

	public class UnitTests: BaseTest
	{
		private static object _syncLock = new object();

		const string _connection = "Server=(localdb)\\mssqllocaldb;Database=developer;Trusted_Connection=True;MultipleActiveResultSets=true";
		const bool _inmemoryTable = false;

		private readonly ILogger<UnitTests> _logger;
		private readonly ILoggerFactory _loggerFactory;

		public class CGetParamsDtoTest
		{
			[JsonProperty(PropertyName = "operator")]
			public EMathOps[] actions; //(int[] actions).Cast<EMathOps>().ToArray();
			public uint? offset;
			public uint? fetch;
		}

		public UnitTests(ITestOutputHelper output)
			: base(output)
		{
			//// ¬опрос: ядро Entity Framework: запросы журнала дл€ одного экземпл€ра контекста db
			//// Entity Framework Core: Log queries for a single db context instance
			//// https://www.programmerz.ru/questions/25714/entity-framework-core-log-queries-for-a-single-db-context-instance-question
			//// https://stackoverflow.com/questions/43424095/how-to-unit-test-with-ilogger-in-asp-net-core
			//// (c) Ilya Chumakov, 2017
			var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
			_loggerFactory = serviceProvider.GetService<ILoggerFactory>();
			_loggerFactory.AddProvider(new LoggerProvider(this));
			_logger = _loggerFactory.CreateLogger<UnitTests>();
		}

		private CalculationsController BuildController(bool NeedEmptyDB)
		{
			IOperationRepo repo;
			if (_inmemoryTable)
				repo = new OperationMemRepo("", _loggerFactory.CreateLogger<OperationMemRepo>());
			else
				repo = new OperationBdRepo(_connection, _loggerFactory.CreateLogger<OperationBdRepo>());

			CalculationsController controller = new CalculationsController(repo, _loggerFactory.CreateLogger<CalculationsController>());

			if (NeedEmptyDB)
				controller.Delete();

			return controller;
		}

		private void AssertActionResult<T>(string expectedJsonStringOfT, int expectedHttpCode, IActionResult actual, Func<T, string> selector = null) where T : class
		{
			Assert.NotNull(actual);
			Assert.IsType<ObjectResult>(actual);

			var actualResult = actual as ObjectResult;
			Assert.Equal(expectedHttpCode, actualResult.StatusCode); 

			if (null != expectedJsonStringOfT)
			{
				Assert.NotNull(actualResult.Value);
				Assert.IsType<T>(actualResult.Value);
				T actualData = (actualResult.Value as T);
				string actualString = (null == selector)?JsonConvert.SerializeObject(actualData): selector(actualData);
				_logger.LogInformation($" . . . . . expected: " + expectedJsonStringOfT);
				_logger.LogInformation($" . . . . . actual: " + actualString);
				Assert.Equal(expectedJsonStringOfT, actualString);
			}
			_logger.LogInformation($" . . . . . . . test passed. HTTP response {actualResult.StatusCode} {(System.Net.HttpStatusCode)actualResult.StatusCode}.");
		}


		//// ¬ообще MS рекомендует переопредел€ть DTO в тестах (NewDto), чтобы зафиксировать тестовую модель данных и исключить ложное согласование  
		//// Ќо мы так делать не будем тк итак очень много DTO
		CPostTestData[] TD_MethodPost_Success = new CPostTestData[] {
			new CPostTestData(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)1 }, 200, JsonConvert.SerializeObject(new { Result = 3.25+2 })),
			new CPostTestData(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)2 }, 200, JsonConvert.SerializeObject(new { Result = 3.25-2 })),
			new CPostTestData(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)3 }, 200, JsonConvert.SerializeObject(new { Result = 3.25*2 })),
			new CPostTestData(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)4 }, 200, JsonConvert.SerializeObject(new { Result = 3.25/2 })),
		};


		//// ¬ообще MS рекомендует переопредел€ть DTO в тестах (NewDto), чтобы зафиксировать тестовую модель данных и исключить ложное согласование  
		//// Ќо мы так делать не будем тк итак очень много DTO
		CPostTestData[] TD_MethodPost_Fail = new CPostTestData[] {
			new CPostTestData(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)0 }, 400, JsonConvert.SerializeObject(new { Result = double.NaN })),
			new CPostTestData(new Dto.COperationInputDto { operand1 = 3.25, operand2 = 2, action = (EMathOps)5 }, 400, JsonConvert.SerializeObject(new { Result = double.NaN })),
		};


		CGetTestData[] TD_MethodGet_EmptyRepo = new CGetTestData[] {
			new CGetTestData(new CGetParamsDtoTest { offset = null, fetch = null, actions =  new EMathOps[] {} }, 204, null),
			new CGetTestData(new CGetParamsDtoTest { offset = null, fetch = null, actions =  null }, 204, null),
		};


		//// MS рекомендует переопредел€ть DTO в тестах (NewDto), чтобы зафиксировать тестовую модель данных и исключить ложное согласование  
		//// “ут мы именно так и сделаем, хот€ это будут "поддельные" DTO дл€ хранени€ параметров
		CGetTestData[] TD_MethodGet_Success = new CGetTestData[] {
			//	new CGetTestData(new CGetParamsDtoTest { offset = 0, fetch = 0, actions =  new EMathOps[] {(EMathOps)1, (EMathOps)2, (EMathOps)3} }, 200, "[{\"operand1\":3.25,\"operand2\":2.0,\"operator\":1,\"result\":5.25},{\"operand1\":3.25,\"operand2\":2.0,\"operator\":2,\"result\":1.25},{\"operand1\":3.25,\"operand2\":2.0,\"operator\":3,\"result\":6.5}]"),
			//		1		2		3		4		
			//	{	5.25,	1.25,	6.5,	1.625	}
			new CGetTestData(new CGetParamsDtoTest {offset = null, fetch = null, actions =  new EMathOps[] {} }, 200,  JsonConvert.SerializeObject(new double[] { 1.625, 6.5, 1.25, 5.25 } ) ), // /calculations
			new CGetTestData(new CGetParamsDtoTest {offset = null, fetch = 0, actions =  new EMathOps[] {} }, 206,  JsonConvert.SerializeObject(new double[] { 1.625, 6.5, 1.25, 5.25 } ) ), // /calculations?fetch=0&offset=0
			new CGetTestData(new CGetParamsDtoTest {offset = null, fetch = null, actions =  new EMathOps[] {(EMathOps)4} }, 200,  JsonConvert.SerializeObject(new double[] {1.625} ) ), // /calculations?operator=4
			new CGetTestData(new CGetParamsDtoTest {offset = null, fetch = null, actions =  new EMathOps[] {(EMathOps)1, (EMathOps)2, (EMathOps)3} }, 200,  JsonConvert.SerializeObject(new double[] { 6.5, 1.25, 5.25 } ) ), // /calculations?operator=1&operator=2&operator=3

			new CGetTestData(new CGetParamsDtoTest {offset = 0, fetch = 2, actions =  new EMathOps[] {}  }, 206,  JsonConvert.SerializeObject(new double[] { 1.625, 6.5 } ) ), // /calculations?fetch=2&offset=0
			new CGetTestData(new CGetParamsDtoTest {offset = 2, fetch = 1, actions =  new EMathOps[] {}  }, 206,  JsonConvert.SerializeObject(new double[] { 1.25 } ) ), // /calculations?fetch=1&offset=2
			new CGetTestData(new CGetParamsDtoTest {offset = 1, fetch = 3, actions =  new EMathOps[] {}  }, 206,  JsonConvert.SerializeObject(new double[] { 6.5, 1.25, 5.25 } ) ), // /calculations?fetch=3&offset=1
			new CGetTestData(new CGetParamsDtoTest {offset = 1, fetch = 4, actions =  new EMathOps[] {}  }, 206,  JsonConvert.SerializeObject(new double[] { 6.5, 1.25, 5.25 } ) ), // /calculations?fetch=4&offset=1 !!!!
													                    
			new CGetTestData(new CGetParamsDtoTest {offset = 1, fetch = 3, actions =  new EMathOps[] {(EMathOps)1, (EMathOps)3, (EMathOps)4} }, 206,  JsonConvert.SerializeObject(new double[] { 6.5, 5.25 } ) ), // /calculations?operator=1&operator=3&operator=4&fetch=3&offset=1
		};


		//// MS рекомендует переопредел€ть DTO в тестах (NewDto), чтобы зафиксировать тестовую модель данных и исключить ложное согласование  
		//// “ут мы именно так и сделаем, хот€ это будут "поддельные" DTO дл€ хранени€ параметров
		CGetTestData[] TD_MethodGet_Fail = new CGetTestData[] {
			new CGetTestData(new CGetParamsDtoTest {offset = 3, fetch = 3, actions =  new EMathOps[] {(EMathOps)1, (EMathOps)2, (EMathOps)4} }, 204,  null ), // /calculations?operator=1&operator=2&operator=4&fetch=3&offset=1
		};


		[Fact]
		public void MethodDelete_Success()
		{
			lock (_syncLock)
			{
				// Arrange
				_logger.LogInformation($"test MethodDelete_Success()");
				CalculationsController controller = BuildController(false);

				// Act
				var actual = controller.Delete();

				// Assert
				AssertActionResult<IActionResult>(null, 202, actual); // 202 Accepted 
			}
		}


		[Fact]
		public void MethodPost_Success()
		{
			lock (_syncLock)
			{
				// Arrange
				_logger.LogInformation($"test MethodPost_Success()");
				CalculationsController controller = BuildController(false);

				try
				{
					int i = 0;
					foreach (var op in TD_MethodPost_Success)
					{
						_logger.LogInformation($"TD[{i}] = {JsonConvert.SerializeObject(op)}");
						i++;

						// Act
						var actual = controller.Post(op.DtoIn);

						// Assert
						AssertActionResult<Dto.CResultDto>(op.expectedData, op.expectedCode, actual); // 200 OK
					}
				}
				finally
				{
					var actual = controller.Delete();
				}
			}
		}

		[Fact]
		public void MethodPost_Fail()
		{
			lock (_syncLock)
			{
				// Arrange
				_logger.LogInformation($"test MethodPost_Fail()");
				CalculationsController controller = BuildController(false);

				try
				{
					int i = 0;
					foreach (var op in TD_MethodPost_Fail)
					{
						_logger.LogInformation($"TD[{i}] = {JsonConvert.SerializeObject(op)}");
						i++;

						// Act
						var actual = controller.Post(op.DtoIn);

						// Assert
						AssertActionResult<Dto.CResultDto>(op.expectedData, op.expectedCode, actual); // 400 Bad Request
					}
				}
				finally
				{
					var actual = controller.Delete();
				}
			}
		}


		[Fact]
		public void MethodGet_EmptyRepo()
		{
			lock (_syncLock)
			{
				// Arrange
				_logger.LogInformation($"test MethodGet_EmptyRepo()");
				CalculationsController controller = BuildController(true);

				int i = 0;
				foreach (var op in TD_MethodGet_EmptyRepo)
				{
					_logger.LogInformation($"TD[{i}] = {JsonConvert.SerializeObject(op)}");
					i++;

					// Act
					var actual = controller.Get(op.DtoIn.actions, op.DtoIn.offset, op.DtoIn.fetch);

					// Assert
					AssertActionResult<Dto.CResultDto>(op.expectedData, op.expectedCode, actual); // 200 OK
				}
			}
		}

		[Fact]
		public void MethodGet_Success()
		{
			lock (_syncLock)
			{
				// Arrange
				_logger.LogInformation($"test MethodGet_Success()");
				CalculationsController controller = BuildController(true);

				try
				{
					/*
								CPostTestData[] TD_MethodGet_Success_Repo = new CPostTestData[TD_MethodPost_Success.Count()*2];
								TD_MethodPost_Success.CopyTo(TD_MethodGet_Success_Repo,0);
								TD_MethodPost_Success.CopyTo(TD_MethodGet_Success_Repo, TD_MethodPost_Success.Count());
					*/
					int i0 = 0;
					foreach (var op in TD_MethodPost_Success)
					{
						// Arrange
						var actual = controller.Post(op.DtoIn);
						int iRet = (actual as ObjectResult).StatusCode.Value;
						string sCode = $"HTTP response {iRet} {(System.Net.HttpStatusCode)iRet}";
						string Mes = (200 == iRet) ? (sCode) : ("[[ERROR!]] " + sCode + (System.Net.HttpStatusCode)iRet);
						_logger.LogInformation($"Arrange[{i0}] = {op.expectedData}, {Mes}");
						i0++;
					}

					int i = 0;
					foreach (var op in TD_MethodGet_Success)
					{
						// Arrange
						_logger.LogInformation($"TD[{i}] = {JsonConvert.SerializeObject(op)}");
						i++;

						// Act
						var actual = controller.Get(op.DtoIn.actions, op.DtoIn.offset, op.DtoIn.fetch);

						// Assert
						AssertActionResult<Dto.COperationOutputDto[]>(op.expectedData, op.expectedCode, actual, arr =>
						{
							return JsonConvert.SerializeObject(arr.Select(item => item.result).ToArray());
						});  // 200 || 206
					}
				}
				finally
				{
					var actual = controller.Delete();
				}
			}
		}

		[Fact]
		public void MethodGet_Fail()
		{
			lock (_syncLock)
			{
				// Arrange
				_logger.LogInformation($"test MethodGet_Fail()");
				CalculationsController controller = BuildController(true);

				try
				{
					/*
								CPostTestData[] TD_MethodGet_Success_Repo = new CPostTestData[TD_MethodPost_Success.Count()*2];
								TD_MethodPost_Success.CopyTo(TD_MethodGet_Success_Repo,0);
								TD_MethodPost_Success.CopyTo(TD_MethodGet_Success_Repo, TD_MethodPost_Success.Count());
					*/
					int i0 = 0;
					foreach (var op in TD_MethodPost_Success)
					{
						// Arrange
						var actual = controller.Post(op.DtoIn);
						int iRet = (actual as ObjectResult).StatusCode.Value;
						string sCode = $"HTTP response {iRet} {(System.Net.HttpStatusCode)iRet}";
						string Mes = (200 == iRet) ? (sCode) : ("[[ERROR!]] " + sCode + (System.Net.HttpStatusCode)iRet);
						_logger.LogInformation($"Arrange[{i0}] = {op.expectedData}, {Mes}");
						i0++;
					}

					int i = 0;
					foreach (var op in TD_MethodGet_Fail)
					{
						// Arrange
						_logger.LogInformation($"TD[{i}] = {JsonConvert.SerializeObject(op)}");
						i++;

						// Act
						var actual = controller.Get(op.DtoIn.actions, op.DtoIn.offset, op.DtoIn.fetch);

						// Assert
						AssertActionResult<Dto.COperationOutputDto[]>(op.expectedData, op.expectedCode, actual, arr =>
						{
							return JsonConvert.SerializeObject(arr.Select(item => item.result).ToArray());
						});  // 400
					}
				}
				finally
				{
					var actual = controller.Delete();
				}
			}
		}

		//// How to write output from a unit test? https://stackoverflow.com/questions/4786884/how-to-write-output-from-a-unit-test/49767548#49767548
		//// Unit Test code called supresses Debug.WriteLine and Trace.WriteLine https://stackoverflow.com/questions/9642171/unit-test-code-called-supresses-debug-writeline-and-trace-writeline
		/*///
		[Fact]
		public void CheckConsoleOutput()
		{
			_logger.LogInformation($"test CheckConsoleOutput()");
			Console.WriteLine("01 = Console.WriteLine World");
			Trace.WriteLine("02 = Trace.WriteLine the World");
			Debug.WriteLine("03 = Debug.WriteLine WOrld");
			Output.WriteLine("04 = Output.WriteLine the World"); // print to test output window
			// TestContext.WriteLine("Message..."); 
			Debugger.Log(0, "1", "05 = Debugger.Log  the World");
			Debugger.Break();
			Assert.IsType<bool>(true);
			_logger.LogInformation($"          test passed.");
		}
		//*///
	}


}
