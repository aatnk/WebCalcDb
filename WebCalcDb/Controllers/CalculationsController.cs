using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace WebCalcDb.Controllers
{
	// [Route("api/calculations")]
	[Route("[controller]")]
	public class CalculationsController : Controller
	{
		/* 
		Тестирование логики контроллера в ASP.NET Core
		https://docs.microsoft.com/ru-ru/aspnet/core/mvc/controllers/testing?view=aspnetcore-2.1
		Старайтесь не возвращать бизнес-элементы непосредственно через вызовы API, так как они часто содержат больше данных, 
		чем требуется клиенту API, и связывают внутреннюю модель предметной области приложения с интерфейсом API, 
		доступным извне, чего следует избегать.Сопоставлять элементы предметной области и типы, возвращаемые по сети, 
		можно вручную (с помощью инструкции LINQ Select, как показано здесь) или с помощью такой библиотеки, как AutoMapper.
		*/
		public class Dto
		{
			public class COperationInputDto
			{
				public double operand1 { get; set; }
				public double operand2 { get; set; }
				[JsonProperty(PropertyName = "operator")]
				public EMathOps action { get; set; }
			}

			public class COperationOutputDto
			{
				public double operand1 { get; set; }
				public double operand2 { get; set; }
				[JsonProperty(PropertyName = "operator")]
				public EMathOps action { get; set; }
				public double result { get; set; }
			}
			public class CResultDto
			{
				public double Result { get; set; } // По ТЗ <<Result>> в данном ответе с заглавной
			}
		}

		//// (!!) Доступ к зависимым инъецированным службам в MVC 6
		//// http://qaru.site/questions/2222129/accessing-dependency-injected-services-in-mvc-6
		//// Инъекция зависимостей с классами, отличными от класса контроллера
		//// http://askdev.info/questions/635074/dependency-injection-with-classes-other-than-a-controller-class
		//private readonly IConfiguration _oCfg;
		//public CalculationsController(IServiceCollection services)
		//{
		//	_oCfg = ActivatorUtilities.CreateInstance<IConfiguration>(services);
		//	// etc...
		//}


		private readonly IOperationRepo _oRepo;
		private readonly ILogger<CalculationsController> _logger;

		public CalculationsController(IOperationRepo oRepo,
			ILogger<CalculationsController> logger)
		{
			//// ASP.NET Core: Создание серверных служб для мобильных приложений
			//// https://habr.com/company/microsoft/blog/319482/
			//// Внедрение зависимостей в ASP.NET Core
			//// https://docs.microsoft.com/ru-ru/aspnet/core/fundamentals/dependency-injection
			this._oRepo = oRepo;

			//// dotNET.today - Логирование
			//// http://dotnet.today/ru/aspnet5-vnext/fundamentals/logging.html
			this._logger = logger;
		}

		private ObjectResult StatusCode(object val = null, int iCode = 200, string msg = null, Exception ex = null)
		{
			string sLongLogMsg = "HTTP(" + iCode + "): " + ((null == msg) ? "(no message)" : msg);
			if (iCode < 200)
				_logger.LogInformation(ex, sLongLogMsg);
			if (200 <= iCode && iCode < 300 && (null != msg || null != ex))
				_logger.LogInformation(ex, sLongLogMsg);
			if (300 <= iCode && iCode < 400)
				_logger.LogInformation(ex, sLongLogMsg);
			if (400 <= iCode && iCode < 500)
				_logger.LogWarning(ex, sLongLogMsg);
			if (500 <= iCode)
				_logger.LogError(ex, sLongLogMsg);

			return base.StatusCode(iCode, val);
		}

		/// <summary>
		/// Выполнять операции сложения, вычитания, умножения и деления
		/// curl -X POST -H 'Content-Type: application/json' -i http://localhost:14590/calculations --data '{ "operand1": 3.25, "operand2": 2, "operator": 1 }'
		///  => 200 OK { “Result”: 4.25 }
		/// </summary>
		/// <param name="operand1"></param>
		/// <param name="operand2"></param>
		/// <param name="op"></param>
		/// <returns>200 OK { “Result”: 4.25 }</returns>
		[HttpPost]
		public IActionResult Post([FromBody]Dto.COperationInputDto itemDto)
		{
			try
			{
				if (null == itemDto)
					return StatusCode(null, 400, "the request does not contain valid data");

				var item = new COperation { operand1 = itemDto.operand1, operand2 = itemDto.operand2, action = itemDto.action };

				if (double.IsNaN(item.result))
					return StatusCode(new Dto.CResultDto { Result = item.result }, 400, "the request does not contain valid data (incorrect mathematical operation?)");

				_oRepo.Add(item);
				// По ТЗ <<Result>> в данном ответе с заглавной
				return StatusCode(new Dto.CResultDto { Result = item.result }); // 200 Ok
			}
			catch (Exception ex)
			{
				return StatusCode(null, 500, "POST fail with exception", ex);
			}
		}
		/*
				public IActionResult Post([FromBody]double operand1, [FromBody]double operand2, [FromBody] [Bind(Prefix = "operator")] EMathOps action)
				{
					COperation op = new COperation();
					op.operand1 = operand1;
					op.operand2 = operand2;
					op.action = action;
					SCOperation.data.Add(op);
					return Ok(new { Result = op.result });  //TODO: По ТЗ <<Result>> в ответе с заглавной
				}
		*/
		//TODO: 1) <<GET /calculations? fetch = 1 & offset = 3 &operator=1 >> - что за параметр fetch? Кроме данного примера более в ТЗ нигде не описан.
		//TODO: 2) <<GET /calculations? offset = 20 & fetch = 10 & operator=1 >> - какова логика пагинации - offset и fetch делаем по записям в базе или по отфильтрованной через <<operator>> выборке?
		//TODO: 3) В ТЗ operator для GET запроса описан в числовом виде <<&operator=1>> а в JSON-ответе - в символьном <<“operator”: “Sum”>>. Сохранить данное несоответствие или исправить?
		// Можно ли вопросы по ТЗ адресовать напрямую Максиму? )
		// Кому и как передавать результат? Ссылка на репозиторий в Bitbucket?(оптимально)? архив? гитхаб? 

		//TODO: [FIXED] <<operator>> - ключевое слово, нати способы его переопределения в запросе ([Bind(Prefix = "operator")]) и в ответе = [JsonProperty(PropertyName = "operator")]
		// https://stackoverflow.com/questions/36974734/fromuriattribute-replacement-in-asp-net-core-specifically-for-parameter-aliasi
		// https://ru.stackoverflow.com/questions/749157/Зачем-нужен-атрибут-jsonproperty

		//TODO: [FIXED] избавится от <<api/>> в путях запросов (Route? Роутинг)
		//TODO: [FIXED] Переименовать [controller]=values в [controller]=calculations согласно ТЗ
		// Изменяя имя класса совместно с [Route("api/[controller]")] или явным заданием вида [Route("calculations")]
		// https://forums.asp.net/t/2133164.aspx?How+to+rename+controller+

		//TODO: Добавить согласно ТЗ <<Ограничения маршрутов>>: https://metanit.com/sharp/aspnet5/11.3.php


		/// <summary>
		/// Просмотреть N последних операций по типам оператора op
		/// *) Метод должен иметь возможность принимать несколько типов операторов, например: GET /calculations? operator=1& operator=2
		/// *) Если в методе не указаны параметры offset и fetch, то возвращается полный список и код HTTP	 200, если указан диапазон, то HTTP 206  (partial content)  и список.
		/// GET /calculations? fetch = 1 & offset = 3 &operator=1 
		///   => 206 partial content [{ “operand1”: 3.25, “operand2”: 1, “operator”: “Sum”, “result”: 4.25  }]
		/// 
		/// 
		/// Если в методе не указаны параметры offset и fetch то возвращается код HTTP 200
		/// GET /calculations
		/// GET /calculations?operator=1&operator=2&operator=3
		///   => 200 OK [{ “operand1”: 3.25, “operand2”: 1, “operator”: “Sum”, “result”: 4.25  }, { “operand1”: 3.25, “operand2”: 1, “operator”: “Mul”, “result”: 3.25  }]
		/// GET /calculations?operator=1
		///   => 200 OK [{ “operand1”: 3.25, “operand2”: 1, “operator”: “Sum”, “result”: 4.25  }]
		///
		/// если указан диапазон, то HTTP 206 Partial Content — сервер удачно выполнил частичный GET-запрос, возвратив только часть сообщения. 
		/// В заголовке Content-Range сервер указывает байтовые диапазоны содержимого. Особое внимание при работе с подобными ответами следует уделить кэшированию. 
		/// GET /calculations? offset = 0 & fetch = 1 & operator=1 
		///   => 206 Partial Content  [{ “operand1”: 3.25, “operand2”: 1, “operator”: “Sum”, “result”: 4.25  }]
		/// </summary>
		/// <param name="op"></param>
		/// <returns>=> 200 OK [{},{},{}] or 206 Partial Content  [{},{},{}]</returns>
		static uint s_uCounter = 0;
		uint m_uCounter = 0;
		public IActionResult Get([Bind(Prefix = "operator")] EMathOps[] actions, uint? offset, uint? fetch)
		{
			try
			{
				actions = (null == actions ) ? ( new EMathOps[] { } ) : actions;

				#region //TODO: DelDebCode
				s_uCounter++;
				m_uCounter++;
				var s_DbgMsg = "";
				s_DbgMsg += s_uCounter + ":" + m_uCounter;

				s_DbgMsg += ("; op=" + actions.Length);

				uint op_item_counter = 0;
				foreach (var op_item in actions)
				{
					s_DbgMsg += ("; op[" + op_item_counter + "]=" + op_item);
					op_item_counter++;
				}

				if (offset.HasValue || fetch.HasValue)
				{
					s_DbgMsg += offset.HasValue ? ("; offset=" + offset.Value.ToString()) : "";
					if (!offset.HasValue) s_DbgMsg += "; (offset skipped)";

					s_DbgMsg += fetch.HasValue ? ("; fetch=" + fetch.Value.ToString()) : "";
					if (!fetch.HasValue) s_DbgMsg += "; (fetch skipped)";
				}
				#endregion

				int iHttpCode = (offset.HasValue || fetch.HasValue) ? 206 : 200; // (Partial content) : (OK)

				if (_oRepo.Count() <= 0)
					return StatusCode(null, iHttpCode, "GET - " + "empty repository");

				IEnumerable<COperation> src = _oRepo.GetAll();
				IEnumerable<COperation> filtered = (actions.Length == 0) ? (src) : (src.Where<COperation>(p => -1 != Array.IndexOf<EMathOps>(actions, p.action)));
				if (!offset.HasValue)
					offset = new uint?(0);
				if (!fetch.HasValue)
					fetch = new uint?(0); // if (fetch==0) parce to end 
				if (offset.Value >= filtered.Count())
					return StatusCode(null, 400, "GET - " + $"offset too lage, offset={offset.Value} more than {filtered.Count()} ({s_DbgMsg})");

				if (0 == fetch.Value || (offset.Value + fetch.Value - 1) > (uint)(filtered.Count()))
					fetch = new uint?((uint)(filtered.Count()) - offset.Value); // Устанавливаем диапазон в конец

				filtered = filtered.Skip((int)offset.Value).Take((int)fetch.Value);

				// https://docs.microsoft.com/ru-ru/aspnet/core/mvc/controllers/testing?view=aspnetcore-2.1
				var result = filtered.Select(item => new Dto.COperationOutputDto()
				{
					action = item.action,
					operand1 = item.operand1,
					operand2 = item.operand2,
					result = item.result
				}).ToArray();

				return StatusCode(result, iHttpCode, "GET - " + s_DbgMsg);
			}
			catch (Exception ex)
			{
				return StatusCode(null, 500, "GET fail with exception", ex);
			}
		}



		/* https://stackoverflow.com/questions/36280947/how-to-pass-multiple-parameters-to-a-get-method-in-asp-net-core
		Why not using just one controller action?

		public string Get(int? id, string firstName, string lastName, string address)
		{
		   if (id.HasValue)
			  GetById(id);
		   else if (string.IsNullOrEmpty(address))
			  GetByName(firstName, lastName);
		   else
			  GetByNameAddress(firstName, lastName, address);
		}
		Another option is to use attribute routing, but then you'd need to have a different URL format:

		//api/person/byId?id=1
		[HttpGet("byId")] 
		public string Get(int id)
		{
		}

		//api/person/byName?firstName=a&lastName=b
		[HttpGet("byName")]
		public string Get(string firstName, string lastName, string address)
		{
		}
			*/

		/// <summary>
		/// Очистить историю вычислений
		/// curl -X DELETE -H 'Content-Type: application/json' -i http://localhost:14590/calculations  
		/// => 202 Accepted
		/// </summary>
		/// <returns>202 Accepted</returns>
		[HttpDelete]
		public IActionResult Delete()
		{
			try
			{
				if (_oRepo.Clear())
					return StatusCode(null, 202, "DELETE success");
				else
					return StatusCode(null, 500, "DELETE unsuccess");
			}
			catch (Exception ex)
			{
				return StatusCode(null, 500, "DELETE fail with exception", ex);
			}
		}

		/*	
200	http://localhost:14590/calculations
200	http://localhost:14590/calculations?operator=4
200	http://localhost:14590/calculations?operator=1&operator=2&operator=3
206	http://localhost:14590/calculations?fetch=0&offset=0
206	http://localhost:14590/calculations?fetch=3&offset=0
206	http://localhost:14590/calculations?fetch=3&offset=2
206	http://localhost:14590/calculations?operator=1&operator=2&fetch=3&offset=2

400	http://localhost:14590/calculations?operator=Random
400	http://localhost:14590/calculations?fetch=3&offset=10

	filtered	
	0	
	operand1	3.25
	operand2	1
	operator	1
	result	4.25
	1	
	operand1	3.25
	operand2	1
	operator	1
	result	4.25
	2	
	operand1	3.25
	operand2	1
	operator	1
	result	4.25
	3	
	operand1	3.25
	operand2	1
	operator	1
	result	4.25
	4	
	operand1	3.25
	operand2	1
	operator	2
	result	2.25
	5	
	operand1	3.25
	operand2	1
	operator	3
	result	3.25
	6	
	operand1	3.25
	operand2	1
	operator	4
	result	3.25
	7	
	operand1	3.25
	operand2	1
	operator	5
	result	"NaN"
		*/

	}
}
