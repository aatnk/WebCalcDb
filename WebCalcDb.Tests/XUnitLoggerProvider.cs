using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

//// Вопрос: Ядро Entity Framework: запросы журнала для одного экземпляра контекста db
//// Entity Framework Core: Log queries for a single db context instance
//// https://www.programmerz.ru/questions/25714/entity-framework-core-log-queries-for-a-single-db-context-instance-question
//// https://stackoverflow.com/questions/43424095/how-to-unit-test-with-ilogger-in-asp-net-core
//// (c) Ilya Chumakov, 2017
namespace Xunit.Loging
{
	public interface IWriter
	{
		void WriteLine(string str);
	}

	public class BaseTest : IWriter
	{
		public ITestOutputHelper Output { get; }

		public BaseTest(ITestOutputHelper output)
		{
			Output = output;
		}

		public void WriteLine(string str)
		{
			Output.WriteLine(str ?? Environment.NewLine);
		}
	}

	public class LoggerProvider : ILoggerProvider
	{
		public IWriter Writer { get; private set; }

		public LoggerProvider(IWriter writer)
		{
			Writer = writer;
		}
		public void Dispose()
		{
		}

		public ILogger CreateLogger(string categoryName)
		{
			return new XUnitLogger(Writer, categoryName);
		}

		public class XUnitLogger : ILogger
		{
			public IWriter Writer { get; }
			public string Name { get; set; }
			public string categoryName { get; private set; }

			public XUnitLogger(IWriter writer, string categoryName)
			{
				this.Writer = writer;
				this.Name = nameof(XUnitLogger);
				this.categoryName = categoryName;
			}

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
				Func<TState, Exception, string> formatter)
			{
				if (!this.IsEnabled(logLevel))
					return;

				if (formatter == null)
					throw new ArgumentNullException(nameof(formatter));

				string message = formatter(state, exception);
				if (string.IsNullOrEmpty(message) && exception == null)
					return;

				string dtNow = DateTime.Now.ToLocalTime().ToString("HH:mm:ss.ffff");
				string line = $"[{dtNow} {this.Name}]>> {logLevel}: {categoryName}: {message}";

				Writer.WriteLine(line);

				if (exception != null)
					Writer.WriteLine(exception.ToString());
			}

			public bool IsEnabled(LogLevel logLevel)
			{
				return true;
			}

			public IDisposable BeginScope<TState>(TState state)
			{
				return new XUnitScope();
			}
		}

		public class XUnitScope : IDisposable
		{
			public void Dispose()
			{
			}
		}
	}
}
