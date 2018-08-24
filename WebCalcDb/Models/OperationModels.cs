using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WebCalcDb.Controllers;

// Логика бизнес-бизнеса в моделях с MVC и MVC должна находиться на уровне модели. И да, модель должна быть слоем, а не классом или объектом.
// http://qaru.site/questions/200747/in-a-mvc-application-should-the-controller-or-the-model-handle-data-access

// За исключением интерфейса, любое приложение можно представить в виде моделей. 
// Именно в них сосредоточены данные, основанные на них правила поведения и, в некоторых случаях, даже вывод этих данных. 
// Именно модель способна понять, истолковать и изложить данные, обеспечив им осмысленное использование. 

// 1. Модель отвечает за сохранения состояния между HTTP-запросами
// 2. Модель включает в себя все правила и ограничения, управляет поведением и использованием данной информации. 
// https://habr.com/post/175465/

// Бизнес-правила и реализация доступа к данным должны обрабатываться в классах, которые предназначены для этих целей:
// https://deviq.com/separation-of-concerns/
namespace WebCalcDb.Models
{

	public enum EMathOps { Sum = 1, Sub, Mul, Div }; //TODO: Заменить Sum на Add - чтобы не путался с Sub

	//// Синтаксис добавления ограничения. Параметр ON DELETE CASCADE - удаляет дочерние строки при удалении родительского ключа
	//// http://sql-oracle.ru/sintaksis-dobavleniya-ogranicheniya-parametr-on-delete-cascade.html

	/// <summary>
	/// { “operand1”: 3.25, “operand2”: 1, “operator”: “Sum”, “result”: 4.25  }
	/// </summary>
	public class COperation
	{
		public double operand1 { get; set; }
		public double operand2 { get; set; }
		[JsonProperty(PropertyName = "operator")]
		public EMathOps action { get; set; }
		public double result
		{
			get
			{
				try
				{
					switch (action)
					{
						case EMathOps.Sum: return operand1 + operand2;
						case EMathOps.Sub: return operand1 - operand2;
						case EMathOps.Mul: return operand1 * operand2;
						case EMathOps.Div: return operand1 / operand2;
						default: return double.NaN;
					}
				}
				catch (Exception)
				{
					return double.NaN;
				}
			}
		}
	}

	/// <summary>
	/// { "Result": 4.25  }
	/// </summary>
	public class CResult
	{
		public double Result	{ get; set; }
	}

	// ASP.NET Core: Создание первого веб-API с использованием ASP.NET Core MVC
	// https://habr.com/company/microsoft/blog/312878/

	public interface IOperationRepo
	{
		bool Add(COperation item);
		IEnumerable<COperation> GetAll();
		bool Clear();
		int Count();
		IEnumerable<COperation> GetPage(EMathOps[] actions, uint offset, uint fetch, bool reverse);
		bool Empty();
	}

	//public abstract class BaseOperationRepo : IOperationRepo
	//{
	//	public abstract void Add(COperation item);
	//	public abstract void Clear();
	//	public abstract IEnumerable<COperation> GetAll();

	//	public int Count()	{ return GetAll().Count(); }

	//}

	public class OperationMemRepo : IOperationRepo
	{
		private List<COperation> data = new List<COperation>();
		ILogger<OperationMemRepo> _logger;

		public OperationMemRepo(string sConnStr, ILogger<OperationMemRepo> logger)
		{
			this._logger = logger;
		}
		public OperationMemRepo(string sConnStr)
		{
		}

		public IEnumerable<COperation> GetAll()
		{
			return data;
		}

		public bool Add(COperation item)
		{
			data.Add(item);
			return true;
		}

		public bool Clear()
		{
			data.Clear();
			return true;
		}

		public int Count()
		{
			return data.Count;
		}

		public IEnumerable<COperation> GetPage(EMathOps[] actions, uint offset, uint fetch, bool reverse)
		{
			actions = (null == actions) ? (new EMathOps[] { }) : actions;

			IEnumerable<COperation> src = this.GetAll();
			if (reverse) src = src.Reverse();
			IEnumerable<COperation> filtered = (actions.Length == 0) ? (src) : (src.Where<COperation>(p => -1 != Array.IndexOf<EMathOps>(actions, p.action)));

			if (offset >= filtered.Count())
				return new COperation[0];

			if (0 == fetch || (offset + fetch - 1) > (uint)(filtered.Count()))
				fetch = (uint)(filtered.Count() - offset); // Устанавливаем диапазон в конец

			filtered = filtered.Skip((int)offset).Take((int)fetch);

			return filtered;
		}

