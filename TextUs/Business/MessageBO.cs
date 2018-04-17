using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextUs.Common;
using TextUs.Data;
using TextUs.DataSets;

namespace TextUs.Business
{
	public class MessageBO
	{

		private MessageDO _dataObj = new MessageDO();

		#region Constants

		private const int c_ActionTypeConfigChoiceID = 4578;
		private const bool c_AllDayEvent = false;
		private const bool c_IsComplete = false;
		private const bool c_IsSharedAppointment = false;
		private const int c_MessageTypeConfigSystemChoiceID = 64;
		private const bool c_ReminderEnabled = false;
		private const int c_ReminderInterval = 0;
		private const bool c_ShowOnWebPortal = false;
		private const int c_ReminderUnits = 0;
		private const string c_StaffingActionOrigin = "Employee";

		#endregion

		#region Properties

		//public enum SourceEnum
		//{
		//	QPS = 0, AE = 1
		//}

		public struct TextData
		{
			public DateTime DateEntered;
			public DateTime CreateDate;
			public string PhoneNumber;
			public string Subject;
			public string Body;
			//public SourceEnum Source;
			public int EmployeeID;
			public Guid EmployeeGUID;
			public Guid StaffingActionGUID;
			public string StaffingActionOriginValue;
			public string ExternalId;
		}

		public MessageDS Dataset
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

		public Guid AddStaffingAction(MessageBO reference, TextData dataIn)
		{
			Guid result = Guid.Empty;
			MessageDS.StaffingActionRow newMessage = null;

			try
			{
				newMessage = _dataObj.Dataset.StaffingAction.NewStaffingActionRow();
				newMessage.StaffingActionGUID = Guid.NewGuid();
				newMessage.ActionTypeComments = dataIn.Body;
				newMessage.ActionTypeConfigChoiceID = c_ActionTypeConfigChoiceID;
				newMessage.AllDayEvent = c_AllDayEvent;
				//newMessage.AllProperties  ---  default to null
				newMessage.StartDateTime = dataIn.CreateDate;
				newMessage.EndDateTime = dataIn.CreateDate.AddMinutes(30);
				//newMessage.ExternalDB  ---  default to null
				newMessage.ExternalID = dataIn.ExternalId;
				//newMessage.ExternalLabel  ---  default to null
				newMessage.IsComplete = c_IsComplete;
				newMessage.IsSharedAppointment = c_IsSharedAppointment;
				//newMessage.Location  ---  default to null
				newMessage.MessageTypeConfigSystemChoiceID = c_MessageTypeConfigSystemChoiceID;
				newMessage.ModifiedDate = dataIn.DateEntered;
				newMessage.OriginalStartDateTime = dataIn.CreateDate;
				//newMessage.ParentGUID  ---  default to null
				//newMessage.Recurrence  ---  default to null
				//newMessage.RecurrenceGUID  ---  default to null
				newMessage.ReminderEnabled = c_ReminderEnabled;
				newMessage.ReminderInterval = c_ReminderInterval;
				newMessage.ReminderUnits = c_ReminderUnits;
				//newMessage.RootStaffingActionGUID  ---  default to null
				newMessage.Subject = dataIn.Subject;
				newMessage.UserGUID = Properties.Settings.Default.QPS_UserId;
				_dataObj.Dataset.StaffingAction.AddStaffingActionRow(newMessage);
				result = newMessage.StaffingActionGUID;
				dataIn.StaffingActionGUID = result;
			}
			catch (Exception ex)
			{
				throw ex;
			}

			return result;
		}

		public Guid AddStaffingActionFK(MessageBO reference, TextData dataIn)
		{
			Guid result = Guid.Empty;
			MessageDS.StaffingActionFKRow newMessage = null;

			try
			{
				newMessage = _dataObj.Dataset.StaffingActionFK.NewStaffingActionFKRow();
				newMessage.StaffingActionFKGUID = Guid.NewGuid();
				newMessage.StaffingActionGUID = dataIn.StaffingActionGUID;
				newMessage.ContactMethod = String.Format("{0:(###) ###-####}", double.Parse(dataIn.PhoneNumber));
				newMessage.DateEntered = dataIn.DateEntered;
				newMessage.FKGUID = dataIn.EmployeeGUID;
				//newMessage.MessageOpenDate  ---  default to null
				newMessage.ShowOnWebPortal = c_ShowOnWebPortal;
				newMessage.StaffingActionOrigin = c_StaffingActionOrigin;
				newMessage.StaffingActionOriginValue = dataIn.StaffingActionOriginValue;
				newMessage.StaffingActionOriginValueID = dataIn.EmployeeID;
				newMessage.UserGUID = Properties.Settings.Default.QPS_UserId;
				_dataObj.Dataset.StaffingActionFK.AddStaffingActionFKRow(newMessage);
				result = newMessage.StaffingActionFKGUID;
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

		public MessageDS.StaffingActionRow GetStaffingActionByExternalIdAndChoiceID(string externalId, int configChoiceID = -1)
		{
			MessageDS.StaffingActionRow result = null;

			MessageDS data;
			string msg = string.Empty;

			try
			{
				if (configChoiceID < 1) { configChoiceID = c_ActionTypeConfigChoiceID;  };
				_dataObj.OpenConnection();
				data = _dataObj.GetStaffingActionByExternalIdAndChoiceID(externalId, configChoiceID);

				if (data != null && data.Tables.Count > 0)
				{
					switch (data.StaffingAction.Rows.Count)
					{
						case 0:
							break;
						case 1:
							result = (MessageDS.StaffingActionRow)data.StaffingAction.Rows[0];
							break;
						default:
							try
							{
								result = (MessageDS.StaffingActionRow)data.StaffingAction.Rows[0];
								msg = string.Format("Multiple messages (StaffingAction records) have been found for ExternalId:{0}.  Query returned {1} rows when 1 was expected.",
									externalId, data.StaffingAction.Rows.Count);
								HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Warning, msg);
								if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
							}
							catch
							{
								// skip the failure while attempting to add extra trace info.
							}
							break;
					}
				}
			}
			catch (Exception ex)
			{
				try
				{   // Attempt to add as much info to the exeception as exists
					HelperFunctions.AddInfo(ex, "Get message (Staffing Action) by ExternalId#: ", externalId);
				}
				catch
				{
					// Eat problems with adding execetion info.
				}
				throw ex;
			}
			finally
			{
				_dataObj.CloseConnection();
			}

			return result;
		}

		public System.Data.DataTable GetEmployeesByPhoneNumber(Int64 phoneNumber)
		{
			System.Data.DataTable result = null;
			string msg = string.Empty;

			try
			{
				_dataObj.OpenConnection();
				result = _dataObj.GetEmployeesByPhoneNumber(phoneNumber);

				if (result == null)
				{
					try
					{
						msg = string.Format("Employee phone number ({0}) query did not return any data.", phoneNumber);
						HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Warning, msg);
						if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
					}
					catch
					{
						// skip the failure while attempting to add extra trace info.
					}
				}
			}
			catch (Exception ex)
			{
				try
				{   // Attempt to add as much info to the exeception as exists
					HelperFunctions.AddInfo(ex, "Get AEs by phone#: ", phoneNumber.ToString());
				}
				catch
				{
					// Eat problems with adding execetion info.
				}
				throw ex;
			}
			finally
			{
				_dataObj.CloseConnection();
			}

			return result;
		}

		#endregion

	}
}