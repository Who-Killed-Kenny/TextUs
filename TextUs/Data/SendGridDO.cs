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
	public class SendGridDO
	{
		private SqlConnection _conn;
		private SendGridDS _data;

		#region Properties

		public SendGridDS Dataset
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

		public bool GetData(Guid SendGridGUID, bool withRefresh = true)
		{
			bool result = true;
			//SqlDataAdapter daSendGrid = null;
			//SqlDataAdapter daSendGridFK = null;
			//SendGridDS.SendGridFKRow actionFKRow = null;
			//string msg = string.Empty;

			//try
			//{
			//	daSendGridFK = AdapterSendGridFK(Connection);
			//	daSendGridFK.SelectCommand.Parameters["@SendGridFKGUID"].Value = SendGridFKGUID;
			//	daSendGrid = AdapterSendGrid(Connection);

			//	if (withRefresh) { RefreshDataset(); }
			//	Dataset.EnforceConstraints = false;
			//	daSendGridFK.Fill(Dataset.SendGridFK);
			//	switch (Dataset.SendGridFK.Rows.Count)
			//	{
			//		case 0:
			//			//claim number doesn't yet exist.
			//			result = false;
			//			break;
			//		case 1:
			//			actionFKRow = (SendGridDS.SendGridFKRow)Dataset.SendGridFK.Rows[0];
			//			daSendGrid.SelectCommand.Parameters["@SendGridGUID"].Value = actionFKRow.SendGridGUID;
			//			daSendGrid.Fill(Dataset.SendGrid);
			//			break;
			//		default:
			//			result = false;
			//			msg = string.Format("SendGridFKGUID ({0}) returned {1} {2} records.  Zero or one was expected",
			//				SendGridFKGUID.ToString(), Dataset.SendGridFK.Rows.Count, Dataset.SendGridFK.TableName);
			//			HelperFunctions.MessageAdd(HelperFunctions.MsgTypeEnum.Warning, msg);
			//			if (HelperFunctions.IsTraceOn) { HelperFunctions.WriteTraceEntry(msg); }
			//			break;
			//	}
			//	Dataset.EnforceConstraints = true;
			//}
			//catch (Exception ex)
			//{
			//	result = false;
			//	if (daSendGrid != null) { HelperFunctions.AddInfo(ex, "daSendGrid.sql", daSendGrid.SelectCommand.CommandText); }
			//	if (daSendGridFK != null) { HelperFunctions.AddInfo(ex, "daSendGridFK.sql", daSendGridFK.SelectCommand.CommandText); }

			//	throw ex;
			//}

			return result;
		}
		#endregion

		#region Set methods

		public bool SetData()
		{
			return SetData(Dataset);
		}

		private bool SetData(SendGridDS dataIn)
		{
			bool result = false;
			SqlTransaction tx = null;
			Guid emailGuid = Guid.Empty;

			try
			{
				emailGuid = ((SendGridDS.QPS_SendGridRow)Dataset.QPS_SendGrid.Rows[0]).SendGridGUID;
				tx = Connection.BeginTransaction();
				result = SetData(dataIn, Connection, tx);
				tx.Commit();
				tx.Dispose();
				tx = null;

				GetData(emailGuid);
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

		private bool SetData(SendGridDS dataIn, SqlConnection cn, SqlTransaction tx)
		{
			bool result = false;
			SendGridDS delsDS = null;
			SendGridDS modsDS = null;
			SqlDataAdapter daSendGrid = null;

			if (cn == null || tx == null)
			{
				throw new ArgumentException();
			}

			try
			{
				daSendGrid = AdapterSendGrid(cn, tx);

				delsDS = (SendGridDS)dataIn.GetChanges(DataRowState.Deleted);
				modsDS = (SendGridDS)dataIn.GetChanges(DataRowState.Modified | DataRowState.Added);

				dataIn.Clear();

				//Take care of the deletes first, mindful of the fk constraints.
				if ((delsDS != null))
				{
					delsDS.EnforceConstraints = false;
					daSendGrid.Update(delsDS, "QPS_SendGrid");
					dataIn.Merge(delsDS);
					delsDS.EnforceConstraints = true;
				}

				if ((modsDS != null))
				{
					daSendGrid.Update(modsDS, "QPS_SendGrid");
					modsDS.EnforceConstraints = false;
					dataIn.Merge(modsDS);
					modsDS.EnforceConstraints = true;
				}
				result = true;
			}
			catch (Exception ex)
			{
				result = false;
				if (daSendGrid != null) { HelperFunctions.AddInfo(ex, "daSendGrid.sql", daSendGrid.SelectCommand.CommandText); }

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

		private SqlDataAdapter AdapterSendGrid(SqlConnection conn, SqlTransaction tx = null)
		{
			SqlDataAdapter result = null;
			SqlCommand cmdCreate;
			SqlCommand cmdRetrieve;
			SqlCommand cmdUpdate;
			SqlCommand cmdDelete;

			try
			{
				cmdCreate = new SqlCommand("QPS_SendGrid_Insert", conn, tx);
				cmdCreate.CommandType = CommandType.StoredProcedure;
				cmdCreate.Parameters.Add("@SendGridGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "SendGridGUID");
				cmdCreate.Parameters.Add("@sg_Email", System.Data.SqlDbType.VarChar, 8000, "sg_Email");
				cmdCreate.Parameters.Add("@sg_Timestamp", System.Data.SqlDbType.VarChar, 8000, "sg_Timestamp");
				cmdCreate.Parameters.Add("@sg_Smtp_Id", System.Data.SqlDbType.VarChar, 8000, "sg_Smtp_Id");
				cmdCreate.Parameters.Add("@sg_Event", System.Data.SqlDbType.VarChar, 8000, "sg_Event");
				cmdCreate.Parameters.Add("@sg_Category", System.Data.SqlDbType.VarChar, 8000, "sg_Category");
				cmdCreate.Parameters.Add("@sg_Event_Id", System.Data.SqlDbType.VarChar, 8000, "sg_Event_Id");
				cmdCreate.Parameters.Add("@sg_Message_Id", System.Data.SqlDbType.VarChar, 8000, "sg_Message_Id");
				cmdCreate.Parameters.Add("@sg_Response", System.Data.SqlDbType.VarChar, 8000, "sg_Response");
				cmdCreate.Parameters.Add("@sg_Attempt", System.Data.SqlDbType.VarChar, 8000, "sg_Attempt");
				cmdCreate.Parameters.Add("@sg_Useragent", System.Data.SqlDbType.VarChar, 8000, "sg_Useragent");
				cmdCreate.Parameters.Add("@sg_IP", System.Data.SqlDbType.VarChar, 8000, "sg_IP");
				cmdCreate.Parameters.Add("@sg_URL", System.Data.SqlDbType.VarChar, 8000, "sg_URL");
				cmdCreate.Parameters.Add("@sg_Status", System.Data.SqlDbType.VarChar, 8000, "sg_Status");
				cmdCreate.Parameters.Add("@sg_Asm_Group_Id", System.Data.SqlDbType.VarChar, 8000, "sg_Asm_Group_Id");
				cmdCreate.Parameters.Add("@UserGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "UserGUID");
				cmdCreate.Parameters.Add("@DateEntered", System.Data.SqlDbType.SmallDateTime, 4, "DateEntered");

				cmdRetrieve = new SqlCommand("QPS_SendGrid_Select", conn, tx);
				cmdRetrieve.CommandType = CommandType.StoredProcedure;
				cmdRetrieve.Parameters.Add("@SendGridGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "SendGridGUID");

				cmdUpdate = new SqlCommand("QPS_SendGrid_Update", conn, tx);
				cmdUpdate.CommandType = CommandType.StoredProcedure;
				cmdUpdate.Parameters.Add("@SendGridID", System.Data.SqlDbType.Int, 32, "SendGridID");
				cmdUpdate.Parameters.Add("@sg_Email", System.Data.SqlDbType.VarChar, 8000, "sg_Email");
				cmdUpdate.Parameters.Add("@sg_Timestamp", System.Data.SqlDbType.VarChar, 8000, "sg_Timestamp");
				cmdUpdate.Parameters.Add("@sg_Smtp_Id", System.Data.SqlDbType.VarChar, 8000, "sg_Smtp_Id");
				cmdUpdate.Parameters.Add("@sg_Event", System.Data.SqlDbType.VarChar, 8000, "sg_Event");
				cmdUpdate.Parameters.Add("@sg_Category", System.Data.SqlDbType.VarChar, 8000, "sg_Category");
				cmdUpdate.Parameters.Add("@sg_Event_Id", System.Data.SqlDbType.VarChar, 8000, "sg_Event_Id");
				cmdUpdate.Parameters.Add("@sg_Message_Id", System.Data.SqlDbType.VarChar, 8000, "sg_Message_Id");
				cmdUpdate.Parameters.Add("@sg_Response", System.Data.SqlDbType.VarChar, 8000, "sg_Response");
				cmdUpdate.Parameters.Add("@sg_Attempt", System.Data.SqlDbType.VarChar, 8000, "sg_Attempt");
				cmdUpdate.Parameters.Add("@sg_Useragent", System.Data.SqlDbType.VarChar, 8000, "sg_Useragent");
				cmdUpdate.Parameters.Add("@sg_IP", System.Data.SqlDbType.VarChar, 8000, "sg_IP");
				cmdUpdate.Parameters.Add("@sg_URL", System.Data.SqlDbType.VarChar, 8000, "sg_URL");
				cmdUpdate.Parameters.Add("@sg_Status", System.Data.SqlDbType.VarChar, 8000, "sg_Status");
				cmdUpdate.Parameters.Add("@sg_Asm_Group_Id", System.Data.SqlDbType.VarChar, 8000, "sg_Asm_Group_Id");
				cmdUpdate.Parameters.Add("@UserGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "UserGUID");
				cmdUpdate.Parameters.Add("@DateEntered", System.Data.SqlDbType.SmallDateTime, 4, "DateEntered");

				cmdDelete = new SqlCommand("QPS_SendGrid_Delete", conn, tx);
				cmdDelete.CommandType = CommandType.StoredProcedure;
				cmdDelete.Parameters.Add("@SendGridGUID", System.Data.SqlDbType.UniqueIdentifier, 16, "SendGridGUID");

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
			_data = new SendGridDS();
		}

		#endregion


	}
}