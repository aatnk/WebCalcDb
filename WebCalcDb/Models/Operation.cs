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

// ������ ������-������� � ������� � MVC � MVC ������ ���������� �� ������ ������. � ��, ������ ������ ���� �����, � �� ������� ��� ��������.
// http://qaru.site/questions/200747/in-a-mvc-application-should-the-controller-or-the-model-handle-data-access

// �� ����������� ����������, ����� ���������� ����� ����������� � ���� �������. 
// ������ � ��� ������������� ������, ���������� �� ��� ������� ��������� �, � ��������� �������, ���� ����� ���� ������. 
// ������ ������ �������� ������, ����������� � �������� ������, ��������� �� ����������� �������������. 

// 1. ������ �������� �� ���������� ��������� ����� HTTP-���������
// 2. ������ �������� � ���� ��� ������� � �����������, ��������� ���������� � �������������� ������ ����������. 
// https://habr.com/post/175465/

// ������-������� � ���������� ������� � ������ ������ �������������� � �������, ������� ������������� ��� ���� �����:
// https://deviq.com/separation-of-concerns/
namespace WebCalcDb
{

	public enum EMathOps { Sum = 1, Sub, Mul, Div }; //TODO: �������� Sum �� Add - ����� �� ������� � Sub

	/// <summary>
	/// { �operand1�: 3.25, �operand2�: 1, �operator�: �Sum�, �result�: 4.25  }
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
				catch (Exception ex)
				{
					return double.NaN;
				}
			}
		}
	}

	public class SCOperation
	{
		//		public static List<COperation> data = new List<COperation>(64);
	}


	// ASP.NET Core: �������� ������� ���-API � �������������� ASP.NET Core MVC
	// https://habr.com/company/microsoft/blog/312878/

	public interface IOperationRepo
	{
		void Add(COperation item);
		IEnumerable<COperation> GetAll();
		void Clear();
		int Count();
	}

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

		public IEnumerable<COperation> GetAll()
		{
			return data;
		}

		public void Add(COperation item)
		{
			data.Add(item);
		}

		public void Clear()
		{
			data.Clear();
			return;
		}

		public int Count()
		{
			return data.Count;
		}
	}


	public class OperationBdRepo : IOperationRepo
	{
		private string sConnStr;
		SqlConnection oSqlConnection;

		//		public TodoRepository()
		//		{
		////			Add(new COperation { Name = "Item1" });
		//		}

		public OperationBdRepo(string sConnStr)
		{
			this.sConnStr = sConnStr;
			//var connectionString = Configuration.GetConnectionString("MovieContext")

			//// ��� � Asp Net Core ������������ � MS SQL Server � ������� ������?
			//// https://toster.ru/q/400508
			oSqlConnection = new SqlConnection(this.sConnStr);
			oSqlConnection.Open();

			//oSqlConnection.Close();
			//// ��� ������������� ������� SqlConnection ��� ������������� ���� ����������?
			//// http://qaru.site/questions/153079/how-to-force-a-sqlconnection-to-physically-close-while-using-connection-pooling
		}

		public IEnumerable<COperation> GetAll()
		{
			// https://toster.ru/q/400508
			using (SqlCommand command = new SqlCommand("SELECT * FROM dbo.WebCalc", oSqlConnection))
			{
				var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var a = reader["Column"];//������������� �������� ���������� ����� �� ������� ��
				}
			}
			return null;
		}

		public void Add(COperation item)
		{
			throw (new NotImplementedException());
			return;
		}

		public void Clear()
		{
			throw (new NotImplementedException());
			return;
		}

		public int Count()
		{
			throw (new NotImplementedException());
			//			return data.Count;
		}
	}

}