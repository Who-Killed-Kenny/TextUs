using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;
using TextUs;
using TextUs.Common;
using System.Globalization;
using System.Net.Http;
using System.Net;

namespace TextUs.WebHookHandlers
{
	public class GenericJsonWebHookHandler: WebHookHandler
	{
		public GenericJsonWebHookHandler()
		{
			this.Receiver = GenericJsonWebHookReceiver.ReceiverName;
		}

		public override Task ExecuteAsync(string generator, WebHookHandlerContext context)
		{
			//bool success = false;
			Business.Main.ProcessResultEnum processResult = Business.Main.ProcessResultEnum.Error;
			JavaScriptSerializer js = new JavaScriptSerializer();
			HttpResponseMessage response = context.Request.CreateResponse(context.Request);
			Business.Main _busMain = new Business.Main();
			bool result = false;

			try
			{
				string msg = string.Empty;

				if (string.Compare(context.Id, "TextUs", true) == 0)
				{
					// Received a WebHook from TextUs!
					Business.Main.TextUsData textReceived = new Business.Main.TextUsData();
					Dictionary<string, string> msgDict = null;
		
					try
					{
						// Get JSON from WebHook
						msgDict = js.Deserialize<Dictionary<string, string>>(TextUs.WebApiApplication.documentContents);
						textReceived.Request = context.Request.ToString();
						textReceived.JSON = TextUs.WebApiApplication.documentContents;
						string phoneWork = string.Empty;

						phoneWork = "not provided";
						switch (msgDict["recipient_type"])
						{
							case "Contact":
								textReceived.Source = Business.Main.TextUsData.TextSourceEnum.QPS;
								phoneWork = msgDict["recipient_phone"].Trim();
								break;
							case "Account":
								textReceived.Source = Business.Main.TextUsData.TextSourceEnum.AE;
								phoneWork = msgDict["sender_phone"].Trim();
								break;
						}
						textReceived.EmployeePhoneNumber = (phoneWork.Length > 10 ? phoneWork.Substring(phoneWork.Length - 10) : phoneWork);
						textReceived.ExternalId = msgDict["id"];
						textReceived.Body = msgDict["body"];

						msg = string.Format("TextUs message ({0}) received ...", textReceived.ExternalId);
						if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
						HelperFunctions.WriteRunLog(msg);

						try
						{
							DateTime parsedDate = DateTime.ParseExact(msgDict["created_at"], "yyyy-MM-dd HH:mm:ss UTC", CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal);
							textReceived.TextDate = parsedDate;
							//TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
							//if (cstZone.IsDaylightSavingTime(parsedDate))
							//{
							//	parsedDate.AddHours(1);
							//}
							//textReceived.TextDate = TimeZoneInfo.ConvertTimeFromUtc(parsedDate, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));
						}
						catch
						{
							msg = string.Format("TextUs created_at data ({0}) for phone number {1} (ID={2}) failed to convert to a date.  Current date used.",
								msgDict["created_at"], textReceived.EmployeePhoneNumber, textReceived.ExternalId);
							HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Warning, msg);
							if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
							textReceived.TextDate = DateTime.Now;
						}

						msg = string.Format("TextUs message ({0}) trying to process ...", textReceived.ExternalId);
						if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }

						for (int trySub = 1; trySub <= Properties.Settings.Default.ReTryCount; trySub++)
						{
							processResult = _busMain.ProcessText(textReceived);

							if (processResult == Business.Main.ProcessResultEnum.TimeOut)
							{
								msg = string.Format("Text message (ExternalId: {0}) attempt {1} timed out.  result:{2}  Retrying ...", textReceived.ExternalId, trySub, processResult.ToString());
								HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Info, msg);
								if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
								HelperFunctions.WriteRunLog(msg);

								if (trySub == Properties.Settings.Default.ReTryCount)
								{   //Final attempt has failed.
									HelperFunctions.exLog.SendEmail(string.Format("TextUs Save #{0}: Giving up ...", trySub), msg, "KLiebergen");
									throw new Exception(string.Format("TextUs Save timed out {0} times.  Giving up.", trySub));
								}
								else
								{
									if (trySub > 3) { HelperFunctions.exLog.SendEmail(string.Format("TextUs Save #{0}: Retry ...", trySub), msg, "KLiebergen"); }
									System.Threading.Thread.Sleep(15000);    //  Wait 15 seconds
								}
							}
							else
							{
								break;
							}
						}

						if (processResult != Business.Main.ProcessResultEnum.OK)
						{
							msg = string.Format("TextUs message ({0}) trying to finished process.  result:{1}", textReceived.ExternalId, processResult.ToString());
							if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
						}


						//response.StatusCode = (processResult == Business.Main.ProcessResultEnum.OK || processResult == Business.Main.ProcessResultEnum.Duplicate) ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
						switch (processResult)
						{
							case Business.Main.ProcessResultEnum.OK:
								response.StatusCode = HttpStatusCode.OK;
								result = true;
								break;
							case Business.Main.ProcessResultEnum.Duplicate:
								response.StatusCode = HttpStatusCode.OK;
								result = true;
								break;
							case Business.Main.ProcessResultEnum.PhoneNumberNotFound:
								response.StatusCode = HttpStatusCode.OK;
								result = true;
								break;
							case Business.Main.ProcessResultEnum.TimeOut:
								response.StatusCode = HttpStatusCode.InternalServerError;
								result = false;
								break;
							case Business.Main.ProcessResultEnum.Error:
								response.StatusCode = HttpStatusCode.InternalServerError;
								result = false;
								break;
							default:
								response.StatusCode = HttpStatusCode.InternalServerError;
								result = false;
								break;
						}

						if (HelperFunctions.IsTraceOn)
						{
							try
							{
								if (processResult != Business.Main.ProcessResultEnum.OK)     // if (HelperFunctions.IsTest && !success)
								{
									HelperFunctions.WriteTraceEntry(string.Format("Text Message (ExternalId: {0}) creation {1} phone number {2} has finished with {3} ... Response: {4}. Return: {5}.",
										textReceived.ExternalId, ((textReceived.Source == Business.Main.TextUsData.TextSourceEnum.AE) ? "from" : "to"),
										textReceived.EmployeePhoneNumber, processResult.ToString(), response.ToString(), result.ToString()
										));
								}
							}
							catch
							{
								// Eat the error trying to write to the log
							}
						}
					}
					catch (Exception ex)
					{
						processResult = Business.Main.ProcessResultEnum.Error;
						try
						{   // Attempt to add as much info to the exeception as exists
							ex.Data.Add("systemAppName", HelperFunctions.SystemAppName);
							ex.Data.Add("systemMethodName", System.Reflection.MethodBase.GetCurrentMethod().Name);
							ex.Data.Add("Request in", textReceived.Request.ToString());
							ex.Data.Add("TextUs Id", textReceived.ExternalId);
							ex.Data.Add("JSON in", textReceived.JSON.ToString());
							HelperFunctions.AddInfo(ex, "ProcessText data", textReceived.Display);
							HelperFunctions.AddInfo(ex, "Request", textReceived.Request);
							HelperFunctions.AddInfo(ex, "JSON", textReceived.JSON);

							if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Exception encountered. " + ex.Message); }
						}
						catch
						{
							msg = string.Format("TextUs message ({0}) error adding execetion info", textReceived.ExternalId);
							if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
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
					}
				}

