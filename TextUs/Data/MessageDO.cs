using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using TextUs.Common;
using TextUs.DataSets;
using Microsoft.ApplicationBlocks.Data;

namespace TextUs.Data
{
	public class MessageDO
	{
		private SqlConnection _conn;
		private MessageDS _data;

		#region Properties

		public MessageDS Dataset
		{
			get
			{
				if (_data == null) { RefreshDataset(); }
				return _data;
			}
			set
			{
				_data = value;
			}
		}

		public SqlConnection Connection
		{
			get
			{
				if (_conn == null)
				{
					//if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Creating the connection"); }
					_conn = HelperFunctions.DbConnection();
				}
				return _conn;
			}
			set
			{
				_conn = value;
			}
		}

		#endregion

		#region Get methods

		public bool GetData(Guid StaffingActionFKGUID, bool withRefresh = true)
		{
			bool result = true;
			SqlDataAdapter daStaffingAction = null;
			SqlDataAdapter daStaffingActionFK = null;
			MessageDS.StaffingActionFKRow actionFKRow = null;
			string msg = string.Empty;

			try
			{
				daStaffingActionFK = AdapterStaffingActionFK(Connection);
				daStaffingActionFK.SelectCommand.Parameters["@StaffingActionFKGUID"].Value = StaffingActionFKGUID;
				daStaffingAction = AdapterStaffingAction(Connection);

				if (withRefresh) { RefreshDataset(); }
				Dataset.EnforceConstraints = false;
				daStaffingActionFK.Fill(Dataset.StaffingActionFK);
				switch (Dataset.StaffingActionFK.Rows.Count)
				{
					case 0:
						//claim number doesn't yet exist.
						result = false;
						break;
					case 1:
						actionFKRow = (MessageDS.StaffingActionFKRow)Dataset.StaffingActionFK.Rows[0];
						daStaffingAction.SelectCommand.Parameters["@StaffingActionGUID"].Value = actionFKRow.StaffingActionGUID;
						daStaffingAction.Fill(Dataset.StaffingAction);
						break;
					default:
						result = false;
						msg = string.Format("StaffingActionFKGUID ({0}) returned {1} {2} records.  Zero or one was expected",
							StaffingActionFKGUID.ToString(), Dataset.StaffingActionFK.Rows.Count, Dataset.StaffingActionFK.TableName);
						HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Warning, msg);
						if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
						break;
				}
				Dataset.EnforceConstraints = true;
			}
			catch (Exception ex)
			{
				result = false;
				if (daStaffingAction != null) { HelperFunctions.AddInfo(ex, "daStaffingAction.sql", daStaffingAction.SelectCommand.CommandText); }
				if (daStaffingActionFK != null) { HelperFunctions.AddInfo(ex, "daStaffingActionFK.sql", daStaffingActionFK.SelectCommand.CommandText); }

				throw ex;
			}
			//finally
			//{
			//	CloseConnection();
			//}

			return result;
		}
		#endregion

		#region Set methods

		public bool SetData()
		{
			return SetData(Dataset);
		}

		private bool SetData(MessageDS dataIn)
		{
			bool result = false;
			SqlTransaction tx = null;
			Guid actionFkGuid = Guid.Empty;

			try
			{
				actionFkGuid = ((MessageDS.StaffingActionFKRow)Dataset.StaffingActionFK.Rows[0]).StaffingActionFKGUID;
				tx = Connection.BeginTransaction();
				result = SetData(dataIn, Connection, tx);
				tx.Commit();
				tx.Dispose();
				tx = null;

				GetData(actionFkGuid);
			}
			catch (Exception ex)
			{
				result = false;
				if ((tx != null))
				{
					try
					{
						tx.Rollback();
					}
					catch
					{
						//Swallow Rollback Exception
					}
				}
				throw ex;
			}
			finally
			{
				//CloseConnection();
				if ((tx != null))
				{
					tx.Dispose();
				}
			}

			return result;
		}

