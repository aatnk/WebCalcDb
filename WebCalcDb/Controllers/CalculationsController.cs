using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebCalcDb.Controllers
{
	// [Route("api/calculations")]
	[Route("[controller]")]
	public class CalculationsController : Controller
	{

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
		public CalculationsController(IOperationRepo oRepo)
		{
			//// ASP.NET Core: Создание серверных служб для мобильных приложений
			//// https://habr.com/company/microsoft/blog/319482/
			//// Внедрение зависимостей в ASP.NET Core
			//// https://docs.microsoft.com/ru-ru/aspnet/core/fundamentals/dependency-injection
			this._oRepo = oRepo;
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
		public IActionResult Post([FromBody]COperation item)
		{
			_oRepo.Add(item);
			return Ok(new { Result = item.result });  //TODO: По ТЗ <<Result>> в ответе с заглавной
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
		//TODO: 2) <<GET /calculations? offset = 20 & range = 10 & operator=1 >> - какова логика пагинации - offset и range делаем по записям в базе или по отфильтрованной через <<operator>> выборке?
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
		/// *) Если в методе не указаны параметры offset и range, то возвращается полный список и код HTTP	 200, если указан диапазон, то HTTP 206  (partial content)  и список.
		/// GET /calculations? fetch = 1 & offset = 3 &operator=1 
		///   => 206 partial content [{ “operand1”: 3.25, “operand2”: 1, “operator”: “Sum”, “result”: 4.25  }]
		/// 
		/// 
		/// Если в методе не указаны параметры offset и range то возвращается код HTTP 200
		/// GET /calculations
		/// GET /calculations?operator=1&operator=2&operator=3
		///   => 200 OK [{ “operand1”: 3.25, “operand2”: 1, “operator”: “Sum”, “result”: 4.25  }, { “operand1”: 3.25, “operand2”: 1, “operator”: “Mul”, “result”: 3.25  }]
		/// GET /calculations?operator=1
		///   => 200 OK [{ “operand1”: 3.25, “operand2”: 1, “operator”: “Sum”, “result”: 4.25  }]
		///
		/// если указан диапазон, то HTTP 206 Partial Content — сервер удачно выполнил частичный GET-запрос, возвратив только часть сообщения. 
		/// В заголовке Content-Range сервер указывает байтовые диапазоны содержимого. Особое внимание при работе с подобными ответами следует уделить кэшированию. 
		/// GET /calculations? offset = 0 & range = 1 & operator=1 
		///   => 206 Partial Content  [{ “operand1”: 3.25, “operand2”: 1, “operator”: “Sum”, “result”: 4.25  }]
		/// </summary>
		/// <param name="op"></param>
		/// <returns>=> 200 OK [{},{},{}] or 206 Partial Content  [{},{},{}]</returns>
		static uint s_uCounter = 0;
		uint m_uCounter = 0;
		public IActionResult Get([Bind(Prefix = "operator")] EMathOps[] actions, uint? offset, uint? range)

		{
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

			if (offset.HasValue || range.HasValue)
			{
				s_DbgMsg += offset.HasValue ? ("; offset=" + offset.Value.ToString()) : "";
				if (!offset.HasValue) s_DbgMsg += "; (offset skipped)";

				s_DbgMsg += range.HasValue ? ("; range=" + range.Value.ToString()) : "";
				if (!range.HasValue) s_DbgMsg += "; (range skipped)";
			}
			#endregion 

			int StatusCode = (offset.HasValue || range.HasValue)? 206 : 200; // (Partial content) : (OK)
			ObjectResult res;

			if (_oRepo.Count() <= 0)
				res = new ObjectResult(new { s_DbgMsg, error = "Empty SCOperation.data" });
			else
			{
				IEnumerable<COperation> src = _oRepo.GetAll();
				IEnumerable<COperation> filtered = (actions.Length == 0) ? (src) : (src.Where<COperation>(p => -1 != Array.IndexOf<EMathOps>(actions, p.action)));
				if (!offset.HasValue)
					offset = new uint?(0);
				if (!range.HasValue)
					range = new uint?(0); // if (range==0) parce to end 
				if (offset.Value >= filtered.Count())
					return BadRequest(new { s_DbgMsg, error = "offset too lage, offset="+ offset.Value+ " and filtered.Count()=" + filtered.Count() });

				if (0 == range.Value || (offset.Value + range.Value - 1) > (uint)(filtered.Count()))
					range = new uint?((uint)(filtered.Count()) - offset.Value); // Устанавливаем диапазон в конец

				filtered = filtered.Skip((int)offset.Value).Take((int)range.Value);

				res = new ObjectResult(new { s_DbgMsg, filtered });
			}


			res.StatusCode = StatusCode;
			return res;
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

		// DELETE api/calculations/5
		/// <summary>
		/// Очистить историю вычислений
		/// DELETE /calculations => 202 Accepted
		/// </summary>
		/// <returns>202 Accepted</returns>
		[HttpDelete("{id}")]
		public IActionResult Delete()
		{
			return Accepted();
		}
	}

	/*	
		http://localhost:14590/calculations
		http://localhost:14590/calculations?operator=4
		http://localhost:14590/calculations?operator=1&operator=2&operator=3
		http://localhost:14590/calculations?range=3&offset=2
		http://localhost:14590/calculations?operator=1&operator=Sub&range=3&offset=2

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

	[Route("api/[controller]")]
	public class AABBCCController : Controller
	{
		// SCOperation.da
		public IActionResult Get()
		{
			return Accepted();
		}
	}
}
