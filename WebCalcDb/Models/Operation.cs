using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace WebCalcDb
{
	public enum EMathOps { Sum = 1, Sub, Mul, Div }; //TODO: Çàìåíèòü Sum íà Add - ÷òîáû íå ïóòàëñÿ ñ Sub

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
	}
	
	/// <summary>
	/// ID	UTC	(ms)	IP	operator(char[1] or Enum)	opnd1	opnd2
	/// </summary>
	public class COperationDb: COperation
	{
		public uint _id { get; set; }
		public DateTime utc { get; set; }
		public System.Net.IPAddress fromIP{ get; set; }
	}
}