		private bool SetData(MessageDS dataIn, SqlConnection cn, SqlTransaction tx)
		{
			bool result = false;
			MessageDS delsDS = null;
			MessageDS modsDS = null;
			SqlDataAdapter daStaffingAction = null;
			SqlDataAdapter daStaffingActionFK = null;

			if (cn == null || tx == null)
			{
				throw new ArgumentException();
			}

			try
			{
				daStaffingAction = AdapterStaffingAction(cn, tx);
				daStaffingActionFK = AdapterStaffingActionFK(cn, tx);

				delsDS = (MessageDS)dataIn.GetChanges(DataRowState.Deleted);
				modsDS = (MessageDS)dataIn.GetChanges(DataRowState.Modified | DataRowState.Added);

				dataIn.Clear();

				//Take care of the deletes first, mindful of the fk constraints.
				if ((delsDS != null))
				{
					delsDS.EnforceConstraints = false;
					daStaffingAction.Update(delsDS, "StaffingAction");
					daStaffingActionFK.Update(delsDS, "StaffingActionFK");
					dataIn.Merge(delsDS);
					delsDS.EnforceConstraints = true;
				}

				if ((modsDS != null))
				{
					daStaffingAction.Update(modsDS, "StaffingAction");
					daStaffingActionFK.Update(modsDS, "StaffingActionFK");
					modsDS.EnforceConstraints = false;
					dataIn.Merge(modsDS);
					modsDS.EnforceConstraints = true;
				}
				result = true;
			}
			catch (Exception ex)
			{
				result = false;
				if (daStaffingAction != null) { HelperFunctions.AddInfo(ex, "daStaffingAction.sql", daStaffingAction.SelectCommand.CommandText); }
				if (daStaffingActionFK != null) { HelperFunctions.AddInfo(ex, "daStaffingActionFK.sql", daStaffingActionFK.SelectCommand.CommandText); }

				// check for database errors.
				StringBuilder errors = null;
				if ((modsDS != null))
				{
					HelperFunctions.DebugDataSetErrors(errors, modsDS);
				}
				if ((delsDS != null))
				{
					HelperFunctions.DebugDataSetErrors(errors, delsDS);
				}
				HelperFunctions.DebugDataSetErrors(errors, dataIn);

				if (errors != null && errors.Length > 0) { HelperFunctions.AddInfo(ex, "Database Error(s)", errors.ToString()); }

				throw ex;
			}

			return result;
		}
		#endregion

		#region Adapter methods

