using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using TextUs.Common;

namespace TextUs.Business
{
	public class Main
	{

		#region Constants

		private const int c_TBD = -1;

		#endregion

		#region Private Properties

		private MessageBO _busMessage = new MessageBO();

		private SendGridBO _busEmail = new SendGridBO();

		#endregion

		#region Public Properties

		public struct TextUsData
		{
			public enum TextSourceEnum
			{
				QPS = 0, AE = 1
			}

			public TextSourceEnum Source;
			public string EmployeePhoneNumber;
			public DateTime TextDate;
			public string Body; 
			public string Request;
			public string JSON;
			public string ExternalId;

			public string Display
			{
				get
				{
					StringBuilder msg = new StringBuilder();
					msg.AppendFormat("{0}({1}), ", "Source", Source.ToString());
					msg.AppendFormat("{0}({1}), ", "EmployeePhoneNumber", EmployeePhoneNumber);
					msg.AppendFormat("{0}({1}), ", "ExternalId", ExternalId);
					msg.AppendFormat("{0}({1}), ", "TextDate", TextDate.ToShortTimeString());
					//msg.AppendFormat("{0}({1}), ", "Request", Request);
					//msg.AppendFormat("{0}({1}), ", "JSON", JSON);
					msg.AppendFormat("{0}({1})", "Body", Body);
					return msg.ToString();
				}
			}
		}

		public struct SendGridData
		{
			public string Body { get; set; }
			public string Request { get; set; }
			public string JSON { get; set; }
			public string DkIm { get; set; }
			//public Dictionary<string, string> Dict { get; set; }
			public List<Dictionary<string, string>> DictList { get; set; }
			public Business.SendGridBO.EmailList Emails { get; set; }

			public List<Business.SendGridBO.EmailEvent> Events { get; set; }
			//public class jsonDataTable
			//{
			//	public IEnumerable<Dictionary<string, string>> sg_emails { get; set; }
			//}

			public string Display
			{
				get
				{
					StringBuilder msg = new StringBuilder();
					msg.AppendFormat("{0}({1}), ", "Request", Request);
					msg.AppendFormat("{0}({1}), ", "JSON", JSON);
					msg.AppendFormat("{0}({1})", "Body", Body);
					return msg.ToString();
				}
			}
		}

		public enum ProcessResultEnum
		{
			OK = 0,
			Duplicate = 1,
			PhoneNumberNotFound = 2,
			Error = 10,
			TimeOut = 11
		}

		#endregion

		#region Public Methods

		public bool TextAlreadyProcessed(string textUsId)
		{
			bool result = false;
			//MessageBO busObj = new MessageBO();
			string msg = string.Empty;

			try
			{
				//if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(string.Format("Check if TextAlreadyProcessed ({0})", textUsId)); }
				result = (_busMessage.GetStaffingActionByExternalIdAndChoiceID(textUsId) != null);				
			}
			catch (Exception ex)
			{
				result = false;
				//try
				//{   // Attempt to add as much info to the exeception as exists
				//	ex.Data.Add("systemAppName", HelperFunctions.SystemAppName);
				//	ex.Data.Add("systemMethodName", System.Reflection.MethodBase.GetCurrentMethod().Name);

				//	if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Exception encountered. " + ex.Message); }
				//}
				//catch
				//{
				//	// Eat problems with adding execetion info.
				//}
				throw ex;

				//try
				//{
				//	HelperFunctions.exLog.Publish(ex, "ProcessText Error", true);
				//}
				//catch
				//{
				//	// Since publishing the error failed, send it to error page.
				//	throw ex;
				//}

				//// Since the error was published, eat it and go on.
			}

			return result;
		}

