using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextUs.Common;
using TextUs.Data;
using TextUs.DataSets;
using Newtonsoft.Json;

namespace TextUs.Business
{
	public class SendGridBO
	{
		private SendGridDO _dataObj = new SendGridDO();

		#region Constants

		private const int c_zero = 0;

		#endregion

		#region Properties

		public SendGridDS Dataset
		{
			get
			{
				return _dataObj.Dataset;
			}
			set
			{
				_dataObj.Dataset = value;
			}
		}

		#endregion

		#region Load/Save
		public bool Load(Guid StaffingActionFKGUID)
		{
			bool result = false;
			try
			{
				result = _dataObj.GetData(StaffingActionFKGUID);
			}
			catch (Exception ex)
			{
				//Trace.WriteLineIf(HelperFunctions.HelperFunctionsSwitch.TraceError,
				//	string.Format("Load has failed. Error:{0}", ex.ToString()));
				throw ex;
			}

			return result;
		}

		public bool Save()
		{
			try
			{
				//int STOP = 1; if (STOP == 1) { throw new System.ApplicationException("STOP!!!  We are not yet ready to update the database!"); }
				_dataObj.OpenConnection();
				return _dataObj.SetData();
			}
			finally
			{
				_dataObj.CloseConnection();
			}
		}

		#endregion

		#region Add Methods	

		public bool AddSendGridEmail(SendGridBO reference, SendGridBO.EmailEvent dataIn)
		{
			bool result = false;
			SendGridDS.QPS_SendGridRow newEmail = null;

			try
			{
				newEmail = _dataObj.Dataset.QPS_SendGrid.NewQPS_SendGridRow();
				newEmail.SendGridGUID = Guid.NewGuid();
				newEmail.SendGridID = 0;
				newEmail.sg_Asm_Group_Id = dataIn.Asm_Group_Id;
				newEmail.sg_Attempt = dataIn.Attempt;
				newEmail.sg_Category = dataIn.Category;
				newEmail.sg_Email = dataIn.Email;
				newEmail.sg_Event = dataIn.Event;
				newEmail.sg_Event_Id = dataIn.Event_Id;
				newEmail.sg_IP = dataIn.IP;
				newEmail.sg_Message_Id = dataIn.Message_Id;
				newEmail.sg_Response = dataIn.Response;
				newEmail.sg_Smtp_Id = dataIn.Smtp_Id;
				newEmail.sg_Status = dataIn.Status;
				newEmail.sg_Timestamp = dataIn.Timestamp;
				newEmail.sg_URL = dataIn.URL;
				newEmail.sg_Useragent = dataIn.UserAgent;
				//newEmail.TBD  ---  default to null
				newEmail.UserGUID = Properties.Settings.Default.QPS_UserId;
				newEmail.DateEntered = DateTime.Now;
				_dataObj.Dataset.QPS_SendGrid.AddQPS_SendGridRow(newEmail);
				//result = newEmail.SendGridGUID;
				//dataIn.SendGridGUID = result;
				//dataIn.SendGridID = result;

				result = true;
			}
			catch (Exception ex)
			{
				throw ex;
			}

			return result;
		}

		#endregion

		#region Misc Methods

		public void OpenConnection()
		{
			_dataObj.OpenConnection();
		}

		public void CloseConnection()
		{
			_dataObj.CloseConnection();
		}

		#endregion

		#region Sub Classes

		public class EmailList
		{
			public List<EmailEvent> Events { get; set; }
		}
		public class EmailEvent
		{
			[JsonProperty(PropertyName = "email")]
			public string Email { get; set; }

			[JsonProperty(PropertyName = "timestamp")]
			public string Timestamp { get; set; }

			[JsonProperty(PropertyName = "smtp-id")]
			public string Smtp_Id { get; set; }

			[JsonProperty(PropertyName = "event")]
			public string Event { get; set; }

			[JsonProperty(PropertyName = "category")]
			public string Category { get; set; }

			[JsonProperty(PropertyName = "sg_event_id")]
			public string Event_Id { get; set; }

			[JsonProperty(PropertyName = "sg_message_id")]
			public string Message_Id { get; set; }

			[JsonProperty(PropertyName = "response")]
			public string Response { get; set; }

			[JsonProperty(PropertyName = "attempt")]
			public string Attempt { get; set; }

			[JsonProperty(PropertyName = "useragent")]
			public string UserAgent { get; set; }

			[JsonProperty(PropertyName = "ip")]
			public string IP { get; set; }

			[JsonProperty(PropertyName = "url")]
			public string URL { get; set; }

			[JsonProperty(PropertyName = "status")]
			public string Status { get; set; }

			[JsonProperty(PropertyName = "asm_group_id")]
			public string Asm_Group_Id { get; set; }

			[JsonProperty(PropertyName = "url_offset")]
			public URL_Offset Offset { get; set; }
		}
		public class URL_Offset
		{
			[JsonProperty(PropertyName = "index")]
			public string Index { get; set; }

			[JsonProperty(PropertyName = "type")]
			public string Yype { get; set; }
		}

		#endregion


	}
}