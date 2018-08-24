using System;
using System.Collections.Generic;
using System.Data;  
using System.Data.SqlClient;  
using System.Threading.Tasks;  

namespace WebCalcDb.Models
{

	public static class SqlHelper
	{
		// Set the connection, command, and then execute the command with non query.  
		public static Int32 ExecuteNonQuery(String connectionString, String commandText,
			CommandType commandType, params SqlParameter[] parameters)
		{
			using (SqlConnection conn = new SqlConnection(connectionString))
			{
				using (SqlCommand cmd = new SqlCommand(commandText, conn))
				{
					// There're three command types: StoredProcedure, Text, TableDirect. The TableDirect   
					// type is only for OLE DB.    
					cmd.CommandType = commandType;
					cmd.Parameters.AddRange(parameters);

					conn.Open();
					return cmd.ExecuteNonQuery();
				}
			}
		}

		// Set the connection, command, and then execute the command and only return one value.  
		public static Object ExecuteScalar(String connectionString, String commandText,
			CommandType commandType, params SqlParameter[] parameters)
		{
			using (SqlConnection conn = new SqlConnection(connectionString))
			{
				using (SqlCommand cmd = new SqlCommand(commandText, conn))
				{
					cmd.CommandType = commandType;
					cmd.Parameters.AddRange(parameters);

					conn.Open();
					return cmd.ExecuteScalar();
				}
			}
		}

		//private void AssertActionResult<T>(IActionResult actual, Func<T, string> selector = null)

		// Set the connection, command, and then execute the command with query and extract the data.  
		public static List<T> ExecuteReader<T>(String connectionString, String commandText,
			CommandType commandType, Func<SqlDataReader, T> extractor, params SqlParameter[] parameters)
		{
			using (SqlConnection conn = new SqlConnection(connectionString))
			{
				using (SqlCommand cmd = new SqlCommand(commandText, conn))
				{
					cmd.CommandType = commandType;
					cmd.Parameters.AddRange(parameters);

					conn.Open();
					// When using CommandBehavior.CloseConnection, the connection will be closed when the   
					// IDataReader is closed.  
					SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);

					var output = new List<T>();

					// Call Read before accessing data.
					while (reader.Read())
					{
						output.Add(extractor(reader));
					}

					// Call Close when done reading.
					reader.Close();

					return output;
				}
			}
		}
	}

	public class CSqlConnector
	{
		public String connectionString { get; }

		public CSqlConnector(String connectionString)
		{
			this.connectionString = connectionString;
		}

		// Set the connection, command, and then execute the command with non query.  
		public Int32 ExecuteNonQuery(String commandText, CommandType commandType, params SqlParameter[] parameters)
		{
			return SqlHelper.ExecuteNonQuery(connectionString,  commandText, commandType, parameters);
		}

		// Set the connection, command, and then execute the command and only return one value.  
		public Object ExecuteScalar(String commandText, CommandType commandType, params SqlParameter[] parameters)
		{
			return SqlHelper.ExecuteScalar( connectionString,  commandText, commandType, parameters);
		}

		// Set the connection, command, and then execute the command with query and return the reader.  
		public List<T> ExecuteReader<T>(String commandText, CommandType commandType, Func<SqlDataReader, T> extractor, params SqlParameter[] parameters)
		{
			return SqlHelper.ExecuteReader( connectionString,  commandText, commandType, extractor, parameters);
		}
	}

}