using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace Server.Dao
{
    class FileDao : GeneralDao
    {
        public string fileLinq { get; private set; }
        private string standartPath;
        private string fileType;
        private int subjectId;
        private int userId;
        private string date;
        private string fileName;

        public FileDao()
        {
            tablesToSend = new List<DataTable>();
            standartPath = @"C:\Users\Infamous\Desktop\Projects\OnlineJournal\Server\ReceivedFiles";
            fileLinq = "";
        }

        public void InsertFileLinq(string[] request)
        {
            SetData(request);

            try
            {
                switch (fileType)
                {
                    case "Task":
                        {
                            InsertTaskLinq();
                            break;
                        }
                    case "Work":
                        {
                            InsertWorkLinq();
                            break;
                        }
                }

                serverDataStatus = ServerDataStatus.INSERT_DATA_SUCCESSFUL;
            }
            catch (Exception)
            {
                serverDataStatus = ServerDataStatus.INSERT_DATA_FAILED;
            }
        }

        private void SetData(string[] request)
        {
            fileType = request[1];
            subjectId = Int32.Parse(request[2]);
            userId = Int32.Parse(request[3]);
            date = request[4];
            fileName = request[5];
        }

        private void InsertTaskLinq()
        {
            query = "UPDATE schedule" +
                " SET FILES_FOR_LESSON = '" + standartPath + "\\" + fileName + "'" +
                " WHERE SUBJECT_ID = " + subjectId +
                " AND TEACHER_ID = " + userId +
                " AND LESSON_DATE = '" + date + "';";

            InsertDataInDataBase();
            Console.WriteLine("Insert file" + standartPath + "\\" + fileName + "'");
        }

        private void InsertWorkLinq()
        {
            query = "UPDATE studentacademicperformance" +
               " SET FILE_OF_LABWORK = '" + standartPath + "\\" + fileName + "'" +
                " WHERE SUBJECT_ID = " + subjectId +
                " AND USER_ID= " + userId +
                " AND LESSON_DATE = '" + date + "';"; ;

            InsertDataInDataBase();
        }

        public void ReadFileLinq(string[] request)
        {
            SetData(request);

            try
            {

                switch (fileType)
                {
                    case "Task":
                        {
                            ReadTaskLinq();
                            break;
                        }
                    case "Work":
                        {
                            ReadWorkLinq();
                            break;
                        }
                }
                serverDataStatus = ServerDataStatus.INSERT_DATA_SUCCESSFUL;
            }
            catch (Exception)
            {
                serverDataStatus = ServerDataStatus.INSERT_DATA_FAILED;
            }
        }

        private void ReadTaskLinq()
        {
            query = "SELECT FILE_FOR_LESSON FROM shcedule" +
                " WHERE SUBJECT_ID = " + subjectId +
                " AND TEACHER_ID = " + userId +
                " AND LESSON_DATE = '" + date + "';";

            fileLinq = ReadDataFromDataBase().Rows[0][0].ToString();
        }

        private void ReadWorkLinq()
        {
            query = "SELECT FILE_OF_LABWORK " +
                "FROM studentacademicperformance" +
                " WHERE SUBJECT_ID = " + subjectId +
                " AND USER_ID= " + userId +
                " AND LESSON_DATE = '" + date + "';";

            fileLinq = ReadDataFromDataBase().Rows[0][0].ToString();
        }

    }
}
