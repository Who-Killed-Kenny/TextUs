using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QPS.Email;

namespace TextUs.Common
{
	class EmailFunctions
	{

		//public class emailInfo
		//{
		//	public string To;
		//	public string CC;
		//	public string BCC;
		//	public string FromAddress;
		//	public string FromAlias;
		//	public string Subject;
		//	public string Body;
		//	public string SendAttachment;
		//}

		/// <summary>
		/// Send Error to default error address
		/// </summary>
		/// <param name="subject">Subject of the email</param>
		/// <param name="body">Message body of the email</param>
		/// <param name="toAddress">Who the email is going to</param>
		public static void SendEmail(String subject, String body, String toAddress)
		{
			String[] to = new String[1] { toAddress };
			String[] from = new String[2] { Properties.Settings.Default.MailFromAddress, Properties.Settings.Default.MailFromAlias };

			SendEmail(subject, body, to, from);
		}

		/// <summary>
		/// Send Error to default error address
		/// </summary>
		/// <param name="subject">Subject of the email</param>
		/// <param name="body">Message body of the email</param>
		public static void SendError(String subject, String body)
		{
			String[] to = new String[2] { Properties.Settings.Default.MailErrorsToAddress, Properties.Settings.Default.MailErrorsToAddress2 };
			String[] from = new String[2] { Properties.Settings.Default.MailFromAddress, Properties.Settings.Default.MailFromAlias };

			SendEmail(subject, body, to, from);
		}

		/// <summary>
		/// Send QPSEmail
		/// </summary>
		/// <param name="subject">Subject of the email</param>
		/// <param name="body">Message body of the email</param>
		/// <param name="to">Who the email is going to</param>
		/// <param name="from">Who the email if from</param>
		public static void SendEmail(String subject, String body, String[] to, String[] from)
		{
			if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Entering Function: SendEmail"); }
			try
			{
				QPSEmail mail = new QPSEmail(to, from, Properties.Settings.Default.MailDomain);
				mail.IsBodyHtml = true;
				mail.Subject = subject;
				body += "<br><br><b><em>This is an automated message from an unmonitored account, please do not reply.</em></b><br>" +
						"If you have any questions - please forward this email with a reply to: <a href='mailto:helpdesk@qpsemployment.com'>helpdesk@qpsemployment.com</a>";
				mail.Body = body.Replace(Environment.NewLine, "<br />");
				mail.SendEmail(Properties.Settings.Default.MailServer);
				mail.Dispose();
			}
			catch (Exception ex)
			{
				ex.Data.Add("systemAppName", HelperFunctions.SystemAppName);
				ex.Data.Add("systemMethodName", System.Reflection.MethodBase.GetCurrentMethod().Name);
				HelperFunctions.exLog.Publish(ex, "sending email", false);

				//HelperFunctions.HandleException(ex, "sending email", false);
			}
			if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Exiting Function: SendEmail"); }
		}
	}
}