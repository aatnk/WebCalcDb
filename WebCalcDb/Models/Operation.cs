using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
namespace WebCalcDb
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

		//		public TodoMemRepo()
		//		{
		////			Add(new COperation { Name = "Item1" });
		//		}


		public OperationMemRepo(string sConnStr)
		{
			////			Add(new COperation { Name = "Item1" });
		}

		IEnumerable<COperation> IOperationRepo.GetAll()
		{
			return data;
		}

		bool IOperationRepo.Add(COperation item)
		{
			data.Add(item);
			return true;
		}

		bool IOperationRepo.Clear()
		{
			data.Clear();
			return true;
		}

		int IOperationRepo.Count()
		{
			return data.Count;
		}

	}


	public class OperationBdRepo : IOperationRepo
	{
		private string _sConnStr;
		private SqlConnection _oSqlConnection;

		//		public TodoRepository()
		//		{
		////			Add(new COperation { Name = "Item1" });
		//		}

		public OperationBdRepo(string sConnStr)
		{
			//// Свойство SqlConnection.ConnectionString
			//// https://msdn.microsoft.com/ru-ru/library/system.data.sqlclient.sqlconnection.connectionstring(v=vs.110).aspx
			this._sConnStr = sConnStr;

			//// How To Create Table with Identity Column https://stackoverflow.com/questions/10725705/how-to-create-table-with-identity-column
			//// [ID] [int] IDENTITY(1,1) NOT NULL,
			//// 14 вопросов об индексах в SQL Server, которые вы стеснялись задать https://habr.com/post/247373/#02

			//// Как в Asp Net Core подключиться к MS SQL Server и увидеть данные? https://toster.ru/q/400508
			//// Соединяться с базой каждый раз или сделать одно общее подключение? https://toster.ru/q/279429
			//// Объединение подключений в пул в SQL Server (ADO.NET) https://docs.microsoft.com/ru-ru/dotnet/framework/data/adonet/sql-server-connection-pooling
			_oSqlConnection = new SqlConnection(this._sConnStr);


			//oSqlConnection.Close();
			//// Как принудительно закрыть SqlConnection при использовании пула соединений?
			//// http://qaru.site/questions/153079/how-to-force-a-sqlconnection-to-physically-close-while-using-connection-pooling
		}

		public IEnumerable<COperation> GetAll()
		{
			var ret = new List<COperation>();
			// https://toster.ru/q/400508
			using (SqlCommand cmd = new SqlCommand("SELECT * FROM dbo.WebCalc", _oSqlConnection))
			{
				if (_oSqlConnection.State != System.Data.ConnectionState.Open)
					_oSqlConnection.Open();
				var reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					ret.Add(new COperation() { operand1 = (double) reader["operand1"], operand2 = (double) reader["operand2"], action = (EMathOps)reader["operator"] });
				}
				if (_oSqlConnection.State == System.Data.ConnectionState.Open)
					_oSqlConnection.Close();
			}
			return ret;
		}

		public bool Add(COperation item)
		{
			//try {
				//// https://ru.wikipedia.org/wiki/Insert_(SQL)
				//// Выполнить команду Insrt и вернуть вставленный идентификатор в Sql http://qaru.site/questions/94594/execute-insert-command-and-return-inserted-id-in-sql
				using (SqlCommand cmd = new SqlCommand("INSERT INTO dbo.WebCalc(operator,operand1,operand2) VALUES(@operator,@operand1,@operand2)", _oSqlConnection))
				{
					cmd.Parameters.AddWithValue("@operator", item.action);
					cmd.Parameters.AddWithValue("@operand1", item.operand1);
					cmd.Parameters.AddWithValue("@operand2", item.operand2);

					if (_oSqlConnection.State != System.Data.ConnectionState.Open)
						_oSqlConnection.Open();
					int modified = (int)cmd.ExecuteNonQuery();
					if (_oSqlConnection.State == System.Data.ConnectionState.Open)
						_oSqlConnection.Close();

					return (1 == modified);
				}
			//}	catch (SqlException ex)
			//{
			//	return false;
			//	throw;
			//}

		}


		public bool Clear()
		{
			//// https://ru.wikipedia.org/wiki/Truncate_(SQL)
			using (SqlCommand cmd = new SqlCommand("TRUNCATE TABLE dbo.WebCalc", _oSqlConnection))
			{
				if (_oSqlConnection.State != System.Data.ConnectionState.Open)
					_oSqlConnection.Open();
				int modified = (int)cmd.ExecuteNonQuery();
				if (_oSqlConnection.State == System.Data.ConnectionState.Open)
					_oSqlConnection.Close();

				return true;
			}
		}

		public int Count() { return GetAll().Count(); }
	}

}