		public ProcessResultEnum ProcessEmail(SendGridData dataIn)
		{
			ProcessResultEnum result = ProcessResultEnum.Error;
			JavaScriptSerializer js = new JavaScriptSerializer();
			//TextUs.Business.SendGridEmailBO email = new TextUs.Business.SendGridEmailBO();
			//SendGridBO.EmailList emails = new SendGridBO.EmailList();

			bool atLeastOneSaved = false;

			//string work = dataIn.JSON;
			//int openBrace;
			//int closeBrace;
			//string jsonEmail;

		//while (work.Length > 0)
		//	{
		//		openBrace = work.IndexOf("{");
		//		closeBrace = work.IndexOf("}")+1;
		//		if (closeBrace >= work.Length)
		//		{
		//			jsonEmail = work.Substring(openBrace);
		//			work = string.Empty;
		//		}
		//		else
		//		{
		//			jsonEmail = work.Substring(openBrace, closeBrace);
		//			work = work.Substring(closeBrace+1);
		//		}
		//		dataIn.Dict = js.Deserialize<Dictionary<string, string>>(jsonEmail);
		//		email.Dkim = dataIn.Dict["Dkim"].Trim();
		//	}

			try
			{
				//foreach (Dictionary<string, string> item in dataIn.DictList)
				//{
				//	email.Email = GetString(item, "email");
				//	email.Event = GetString(item, "event");
				//	email.Timestamp = GetString(item, "timestamp");
				//	email.Event_Id = GetString(item, "sg_event_id");
				//	email.Category = GetString(item, "category");
				//	email.Smtp_Id = GetString(item, "smtp-id");
				//	email.Message_Id = GetString(item, "sg_message_id");
				//	email.Response = GetString(item, "response");
				//	email.Attempt = GetString(item, "attempt");
				//	email.UserAgent = GetString(item, "useragent");
				//	email.IP = GetString(item, "ip");
				//	email.URL = GetString(item, "url");
				//	email.Status = GetString(item, "status");
				//	email.Asm_Group_Id = GetString(item, "asm_group_id");

				//	if (_busEmail.AddSendGridEmail(_busEmail, email))
				//	{
				//		atLeastOneSaved = true;
				//	}
				//}
				foreach (SendGridBO.EmailEvent item in dataIn.Events)
				{
					if (_busEmail.AddSendGridEmail(_busEmail, item))
					{
						atLeastOneSaved = true;
					}
				}

				if (atLeastOneSaved && _busEmail.Save())
				{
					result = ProcessResultEnum.OK;
				}

			}
			catch (Exception ex)
			{
				result = ProcessResultEnum.Error;
				try
				{   // Attempt to add as much info to the exeception as exists
					ex.Data.Add("systemAppName", HelperFunctions.SystemAppName);
					ex.Data.Add("systemMethodName", System.Reflection.MethodBase.GetCurrentMethod().Name);
					ex.Data.Add("Request in", dataIn.Request);
					ex.Data.Add("JSON in", dataIn.JSON);
					HelperFunctions.AddInfo(ex, "ProcessEmail data", dataIn.Display);
					HelperFunctions.AddInfo(ex, "Request", dataIn.Request);
					HelperFunctions.AddInfo(ex, "JSON", dataIn.JSON);

					if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Exception encountered. " + ex.Message); }
					HelperFunctions.WriteRunLog(ex.Message);
				}
				catch
				{
					// Eat problems with adding execetion info.
				}

				try
				{
					HelperFunctions.exLog.Publish(ex, "ProcessText Error", true);
				}
				catch
				{
					// Since publishing the error failed, send it to error page.
					throw ex;
				}

				// Since the error was published, eat it and go on.
			}
			finally
			{
				_busEmail.CloseConnection();
			}

			return result;
		}

