using System;
using System.Collections.Generic;
using System.Data;


namespace Server.Dao
{
    class UserDao : GeneralDao
    {
        public UserDao()
        {
            tablesToSend = new List<DataTable>();
        }

        public void Login(string[] request)
        {
            AddUserTypeAndId(request);
            if (serverDataStatus != ServerDataStatus.DB_ERROR_IN_DATA)
            {
                SelectForUserType(tablesToSend[0]);
                serverDataStatus = ServerDataStatus.LOGIN_SUCCESSFULL;
            }
            else serverDataStatus = ServerDataStatus.LOGIN_FAILED;
        }

        private void AddUserTypeAndId(string[] request)
        {
            query = "SELECT ID,USER_TYPE FROM users" +
               " WHERE LOGIN = '" + request[1] + "'" +
               " AND PASSWORD = '" + request[2] + "';";
            AddTableToResult();
        }

        private void SelectForUserType(DataTable user)
        {
            string userType = user.Rows[0]["USER_TYPE"].ToString();

            switch (userType)
            {
                case "Teacher": { SelectForTeacher(user); break; }
                case "Student": { SelectForStudent(user); break; }
                case "Admin": { break; }
            }
        }

        private void SelectForTeacher(DataTable user)
        {
            int id = Int32.Parse(user.Rows[0]["ID"].ToString());
            query = "SELECT * FROM teachers" +
                " WHERE USER_ID = " + id + ";";
            AddTableToResult();
        }

        private void SelectForStudent(DataTable user)
        {
            int id = Int32.Parse(user.Rows[0]["ID"].ToString());
            query = "SELECT * FROM students" +
                " WHERE USER_ID = " + id + ";";
            AddTableToResult();
        }

    }
}