		public bool Empty()
		{
			return 0==Count();
		}
	}


	public class OperationBdRepo : IOperationRepo
	{
		CSqlConnector _bd;
		ILogger<OperationBdRepo> _logger;

		public OperationBdRepo(string sConnStr, ILogger<OperationBdRepo> logger)
		{
			this._logger = logger;
			//// Свойство SqlConnection.ConnectionString
			//// https://msdn.microsoft.com/ru-ru/library/system.data.sqlclient.sqlconnection.connectionstring(v=vs.110).aspx

			//// How To Create Table with Identity Column https://stackoverflow.com/questions/10725705/how-to-create-table-with-identity-column
			//// [ID] [int] IDENTITY(1,1) NOT NULL,
			//// 14 вопросов об индексах в SQL Server, которые вы стеснялись задать https://habr.com/post/247373/#02

			//// Как в Asp Net Core подключиться к MS SQL Server и увидеть данные? https://toster.ru/q/400508
			//// Соединяться с базой каждый раз или сделать одно общее подключение? https://toster.ru/q/279429
			//// Объединение подключений в пул в SQL Server (ADO.NET) https://docs.microsoft.com/ru-ru/dotnet/framework/data/adonet/sql-server-connection-pooling

			_bd = new CSqlConnector(sConnStr);

			//oSqlConnection.Close();
			//// Как принудительно закрыть SqlConnection при использовании пула соединений?
			//// http://qaru.site/questions/153079/how-to-force-a-sqlconnection-to-physically-close-while-using-connection-pooling
		}

		public OperationBdRepo(string sConnStr)
		{
			_bd = new CSqlConnector(sConnStr);
		}

		public IEnumerable<COperation> GetAll()
		{
			List<COperation> ret = _bd.ExecuteReader<COperation>("SELECT * FROM [dbo].[WebCalc]", CommandType.Text, reader =>
			{
				return new COperation() {
					operand1 = (double)reader["operand1"],
					operand2 = (double)reader["operand2"],
					action = (EMathOps)reader["operator"],
				};
			});
			return ret;
		}

		public bool Add(COperation item)
		{
			//try {
			//// https://ru.wikipedia.org/wiki/Insert_(SQL)
			//// Выполнить команду Insrt и вернуть вставленный идентификатор в Sql http://qaru.site/questions/94594/execute-insert-command-and-return-inserted-id-in-sql

			SqlParameter[] parameters = new SqlParameter[] {
				new SqlParameter("@operator", item.action),
				new SqlParameter("@operand1", item.operand1),
				new SqlParameter("@operand2", item.operand2),
				};

			int modified = _bd.ExecuteNonQuery("INSERT INTO [dbo].[WebCalc](operator,operand1,operand2) VALUES(@operator,@operand1,@operand2)",
				CommandType.Text, parameters);

			return (1 == modified);
		}


		public bool Clear()
		{

			//// AFAIK: For UPDATE, INSERT, and DELETE statements, ExecuteNonQuery() returns the number of rows affected by the command.
			//// For all other types of statements, the return value is -1.
			//// And here you are using truncate not UPDATE, INSERT, and DELETE.
			//// https://www.codeproject.com/Questions/247906/Sql-Truncate-Problem
			//// https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.executenonquery?redirectedfrom=MSDN&view=netframework-4.7.2#System_Data_SqlClient_SqlCommand_ExecuteNonQuery
			int ret = _bd.ExecuteNonQuery("TRUNCATE TABLE [dbo].[WebCalc]", CommandType.Text);
			return (-1 == ret);
		}

		public int Count()
		{
			object ret = _bd.ExecuteScalar("SELECT COUNT(*) FROM [dbo].[WebCalc]", CommandType.Text);
			return Convert.ToInt32(ret);
		}

