using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Specialized;
using System.Configuration;

namespace TextUs.Common
{
	public class ExceptionLogPublisher
	{
		bool StackTrace = Properties.Settings.Default.ShowStackTrace;

		/// <summary>
		/// Publish an exception to the run-time log and possibly to an email to the development group.
		/// </summary>
		/// <param name="exception"></param>
		/// <param name="errorActivity"></param>
		/// <param name="alsoSendEmail"></param>
		public void Publish(System.Exception exception, string errorActivity, bool alsoSendEmail = false)
		{
			StringBuilder errInfo = new StringBuilder();
			string systemAppName = "MissingSystemName";
			string systemMethodName = "MissingMethodName";
			object data;

			data = exception.Data["systemAppName"];
			if ((data != null)) { systemAppName = data.ToString(); }

			data = exception.Data["systemMethodName"];
			if ((data != null)) { systemMethodName = data.ToString(); }

			errInfo.AppendFormat("{0}Exception From: {1}.{2} while {3}.",
				Environment.NewLine, systemAppName, systemMethodName, errorActivity);

			FormatException(errInfo, exception, StackTrace);

			HelperFunctions.WriteRunLog(errInfo.ToString());

			if (alsoSendEmail)
			{
				Common.EmailFunctions.SendError(systemAppName + ": Exception encountered. ", errInfo.ToString());
				//Common.EmailFunctions.SendEmail(systemAppName + ": Exception encountered. ", errInfo.ToString(), "KLiebergen");
			}
			exception.Data.Add("ErrorPublished", "True");
		}
		public void SendEmail(string subject, string body, string toAddys)
		{
			Common.EmailFunctions.SendEmail(subject, body, toAddys);
		}

		/// <summary>
		/// Format an exception for logging and/or email.  This includes any existing
		/// additional information data loaded at the time of each failure and also stack
		/// trace information if the flag is true,
		/// </summary>
		/// <param name="error"></param>
		/// <param name="exception"></param>
		/// <param name="showStacktrace"></param>
		private void FormatException(StringBuilder error, System.Exception exception, bool showStacktrace)
		{
			System.Collections.Specialized.NameValueCollection additionalInfo = null;
			object data;

			if (error == null) { error = new StringBuilder(); }

			error.AppendFormat("{1}{0}", Environment.NewLine, exception.ToString());

			if (showStacktrace && exception.StackTrace != null)
			{
				error.AppendLine("");
				error.AppendLine(" -- Stack Trace Information --");
				error.AppendLine(exception.StackTrace.ToString());
			}

			data = exception.Data["additionalInfo"];
			if ((data != null))
			{
				additionalInfo = (System.Collections.Specialized.NameValueCollection)data;
				error.AppendLine("");
				error.AppendLine(" -- Additional Info --");
				foreach (string key in additionalInfo)
				{
					error.AppendFormat("   + {0}: {1}", key, additionalInfo.Get(key));
					error.AppendLine("");
				}
			}

			if (exception.InnerException != null)
			{
				error.AppendLine("");
				error.AppendLine(" ------ Inner Exception ------");
				FormatException(error, exception.InnerException, showStacktrace);
			}
		}
	}
}