		private SqlDataAdapter AdapterStaffingAction(SqlConnection conn, SqlTransaction tx = null)
		{
			SqlDataAdapter result = null;
			SqlCommand cmdCreate;
			SqlCommand cmdRetrieve;
			SqlCommand cmdUpdate;
			SqlCommand cmdDelete;

			try
			{
				cmdCreate = new SqlCommand("QPS_StaffingAction_Insert", conn, tx);
				cmdCreate.CommandType = CommandType.StoredProcedure;
				cmdCreate.Parameters.Add("@StaffingActionGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "StaffingActionGUID");
				cmdCreate.Parameters.Add("@ActionTypeConfigChoiceID", System.Data.SqlDbType.Int, 32, "ActionTypeConfigChoiceID");
				cmdCreate.Parameters.Add("@ActionTypeComments", System.Data.SqlDbType.VarChar, 8000, "ActionTypeComments");
				cmdCreate.Parameters.Add("@StartDateTime", System.Data.SqlDbType.SmallDateTime, 4, "StartDateTime");
				cmdCreate.Parameters.Add("@EndDateTime", System.Data.SqlDbType.SmallDateTime, 4, "EndDateTime");
				cmdCreate.Parameters.Add("@UserGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "UserGUID");
				cmdCreate.Parameters.Add("@IsComplete", System.Data.SqlDbType.Bit, 1, "IsComplete");
				cmdCreate.Parameters.Add("@Subject", System.Data.SqlDbType.VarChar, 255, "Subject");
				cmdCreate.Parameters.Add("@AllDayEvent", System.Data.SqlDbType.Bit, 1, "AllDayEvent");
				cmdCreate.Parameters.Add("@OriginalStartDateTime", System.Data.SqlDbType.SmallDateTime, 4, "OriginalStartDateTime");
				cmdCreate.Parameters.Add("@RecurrenceGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "RecurrenceGUID");
				cmdCreate.Parameters.Add("@Recurrence", System.Data.SqlDbType.VarBinary, 8000, "Recurrence");
				cmdCreate.Parameters.Add("@ReminderEnabled", System.Data.SqlDbType.Bit, 1, "ReminderEnabled");
				cmdCreate.Parameters.Add("@ReminderInterval", System.Data.SqlDbType.Int, 32, "ReminderInterval"); ;
				cmdCreate.Parameters.Add("@ReminderUnits", System.Data.SqlDbType.Int, 32, "ReminderUnits");
				cmdCreate.Parameters.Add("@AllProperties", System.Data.SqlDbType.VarBinary, 8000, "AllProperties");
				cmdCreate.Parameters.Add("@Location", System.Data.SqlDbType.VarChar, 500, "Location");
				cmdCreate.Parameters.Add("@ExternalID", System.Data.SqlDbType.VarChar, 255, "ExternalID");
				cmdCreate.Parameters.Add("@IsSharedAppointment", System.Data.SqlDbType.Bit, 1, "IsSharedAppointment");
				cmdCreate.Parameters.Add("@ExternalDB", System.Data.SqlDbType.VarChar, 50, "ExternalDB");
				cmdCreate.Parameters.Add("@MessageTypeConfigSystemChoiceID", System.Data.SqlDbType.Int, 32, "MessageTypeConfigSystemChoiceID");
				cmdCreate.Parameters.Add("@ExternalLabel", System.Data.SqlDbType.VarChar, 50, "ExternalLabel");
				cmdCreate.Parameters.Add("@ParentGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "ParentGUID");
				cmdCreate.Parameters.Add("@RootStaffingActionGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "RootStaffingActionGUID");
				cmdCreate.Parameters.Add("@ModifiedDate", System.Data.SqlDbType.SmallDateTime, 4, "ModifiedDate");
				
				cmdRetrieve = new SqlCommand("QPS_StaffingAction_Select", conn, tx);
				cmdRetrieve.CommandType = CommandType.StoredProcedure;
				cmdRetrieve.Parameters.Add("@StaffingActionGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "StaffingActionGUID");

				cmdUpdate = new SqlCommand("QPS_StaffingAction_Update", conn, tx);
				cmdUpdate.CommandType = CommandType.StoredProcedure;
				cmdUpdate.Parameters.Add("@StaffingActionGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "StaffingActionGUID");
				cmdUpdate.Parameters.Add("@ActionTypeConfigChoiceID", System.Data.SqlDbType.Int, 32, "ActionTypeConfigChoiceID");
				cmdUpdate.Parameters.Add("@ActionTypeComments", System.Data.SqlDbType.VarChar, 8000, "ActionTypeComments");
				cmdUpdate.Parameters.Add("@StartDateTime", System.Data.SqlDbType.SmallDateTime, 4, "StartDateTime");
				cmdUpdate.Parameters.Add("@EndDateTime", System.Data.SqlDbType.SmallDateTime, 4, "EndDateTime");
				cmdUpdate.Parameters.Add("@UserGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "UserGUID");
				cmdUpdate.Parameters.Add("@IsComplete", System.Data.SqlDbType.Bit, 1, "IsComplete");
				cmdUpdate.Parameters.Add("@Subject", System.Data.SqlDbType.VarChar, 255, "Subject");
				cmdUpdate.Parameters.Add("@AllDayEvent", System.Data.SqlDbType.Bit, 1, "AllDayEvent");
				cmdUpdate.Parameters.Add("@OriginalStartDateTime", System.Data.SqlDbType.SmallDateTime, 4, "OriginalStartDateTime");
				cmdUpdate.Parameters.Add("@RecurrenceGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "RecurrenceGUID");
				cmdUpdate.Parameters.Add("@Recurrence", System.Data.SqlDbType.VarChar, 8000, "Recurrence");
				cmdUpdate.Parameters.Add("@ReminderEnabled", System.Data.SqlDbType.Bit, 1, "ReminderEnabled");
				cmdUpdate.Parameters.Add("@ReminderInterval", System.Data.SqlDbType.Int, 32, "ReminderInterval"); ;
				cmdUpdate.Parameters.Add("@ReminderUnits", System.Data.SqlDbType.Int, 32, "ReminderUnits");
				cmdUpdate.Parameters.Add("@AllProperties", System.Data.SqlDbType.VarChar, 8000, "AllProperties");
				cmdUpdate.Parameters.Add("@Location", System.Data.SqlDbType.VarChar, 500, "Location");
				cmdUpdate.Parameters.Add("@ExternalID", System.Data.SqlDbType.VarChar, 255, "ExternalID");
				cmdUpdate.Parameters.Add("@IsSharedAppointment", System.Data.SqlDbType.Bit, 1, "IsSharedAppointment");
				cmdUpdate.Parameters.Add("@ExternalDB", System.Data.SqlDbType.VarChar, 50, "ExternalDB");
				cmdUpdate.Parameters.Add("@MessageTypeConfigSystemChoiceID", System.Data.SqlDbType.Int, 32, "MessageTypeConfigSystemChoiceID");
				cmdUpdate.Parameters.Add("@ExternalLabel", System.Data.SqlDbType.VarChar, 50, "ExternalLabel");
				cmdUpdate.Parameters.Add("@ParentGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "ParentGUID");
				cmdUpdate.Parameters.Add("@RootStaffingActionGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "RootStaffingActionGUID");
				cmdUpdate.Parameters.Add("@ModifiedDate", System.Data.SqlDbType.SmallDateTime, 4, "ModifiedDate");
				
				cmdDelete = new SqlCommand("QPS_StaffingAction_Delete", conn, tx);
				cmdDelete.CommandType = CommandType.StoredProcedure;
				cmdDelete.Parameters.Add("@StaffingActionGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "StaffingActionGUID");

				result = new SqlDataAdapter(cmdRetrieve);
				result.InsertCommand = cmdCreate;
				result.UpdateCommand = cmdUpdate;
				result.DeleteCommand = cmdDelete;
			}
			catch (Exception ex)
			{
				if ((tx != null))
				{
					try
					{
						tx.Rollback();
					}
					catch
					{
						//Swallow Rollback Exception
					}
				}

				throw ex;
			}

			return result;
		}

		private SqlDataAdapter AdapterStaffingActionFK(SqlConnection conn, SqlTransaction tx = null)
		{
			SqlDataAdapter result = null;
			SqlCommand cmdCreate;
			SqlCommand cmdRetrieve;
			SqlCommand cmdUpdate;
			SqlCommand cmdDelete;
			//string sql;

			try
			{

				cmdCreate = new SqlCommand("QPS_StaffingActionFK_Insert", conn, tx);
				cmdCreate.CommandType = CommandType.StoredProcedure;
				cmdCreate.Parameters.Add("@StaffingActionFKGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "StaffingActionFKGUID");
				cmdCreate.Parameters.Add("@StaffingActionGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "StaffingActionGUID");
				cmdCreate.Parameters.Add("@FKGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "FKGUID");
				cmdCreate.Parameters.Add("@StaffingActionOrigin", System.Data.SqlDbType.VarChar, 255, "StaffingActionOrigin");
				cmdCreate.Parameters.Add("@StaffingActionOriginValue", System.Data.SqlDbType.VarChar, 255, "StaffingActionOriginValue");
				cmdCreate.Parameters.Add("@StaffingActionOriginValueID", System.Data.SqlDbType.Int, 32, "StaffingActionOriginValueID");
				cmdCreate.Parameters.Add("@ContactMethod", System.Data.SqlDbType.VarChar, 255, "ContactMethod");
				cmdCreate.Parameters.Add("@UserGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "UserGUID");
				cmdCreate.Parameters.Add("@DateEntered", System.Data.SqlDbType.SmallDateTime, 4, "DateEntered");
				cmdCreate.Parameters.Add("@ShowOnWebPortal", System.Data.SqlDbType.Bit, 1, "ShowOnWebPortal");
				cmdCreate.Parameters.Add("@MessageOpenDate", System.Data.SqlDbType.SmallDateTime, 4, "MessageOpenDate");

				cmdRetrieve = new SqlCommand("QPS_StaffingActionFK_Select", conn, tx);
				cmdRetrieve.CommandType = CommandType.StoredProcedure;
				cmdRetrieve.Parameters.Add("@StaffingActionFKGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "StaffingActionFKGUID");

				cmdUpdate = new SqlCommand("QPS_StaffingActionFK_Update", conn, tx);
				cmdUpdate.CommandType = CommandType.StoredProcedure;
				cmdUpdate.Parameters.Add("@StaffingActionFKGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "StaffingActionFKGUID");
				cmdUpdate.Parameters.Add("@StaffingActionGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "StaffingActionGUID");
				cmdUpdate.Parameters.Add("@FKGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "FKGUID");
				cmdUpdate.Parameters.Add("@StaffingActionOrigin", System.Data.SqlDbType.VarChar, 255, "StaffingActionOrigin");
				cmdUpdate.Parameters.Add("@StaffingActionOriginValue", System.Data.SqlDbType.VarChar, 255, "StaffingActionOriginValue");
				cmdUpdate.Parameters.Add("@StaffingActionOriginValueID", System.Data.SqlDbType.Int, 32, "StaffingActionOriginValueID");
				cmdUpdate.Parameters.Add("@ContactMethod", System.Data.SqlDbType.VarChar, 255, "ContactMethod");
				cmdUpdate.Parameters.Add("@UserGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "UserGUID");
				cmdUpdate.Parameters.Add("@DateEntered", System.Data.SqlDbType.SmallDateTime, 4, "DateEntered");
				cmdUpdate.Parameters.Add("@ShowOnWebPortal", System.Data.SqlDbType.Bit, 1, "ShowOnWebPortal");
				cmdUpdate.Parameters.Add("@MessageOpenDate", System.Data.SqlDbType.SmallDateTime, 4, "MessageOpenDate");

				cmdDelete = new SqlCommand("QPS_StaffingActionFK_Delete", conn, tx);
				cmdDelete.CommandType = CommandType.StoredProcedure;
				cmdDelete.Parameters.Add("@StaffingActionFKGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "StaffingActionFKGUID");

				result = new SqlDataAdapter(cmdRetrieve);
				result.InsertCommand = cmdCreate;
				result.UpdateCommand = cmdUpdate;
				result.DeleteCommand = cmdDelete;
			}
			catch (Exception ex)
			{
				if ((tx != null))
				{
					try
					{
						tx.Rollback();
					}
					catch
					{
						//Swallow Rollback Exception
					}
				}

				throw ex;
			}

			return result;
		}

		#endregion

		#region Public methods

		public MessageDS GetStaffingActionByExternalIdAndChoiceID(string externalId, int configChoiceID)
		{
			MessageDS result = null;
			SqlDataAdapter daStaffingAction = null;
			string sql = string.Empty;

			//if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("GetStaffingActionByExternalIdAndChoiceID"); }

			try
			{
				daStaffingAction = AdapterStaffingAction(Connection);

				daStaffingAction.SelectCommand = new SqlCommand("QPS_StaffingAction_SelectByActionTypeConfigChoiceIDAndExternalID", Connection, null);
				daStaffingAction.SelectCommand.CommandType = CommandType.StoredProcedure;
				daStaffingAction.SelectCommand.Parameters.Add("@ActionTypeConfigChoiceID", System.Data.SqlDbType.Int, 32, "ActionTypeConfigChoiceID").Value = configChoiceID.ToString();
				daStaffingAction.SelectCommand.Parameters.Add("@ExternalID", System.Data.SqlDbType.VarChar, 255, "ExternalID").Value = externalId.ToString();

				RefreshDataset();
				Dataset.EnforceConstraints = false;
				daStaffingAction.Fill(Dataset.StaffingAction);
				Dataset.EnforceConstraints = true;

				result = Dataset;
			}
			catch (Exception ex)
			{
				try
				{   // Attempt to add as much info to the exeception as exists
					HelperFunctions.AddInfo(ex, string.Format("Get message (Staffing Action) by ConfigChoiceID: {0} AND ExternalID: {1}", configChoiceID, externalId), externalId);
				}
				catch
				{
					// Eat problems with adding execetion info.
				}
				throw ex;
			}
			//finally
			//{
			//	CloseConnection();
			//}

			return result;
		}

		public DataTable GetEmployeesByPhoneNumber(Int64 phoneNumber)
		{
			DataTable result = null;
			SqlCommand cmd;
			SqlDataReader dr = null;

			//if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(string.Format("GetEmployeesByPhoneNumber '{0}' ({1})", phoneNumber.ToString(), phoneNumber)); }

			try
			{
				cmd = new SqlCommand("QPS_Employee_SelectByContactMethodValue", Connection);
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add("@ContactMethodValue", System.Data.SqlDbType.VarChar, 255, "ContactMethodValue").Value = phoneNumber.ToString();
				dr = cmd.ExecuteReader();
				result = new DataTable();
				result.Load(dr);
			}
			catch (Exception ex)
			{
				try
				{   // Attempt to add as much info to the exeception as exists
					HelperFunctions.AddInfo(ex, "Get AEs by phone# - ", 
						string.Format("QPS_Employee_SelectByContactMethodValue '{0}'",phoneNumber));
				}
				catch
				{
					// Eat problems with adding execetion info.
				}
				throw ex;
			}
			finally
			{
				if (dr != null)  dr.Close();
			}

			return result;
		}

		#endregion

		#region Misc methods		

		public void OpenConnection()
		{
			if (Connection.State != ConnectionState.Open)
			{
				if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Opening the connection"); }
				Connection.Open();
			}
		}

		public void CloseConnection()
		{
			//HelperFunctions.DbCloseConnection();

			if (_conn != null && _conn.State == ConnectionState.Open)
			{
				if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry("Closing the connection"); }
				_conn.Close();
			}
		}

		public void RefreshDataset()
		{
			_data = new MessageDS();
		}

		#endregion


	}
}