		public IEnumerable<COperation> GetPage2(EMathOps[] actions, uint offset, uint fetch, bool reverse)
		{
			actions = (null == actions) ? (new EMathOps[] { }) : actions;

			IEnumerable<COperation> src = this.GetAll();
			if (reverse) src = src.Reverse();
			IEnumerable<COperation> filtered = (actions.Length == 0) ? (src) : (src.Where<COperation>(p => -1 != Array.IndexOf<EMathOps>(actions, p.action)));

			if (offset >= filtered.Count())
				return null; // StatusCode(null, 400, "GET - " + $"offset too lage, offset={offset.Value} more than {filtered.Count()} ({s_DbgMsg})");

			if (0 == fetch || (offset + fetch - 1) > (uint)(filtered.Count()))
				fetch = (uint)(filtered.Count() - offset); // Устанавливаем диапазон в конец

			filtered = filtered.Skip((int)offset).Take((int)fetch);

			return filtered;
		}

		public IEnumerable<COperation> GetPage(EMathOps[] actions, uint offset, uint fetch, bool reverse)
		{
			actions = (null == actions) ? (new EMathOps[] { }) : actions;

			SqlParameter[] parameters = new SqlParameter[] {
				new SqlParameter("@offset", (Int64) offset),
				new SqlParameter("@fetch", (Int64) fetch),
				};

			string Where = (0 < actions.Length) ? (" WHERE [operator] IN (" + string.Join(',', actions.Select(a=>(int)a)) + ") ") : ("");
			string Limit = (0 != fetch) ? (" FETCH NEXT (@fetch) ROWS ONLY ") : ("");
			string Order = reverse ? " DESC " : " ASC ";
			//string Query = "SELECT * FROM [dbo].[WebCalc] " + Where + " ORDER BY ID DESC SKIP @offset " + Limit;
			// SELECT * FROM [dbo].[WebCalc]  WHERE [operator] IN (1,2,4) ORDER BY [ID] ASC OFFSET 1 ROWS FETCH NEXT 10 ROWS ONLY;
			// SELECT * FROM [dbo].[WebCalc]  WHERE [operator] IN (1,2,4) ORDER BY [ID] ASC OFFSET 1 ROWS
			// SELECT * FROM [dbo].[WebCalc] ORDER BY [ID] ASC OFFSET 1 ROWS
			// SELECT * FROM [dbo].[WebCalc] ORDER BY [ID] ASC 
			// SELECT * FROM [dbo].[WebCalc] ORDER BY [ID] DESC
			string Query = "SELECT * FROM [dbo].[WebCalc] " + Where + " ORDER BY [ID] "+ Order+" OFFSET (@offset) ROWS " + Limit;

			//DECLARE @Skip INT = 2, @Take INT = 2
			//SELECT* FROM TABLE_NAME
			//ORDER BY ID ASC
			//OFFSET(@Skip) ROWS FETCH NEXT(@Take) ROWS ONLY

			if (null!=_logger) _logger.LogError(Query);

			// Постраничная навигация с MySQL при большом количестве записей
			// https://habr.com/post/44608/
			// _bd.ExecuteReader<COperation>("SELECT * FROM [dbo].[WebCalc] WHERE operator IN (1, 2, 3) ORDER BY ID DESC SKIP @offset LIMIT @fetch", CommandType.Text, reader =>
			List <COperation> ret = _bd.ExecuteReader<COperation>(Query, CommandType.Text, reader =>
			{
				return new COperation()
				{
					operand1 = (double)reader["operand1"],
					operand2 = (double)reader["operand2"],
					action = (EMathOps)reader["operator"],
				};
			}, parameters);

			return ret;
		}

		public bool Empty()
		{
			// IF...ELSE (Transact-SQL) https://msdn.microsoft.com/ru-ru/library/ms182717(v=sql.120)
			// WHILE (Transact-SQL) https://msdn.microsoft.com/ru-ru/library/ms178642(v=sql.120)
			// SELECT(Transact - SQL) https://msdn.microsoft.com/ru-ru/library/ms189499(v=sql.120)
			object ret = _bd.ExecuteScalar("IF 0 = ( SELECT COUNT(*) FROM [dbo].[WebCalc] ) SELECT 1 ELSE SELECT 0", CommandType.Text);
			return Convert.ToBoolean(ret);
		}
	}

}