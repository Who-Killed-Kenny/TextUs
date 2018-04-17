using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;

namespace TextUs.Common
{
	public static class HelperFunctions
	{
		public static TraceSwitch HelperFunctionsSwitch = new TraceSwitch("HelperFunctions", "Set HelperFunctions tracing");
		public enum MsgTypeEnum { Error, Info, Warning };
		private static Dictionary<MsgTypeEnum, StringBuilder> messages;
		private static bool _trace = false;
		private static bool _isTest = false;
		private static string _systemAppName = "<unknown>";
		private static DateTime _runTimeStamp;
		private static StreamWriter _runTimeFile;
		private static StreamWriter _traceFile;
		private static ExceptionLogPublisher _exLog = new ExceptionLogPublisher();
		//private static SqlConnection _conn = null;

		#region Properties

		public static bool IsTraceOn
		{
			get
			{
				return _trace;
			}
			set
			{
				_trace = value;
			}
		}

		public static bool IsTest
		{
			get
			{
				return _isTest;
			}
			set
			{
				_isTest = value;
			}
		}

		public static DateTime RunTimeStamp
		{
			get
			{
				if (_runTimeStamp == DateTime.MinValue)
				{
					_runTimeStamp = DateTime.Now;
				}
				return _runTimeStamp;
			}
			set
			{
				_runTimeStamp = value;
			}
		}

		public static string SystemAppName
		{
			get
			{
				return _systemAppName;
			}
			set
			{
				_systemAppName = value;
			}
		}

		public static StreamWriter TraceFile
		{
			get
			{
				return _traceFile;
			}
			set
			{
				_traceFile = value;
			}
		}

		public static StreamWriter RunTimeFile
		{
			get
			{
				return _runTimeFile;
			}
			set
			{
				_runTimeFile = value;
			}
		}

		public static ExceptionLogPublisher exLog
		{
			get
			{
				return _exLog;
			}
			set
			{
				_exLog = value;
			}
		}

		#endregion

		#region Config Methods