		public ProcessResultEnum ProcessText(TextUsData dataIn)
		{
			ProcessResultEnum result = ProcessResultEnum.Error;
			//MessageBO busObj = new MessageBO();
			System.Data.DataTable employees;
			string msg = string.Empty;
			bool moreThan1EmployeeForPhonenumber = false;
			MessageBO.TextData messageData;
			bool skipThisEmployee;
			bool atLeastOneMessageCreated = false;
			Int64 phoneNumber = -1;

			try
			{

				if (TextAlreadyProcessed(dataIn.ExternalId))
				{
					result = Business.Main.ProcessResultEnum.OK;
					msg = string.Format("TextUs sent a text (ID={0} at={1}) that we have already processed.  Sending back a positive response.  JSON {2} ",
						dataIn.ExternalId, dataIn.TextDate.ToShortTimeString(), dataIn.JSON);
					HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Warning, msg);
					if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
					HelperFunctions.WriteRunLog(msg);
				}
				else
				{
					phoneNumber = Convert.ToInt64(dataIn.EmployeePhoneNumber);
					employees = _busMessage.GetEmployeesByPhoneNumber(phoneNumber);

					if (employees == null || employees.Rows.Count == 0)
					{
						result = ProcessResultEnum.PhoneNumberNotFound;
						msg = string.Format("Text message {3} phone number {0} ({1}) failed to match any employee records.  Data: {2}",
							dataIn.EmployeePhoneNumber, phoneNumber, dataIn.JSON,
							(dataIn.Source == TextUsData.TextSourceEnum.AE) ? "from" : "to");
						HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Warning, msg);
						if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
						HelperFunctions.WriteRunLog(msg);
					}
					else
					{
						moreThan1EmployeeForPhonenumber = (employees.Rows.Count > 1);

						messageData = new MessageBO.TextData();
						messageData.Subject = string.Format("TextUs: {0}{1}", dataIn.Source.ToString(),
							(employees.Rows.Count == 1 ? string.Empty : string.Format(" ({0} AE's)", employees.Rows.Count)));
						messageData.Body = dataIn.Body;
						messageData.CreateDate = dataIn.TextDate;
						messageData.DateEntered = DateTime.Now;
						messageData.PhoneNumber = dataIn.EmployeePhoneNumber;
						messageData.ExternalId = dataIn.ExternalId;

						for (int sub = 0; sub <= employees.Rows.Count - 1; sub++)
						{
							skipThisEmployee = false;

							//get the employee guid
							if (!Guid.TryParse(employees.Rows[sub][0].ToString(), out messageData.EmployeeGUID))
							{
								skipThisEmployee = true;
								try
								{
									msg = string.Format("Employee GUID ({0}) using phone number {1} failed to convert to a GUID.",
										employees.Rows[sub][0].ToString(), dataIn.EmployeePhoneNumber);
									HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Warning, msg);
									if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
								}
								catch
								{
									// skip the failure while attempting to add extra trace info.
								}
							}

							//get the Employee ID
							if (!int.TryParse(employees.Rows[sub][1].ToString(), out messageData.EmployeeID))
							{
								skipThisEmployee = true;
								try
								{
									msg = string.Format("Employee ID ({0}) using phone number {1} failed to convert to an integer.",
										employees.Rows[sub][1].ToString(), dataIn.EmployeePhoneNumber);
									HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Warning, msg);
									if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
								}
								catch
								{
									// skip the failure while attempting to add extra trace info.
								}
							}

							messageData.StaffingActionOriginValue = employees.Rows[sub][2].ToString();

							if (!skipThisEmployee)
							{
								if (messageData.StaffingActionGUID == Guid.Empty) { messageData.StaffingActionGUID = _busMessage.AddStaffingAction(_busMessage, messageData); };
								if (!Guid.Equals(_busMessage.AddStaffingActionFK(_busMessage, messageData), Guid.Empty))
								{
									atLeastOneMessageCreated = true;
								}
							}
						}
						if (atLeastOneMessageCreated)
						{
							try
							{
								if (_busMessage.Save())
								{
									result = ProcessResultEnum.OK;
								}
								else
								{
									result = ProcessResultEnum.Error;
								}
							}
							catch (SqlException ex)
							{
								if (ex.Message.Contains("Timeout expired") || ex.Message.Contains("The wait operation timed out") || ex.Message.Contains("timeout period elapsed"))
								{
									result = ProcessResultEnum.TimeOut;
								}
								else
								{
									result = ProcessResultEnum.Error;
									msg = string.Format("TextUs Save: non-timeout SQL error ({0}).  result:{1}", ex.Message, result.ToString());
									HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Info, msg);
									if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
									HelperFunctions.WriteRunLog(msg);
									throw ex;
								}
							}
							catch (Exception ex)
							{
								result = ProcessResultEnum.Error;
								msg = string.Format("TextUs Save: Non-SQL error ({0}).  result:{1}", ex.Message, result.ToString());
								HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Info, msg);
								if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
								HelperFunctions.WriteRunLog(msg);
								throw ex;
							}
						}

						if (atLeastOneMessageCreated)
						{
							if (result == ProcessResultEnum.OK)
							{
								msg = string.Format("Text message (ExternalId: {0}) {2} phone number {1} saved to DB.", dataIn.ExternalId, dataIn.EmployeePhoneNumber,
									(dataIn.Source == TextUsData.TextSourceEnum.AE) ? "from" : "to");
								HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Info, msg);
								if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
								HelperFunctions.WriteRunLog(msg);
							}
						}
						else
						{
							if (result == ProcessResultEnum.Error)
							{
								msg = string.Format("Text message (ExternalId: {0}) {2} phone number {1} failed to create a message record.", dataIn.ExternalId, dataIn.EmployeePhoneNumber,
									(dataIn.Source == TextUsData.TextSourceEnum.AE) ? "from" : "to");
								HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Warning, msg);
								if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
								HelperFunctions.WriteRunLog(msg);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				result =ProcessResultEnum.Error;
				try
				{   // Attempt to add as much info to the exeception as exists
					ex.Data.Add("systemAppName", HelperFunctions.SystemAppName);
					ex.Data.Add("systemMethodName", System.Reflection.MethodBase.GetCurrentMethod().Name);
					ex.Data.Add("Request in", dataIn.Request);
					ex.Data.Add("JSON in", dataIn.JSON);
					HelperFunctions.AddInfo(ex, "ProcessText data", dataIn.Display);
					HelperFunctions.AddInfo(ex, "Request", dataIn.Request);
					HelperFunctions.AddInfo(ex, "JSON", dataIn.JSON);

					if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Exception encountered. " + ex.Message); }
					HelperFunctions.WriteRunLog(ex.Message);
				}
				catch
				{
					// Eat problems with adding execetion info.
				}

				try
				{
					HelperFunctions.exLog.Publish(ex, "ProcessText Error", true);
				}
				catch
				{
					// Since publishing the error failed, send it to error page.
					throw ex;
				}

				// Since the error was published, eat it and go on.
			}
			finally
			{
				_busMessage.CloseConnection();
			}

			return result;
		}

		#endregion

		#region Private Methods

		int GetInt(Dictionary<string, string> dict, string key)
		{
			int result = -1;

			try
			{
				int.TryParse(dict[key], out result);
			}
			catch (Exception ex)
			{
				//result = string.Empty;
				// Eat it and go on.
			}

			return result;
		}

		string GetString(Dictionary<string, string> dict, string key)
		{
			string result = null;

			try
			{
				result = dict[key];
			}
			catch (Exception ex)
			{
				//result = string.Empty;
				// Eat it and go on.
			}

			return result;
		}

		#endregion


	}
}