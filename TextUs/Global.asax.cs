using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Script.Serialization;
using TextUs.Common;

namespace TextUs
{
	public class WebApiApplication: System.Web.HttpApplication
	{
		//public static Dictionary<string, string> msgDict;
		public static string documentContents;

		protected void Application_BeginRequest()
		{
			if (!Request.HttpMethod.Equals("post", StringComparison.InvariantCultureIgnoreCase))
			{
				return;
			}

			//JavaScriptSerializer js = new JavaScriptSerializer();
			using (var receiveStream = Request.InputStream)
			{
				using (var readStream = new StreamReader(receiveStream, Encoding.UTF8))
				{
					documentContents = readStream.ReadToEnd();
				}
			}

			//try
			//{				
			//	Dictionary<string, string> msgDict = js.Deserialize<Dictionary<string, string>>(documentContents.ToString());

			//	var json = JObject.Parse(documentContents);
			//	File.WriteAllLines(@"C:\test\keys.txt", new[] { documentContents, "\r\n", json.ToString() });
			//}
			//catch (Exception ex)
			//{
			//	// do something
			//	int i = 0;
			//}

		}
		protected void Application_Start()
		{
			HelperFunctions.SystemAppName = System.Reflection.Assembly.GetExecutingAssembly().ManifestModule.Name.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
			HelperFunctions.IsTraceOn = Properties.Settings.Default.TraceMessages;
			HelperFunctions.IsTest = Properties.Settings.Default.UseTestDB;

			AreaRegistration.RegisterAllAreas();
			GlobalConfiguration.Configure(WebApiConfig.Register);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);

			
			//Examples:

			//Inbound Message:
			//  {
			//    "id": 1709900,
			//    "body": "Hello",
			//    "read": true,
			//    "broadcast_id": null,
			//    "status": "created",
			//    "created_at": "2015-04-03 19:26:06 UTC",
			//    "sender": 384213,
			//    "sender_type": "Contact",
			//    "sender_phone": "+13032632025",
			//    "recipient": 21,
			//    "recipient_type": "Account",
			//    "recipient_phone": "+13034423223"
			//  }
			//Outbound Message:
			//  {
			//    "id": 1709891,
			//    "body": "yo\n",
			//    "read": true,
			//    "broadcast_id": null,
			//    "status": "created",
			//    "created_at": "2015-04-03 19:25:37 UTC",
			//    "sender": 79,
			//    "sender_type": "User",
			//    "sender_phone": "+13034423223",
			//    "recipient": 384213,
			//    "recipient_type": "Contact",--qps
			//    "recipient_phone": "+13032632025"
			//  }

			//Business.Main message = new Business.Main();
			//Business.Main.TextUsData textReceived = new Business.Main.TextUsData();
			//bool success = false;

			//textReceived.Body = "We have a job for a widget maker.  Are you interested?";
			//textReceived.EmployeePhoneNumber = "4147325733";        //Invalid number: 4147325733  --  Multiple Employees number: 2624566121
			//textReceived.Source = Business.Main.TextUsData.TextSourceEnum.QPS;
			//textReceived.TextDate = DateTime.Now;
			//textReceived.Request = "{testing request from QPS}";
			//textReceived.JSON = "{testing JSON data from QPS}";

			//success = message.ProcessText(textReceived);

			//if (success)
			//{
			//	textReceived.Body = "Yes, I will take the job!";
			//	textReceived.EmployeePhoneNumber = "4147325733";        //Invalid number: 4147325733  --  Multiple Employees number: 2624566121
			//	textReceived.Source = Business.Main.TextUsData.TextSourceEnum.AE;
			//	textReceived.TextDate = DateTime.Now;
			//	textReceived.Request = "{testing request from the AE}";
			//	textReceived.JSON = "{testing JSON data from the AE}";

			//	success = message.ProcessText(textReceived);
			//}

			//if (success)
			//{
			//	int i = 0;
			//}
		}
	}
}
