using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Dao;
using System.Globalization;
using System.Data.SqlClient;
using System.IO;

namespace Server
{
    public class GeneralDao
    {
        protected MySqlConnection connection { get; set; }
        public List<DataTable> tablesToSend { get; protected set; }
        public ServerDataStatus serverDataStatus { get; protected set; }
        public string query { get; protected set; }

        protected string[] SelectObjectsListFromRequest(string request)
        {
            string[] result = request.Split(',');
            for (int i = 0; i < result.Count(); i++)
            {
                result[i] = result[i].Trim();
            }
            return result;
        }

        protected DataTable ReadDataFromDataBase()
        {
            DataTable table = new DataTable();
            table.Locale = new CultureInfo("uk-UA");
            MySqlCommand sqlCom = new MySqlCommand(query, connection);
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            MySqlDataAdapter dataAdapter = new MySqlDataAdapter(sqlCom);
            dataAdapter.Fill(table);

            /*ShowResults(table);*/ // For console testing

            return table;
        }

        protected void InsertDataInDataBase()
        {
            try
            {
                MySqlCommand sqlCom = new MySqlCommand(query, connection);
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                sqlCom.ExecuteNonQuery();
                serverDataStatus = ServerDataStatus.INSERT_DATA_SUCCESSFUL;
            }
            catch (Exception e)
            {
                serverDataStatus = ServerDataStatus.INSERT_DATA_FAILED;
            }
        }

        protected void ShowResults(DataTable table)
        {
            Console.WriteLine("SELECT WAS EXECUTE");
            Console.WriteLine("Result: ");

            if (table.Rows.Count == 0)
            {
                Console.WriteLine("Empty");
            }
            else
            {
                for (int i = 0; i < table.Columns.Count ; i++)
                {

                    Console.Write(table.Columns[i].ColumnName + "|");

                }
                Console.WriteLine();
                for (int j = 0; j < table.Rows.Count; j++)
                {
                    DataRow row = table.Rows[j];

                    for (int n = 0; n < table.Columns.Count; n++)
                    {
                        DataColumn column = table.Columns[n];

                        Console.Write(row[column] + "|");
                    }
                    Console.WriteLine();
                }
            }
        }

        protected void AddTableToResult()
        {
            DataTable table = ReadDataFromDataBase();
            if (table.Rows.Count == 0)
            {
                serverDataStatus = ServerDataStatus.DB_ERROR_IN_DATA;
                Console.WriteLine("Empty result");
            }
            else
            {
                tablesToSend.Add(table);
            }
        }

        protected string[] ReplaceGroupNamesById(string[] groupsId)
        {
            List<string> groupsIdValue = new List<string>();
            foreach (var k in groupsId)
            {
                groupsIdValue.Add(SelectGroupIdByGroupName(k));
            }
            return groupsIdValue.ToArray();
        }

        protected string ReplaceGroupNamesById(string groupName)
        {
            return SelectGroupIdByGroupName(groupName);
        }

        private string SelectGroupIdByGroupName(string groupName)
        {
            query = "SELECT ID FROM groups" +
                " WHERE NAME = '" + groupName + "';";
            int result = 0;
            DataTable table = ReadDataFromDataBase();
            result = (Int32)table.Rows[0]["ID"];
            return result.ToString();
        }

        protected DataTable SelectStudentsInGroup(string groupsId)
        {
            query = "SELECT USER_ID FROM students" +
                " WHERE GROUP_ID = " + groupsId + ";";
            return ReadDataFromDataBase();
        }

        protected DataTable GetSubjectInfoByGroup(string groupId)
        {
            query = "SELECT SUBJECT_ID, LABWORKS_NUMBER FROM subjectsingroups" +
                " WHERE GROUP_ID = " + groupId + ";";

            return ReadDataFromDataBase();
        }

        protected DataTable JoinDataTable(DataTable t1, DataTable t2, string joinName)
        {
            for (int i = 0; i < t1.Rows.Count; i++)
            {
                for (int j = 0; j < t2.Rows.Count; j++)
                {
                    if (t1.Rows[i]["USER_ID"].ToString() == t2.Rows[j]["USER_ID"].ToString())
                    {
                        t1.Rows[i][joinName] = t2.Rows[j][joinName];
                    }
                }
            }

            return t1;
        }

        public void CloseConnection()
        {
            connection.Close();
        }

        public void SetConnection(MySqlConnection connection)
        {
            this.connection = connection;
        }


    }
}