				//   SEND GRID HANDLER ---------------
				else if (string.Compare(context.Id, "SendGrid", true) == 0)
				{
					// Received a WebHook from SendGrid!
					Business.Main.SendGridData emailReceived = new Business.Main.SendGridData();
					//Business.SendGridBO.EmailList emails = new Business.SendGridBO.EmailList();

					try
					{
						// Get JSON from WebHook
						emailReceived.Request = context.Request.ToString();
						//dynamic stuff1 = Newtonsoft.Json.JsonConvert.DeserializeObject(WebApiApplication.documentContents);
						//string Text = stuff1[0];

						emailReceived.JSON = TextUs.WebApiApplication.documentContents;

						dynamic dynamJSON = Newtonsoft.Json.JsonConvert.DeserializeObject(WebApiApplication.documentContents);

						//emailReceived.Dict = js.Deserialize<Dictionary<string, string>>(TextUs.WebApiApplication.documentContents);
						//emailReceived.DictList = js.Deserialize<List<Dictionary<string, string>>>(dynamJSON.ToString());

						//List <Business.SendGridBO.EmailEvent> asdf = js.Deserialize<List<Business.SendGridBO.EmailEvent>>(dynamJSON.ToString());
						emailReceived.Events = js.Deserialize<List<Business.SendGridBO.EmailEvent>>(dynamJSON.ToString());

						
						//System.IO.MemoryStream mStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(WebApiApplication.documentContents.ToString()));
						//System.IO.Stream stream = mStream;
						//emailReceived.Events = DeserializeJson<List<Business.SendGridBO.EmailEvent>>(stream);

						//TextUs.Business.Main.SendGridData.jsonDataTable jsonArray =
						//	js.Deserialize<TextUs.Business.Main.SendGridData.jsonDataTable>(TextUs.WebApiApplication.documentContents);
						processResult = _busMain.ProcessEmail(emailReceived);

						//response.StatusCode = (processResult == Business.Main.ProcessResultEnum.OK || processResult == Business.Main.ProcessResultEnum.Duplicate) ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
						switch (processResult)
						{
							case Business.Main.ProcessResultEnum.OK:
								response.StatusCode = HttpStatusCode.OK;
								result = true;
								break;
							case Business.Main.ProcessResultEnum.Duplicate:
								response.StatusCode = HttpStatusCode.OK;
								result = true;
								break;
							case Business.Main.ProcessResultEnum.PhoneNumberNotFound:
								response.StatusCode = HttpStatusCode.OK;
								result = true;
								break;
							case Business.Main.ProcessResultEnum.TimeOut:
								response.StatusCode = HttpStatusCode.InternalServerError;
								result = false;
								break;
							case Business.Main.ProcessResultEnum.Error:
								response.StatusCode = HttpStatusCode.InternalServerError;
								result = false;
								break;
							default:
								response.StatusCode = HttpStatusCode.InternalServerError;
								result = false;
								break;
						}

						if (HelperFunctions.IsTraceOn)
						{
							try
							{
								if (processResult != Business.Main.ProcessResultEnum.OK)     // if (HelperFunctions.IsTest && !success)
								{
									HelperFunctions.WriteTraceEntry(string.Format("SendGrid Email (DkIm: {0}) has finished with {1} ... Response: {2}. Return: {3}.",
										emailReceived.DkIm, processResult.ToString(), response.ToString(), result.ToString()
										));
								}
							}
							catch
							{
								// Eat the error trying to write to the log
							}
						}
					}
					catch (Exception ex)
					{
						processResult = Business.Main.ProcessResultEnum.Error;
						try
						{   // Attempt to add as much info to the exeception as exists
							ex.Data.Add("systemAppName", HelperFunctions.SystemAppName);
							ex.Data.Add("systemMethodName", System.Reflection.MethodBase.GetCurrentMethod().Name);
							ex.Data.Add("Request in", emailReceived.Request.ToString());
							ex.Data.Add("JSON in", emailReceived.JSON.ToString());
							HelperFunctions.AddInfo(ex, "ProcessEmail data", emailReceived.Display);
							HelperFunctions.AddInfo(ex, "Request", emailReceived.Request);
							HelperFunctions.AddInfo(ex, "JSON", emailReceived.JSON);

							if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Exception encountered. " + ex.Message); }
						}
						catch
						{
							msg = string.Format("SendGrid message ({0}) error adding execetion info", emailReceived.DkIm);
							if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
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
					}
				}
				else
				{
					//  "i"  ===>  Received a WebHook from IFTTT!
					//  "z"  ===>  Received a WebHook from Zapier!
					if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Unknown WebHook id: " + context.Id); }
				}


			}
			catch (Exception ex)
			{
				ex.Data.Add("systemAppName", HelperFunctions.SystemAppName);
				ex.Data.Add("systemMethodName", System.Reflection.MethodBase.GetCurrentMethod().Name);
				ex.Data.Add("Generator in", generator);
				ex.Data.Add("Request in", context.Request.ToString());
				ex.Data.Add("JSON in", context.Data.ToString());
				HelperFunctions.AddInfo(ex, "Generator", generator);
				HelperFunctions.AddInfo(ex, "Request", context.Request.ToString());
				HelperFunctions.AddInfo(ex, "JSON", context.Data.ToString());
				HelperFunctions.exLog.Publish(ex, "ExecuteAsync Error", true);
			}

			//return Task.FromResult(response);
			if (result)
			{
				return Task.FromResult(result);
			}
			else
			{
				//return Task.FromResult(false);		This still ended up returning a 200 (OK) response to the sender.  Idunno why
				return null;
			}
		}
		public static T DeserializeJson<T>(System.IO.Stream stream) where T : class
		{
			System.Runtime.Serialization.Json.DataContractJsonSerializerSettings settings = new System.Runtime.Serialization.Json.DataContractJsonSerializerSettings();
			settings.UseSimpleDictionaryFormat = true;

			System.Runtime.Serialization.Json.DataContractJsonSerializer jsonSerializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T), settings);
			return jsonSerializer.ReadObject(stream) as T;
		}
	}
}