		public static string ConfigEntry(string configKey)
		{
			string hold = string.Empty;
			try
			{
				hold = ConfigurationManager.AppSettings[configKey] ?? "Not Found";
				if (string.IsNullOrEmpty(hold) || hold == "Not Found")
				{
					throw new System.ApplicationException(string.Format("Config entry '{0}' not found.  Check .config file.", configKey));
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLineIf(HelperFunctionsSwitch.TraceError, string.Format("Config entry '{0}' not found.  Check .config file.", configKey));
				throw ex;
			}
			return hold;
		}

		public static string ConnectionStr()
		{
			string connString = string.Empty;
			try
			{
				if (Properties.Settings.Default.UseTestDB)
				{
					connString = Properties.Settings.Default.TestDB_Connection ?? "Not Found";
				}
				else
				{
					connString = Properties.Settings.Default.ProdDB_Connection ?? "Not Found";
				}

				if (string.IsNullOrEmpty(connString) || connString == "Not Found")
				{
					throw new System.ApplicationException("Connection string not found.  Check .config file.");
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLineIf(HelperFunctionsSwitch.TraceError, "Connection Failed with connString=" + connString);
				throw ex;
			}
			return connString;
		}

		#endregion

		#region Database Methods

		public static SqlConnection DbConnection()
		{
			SqlConnection _conn = null;		// original place of creation before static connection
			try
			{
				if (_conn == null)
				{
					if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Creating (and opening) the connection"); }
					_conn = new SqlConnection();
					_conn.ConnectionString = ConnectionStr();
				}
				if (_conn.State != ConnectionState.Open) _conn.Open();
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("Timeout expired") || ex.Message.Contains("The wait operation timed out") || ex.Message.Contains("timeout period elapsed"))
				{
					try
					{   //try it one more time after a 5 second wait...
						System.Threading.Thread.Sleep(5000);
						if (_conn == null)
						{
							if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Creating the connection after timeout"); }
							_conn = new SqlConnection();
							_conn.ConnectionString = ConnectionStr();
						}
						if (_conn.State != ConnectionState.Open) _conn.Open();
					}
					catch
					{
						//eat this one and throw the original
						HelperFunctions.AddInfo(ex, "connstring", _conn.ConnectionString);
						throw ex;
					}
				}
				else
				{
					HelperFunctions.AddInfo(ex, "connstring", _conn.ConnectionString);
					throw ex;
				}
			}

			return _conn;
		}

		//public static void DbCloseConnection()
		//{
		//	if (_conn != null && _conn.State == ConnectionState.Open)
		//	{
		//		if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Closing the connection"); }
		//		_conn.Close();
		//	}
		//}

		public static void DebugDataSetErrors(StringBuilder error, DataSet ds)
		{
			try
			{
				if (error == null) { error = new StringBuilder(); }

				foreach (DataTable table in ds.Tables)
				{
					if (table.HasErrors)
					{
						error.AppendLine(table.TableName + ": at least one row returned error");

						foreach (DataRow row in table.Rows)
						{
							if (row.HasErrors)
							{
								error.AppendLine(row.RowError.ToString());
							}
						}
					}
				}
			}
			catch
			{
				//Eat problems with reporting db errors
			}

		}

		#endregion

		#region Exception Methods

		public static void AddInfo(Exception exIn, string key, string value)
		{
			try
			{
				NameValueCollection extraInfo;
				object item = exIn.Data["additionalInfo"];
				if (item == null)
				{
					extraInfo = new NameValueCollection();
					exIn.Data.Add("additionalInfo", extraInfo);
				}
				else
				{
					extraInfo = (NameValueCollection)item;
				}
				extraInfo.Add(key, value);
			}
			catch
			{
				//Eat problems with adding additionalInfo
				//int catchTest = 0;
			}
		}

		#endregion

		#region Logging Methods

		public static void WriteTraceEntry(string text)
		{
			string timeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");
			string msg = "{0}{1} {2}";
			string logPath = "Unknown";
			string logName = "Unknown";

			try
			{
				if (!string.IsNullOrEmpty(text))
				{
					logPath = Properties.Settings.Default.SystemLogPath.Trim().Replace("\\", "/");
					if (logPath.Substring(logPath.Length - 1, 1) != "/") { logPath += "/"; }
					logName = string.Format(Properties.Settings.Default.TraceLogName, RunTimeStamp.ToString("yyyy-MM-dd HH"));
					TraceFile = File.AppendText(logPath + logName);
					TraceFile.WriteLine(string.Format(msg, IsTest ? "Test: " : "", timeStamp, text));
					TraceFile.Close();
				}
			}
			catch
			{
				// eat errors writing logging entries
				int catchEx = 0;
			}
		}

		public static void WriteRunLog(string text)
		{
			string timeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");
			string appName = "(" + SystemAppName + ")";
			string msg = "{0}{1} {2}";
			string logPath = "Unknown";
			string logName = "Unknown";

			try
			{
				if (text.Length > 0)
				{
					logPath = Properties.Settings.Default.SystemLogPath.Trim().Replace("\\", "/");
					if (logPath.Substring(logPath.Length - 1, 1) != "/") { logPath += "/"; }
					logName = string.Format(Properties.Settings.Default.RunTimesLogName, RunTimeStamp.ToString("yyyy-MM-dd"));

					RunTimeFile = File.AppendText(logPath + logName);
					RunTimeFile.WriteLine(string.Format(msg, IsTest ? "Test: " : "", timeStamp, text));
					RunTimeFile.Close();
				}
			}
			catch
			{
				// eat errors writing logging entries
				int catchEx = 0;
			}
		}

		#endregion

		#region Message Methods

		public static bool MessageTypeExists(MsgTypeEnum type)
		{
			bool result = false;

			if (messages != null && messages.ContainsKey(type) && messages[type].Length > 0)
			{
				result = true;
			}

			return result;
		}

		public static void MessageAdd(MsgTypeEnum type, string msg)
		{
			try
			{
				StringBuilder currentMsg;

				if (messages == null) { messages = new Dictionary<MsgTypeEnum, StringBuilder>(); }
				if (messages.ContainsKey(type))
				{
					currentMsg = messages[type];
				}
				else
				{
					currentMsg = new StringBuilder();
					messages.Add(type, currentMsg);
				}
				currentMsg.AppendLine(msg);
			}
			catch
			{
				//Eat problems with adding a message
			}
		}

		public static StringBuilder MessageGet(MsgTypeEnum type)
		{
			StringBuilder result = null;
			try
			{
				if (messages == null) { messages = new Dictionary<MsgTypeEnum, StringBuilder>(); }
				if (messages.ContainsKey(type))
				{
					result = messages[type];
				}
				else
				{
					result = new StringBuilder();
				}
			}
			catch
			{
				//Eat problems with adding a message
			}

			return result;
		}

		public static void MessageReset(MsgTypeEnum type)
		{
			try
			{
				if (messages == null) { messages = new Dictionary<MsgTypeEnum, StringBuilder>(); }
				if (messages.ContainsKey(type))
				{
					messages[type] = new StringBuilder();
				}
				else
				{
					messages.Add(type, new StringBuilder());
				}
			}
			catch
			{
				//Eat problems with adding a message
			}
		}

		#endregion

	}
}