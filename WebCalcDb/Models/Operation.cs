using System;
using System.Collections.Generic;
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

namespace WebCalcDb
{
	public enum EMathOps { Sum = 1, Sub, Mul, Div }; //TODO: «аменить Sum на Add - чтобы не путалс€ с Sub

	/// <summary>
	/// { Уoperand1Ф: 3.25, Уoperand2Ф: 1, УoperatorФ: УSumФ, УresultФ: 4.25  }
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
				}catch(Exception ex)
				{
					return double.NaN;
				}
			}
		}
	}

	public static class SCOperation
	{
		public static List<COperation> data = new List<COperation>(64);
		public static LoadFromDb()
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				connection.Open();
				using (SqlCommand command = new SqlCommand("SELECT * FROM dbo.Table", connection))
				{
					var reader = command.ExecuteReader();
					while (reader.Read())
					{
						var a = reader["Column"];//инициализаци€ значени€ переменной полем из таблицы Ѕƒ
					}
				}
			}

		}
	}

	/// <summary>
	/// ID	UTC	(ms)	IP	operator(char[1] or Enum)	operand1	operand2
	/// </summary>
	public class COperationDb: COperation
	{
		public uint _id { get; set; }
		public DateTime utc { get; set; }
		public System.Net.IPAddress fromIP{ get; set; }
	}


	/*
	public class Movie
	{
		public int ID { get; set; }
		public string Title { get; set; }
		public DateTime ReleaseDate { get; set; }
		public string Genre { get; set; }
		public decimal Price { get; set; }
	}

	public class MovieContext : DbContext
	{
		public MovieContext(DbContextOptions<MovieContext> options)
				: base(options)
		{
		}

		public DbSet<Movie> Movie { get; set; }
	}
	*/
}