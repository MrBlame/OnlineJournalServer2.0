using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Dao;
using System.Globalization;

namespace Server
{
    class CommandRecognizer
    {
        public MySqlConnection connection { get; private set; }
        public List<DataTable> tablesToSend { get; private set; }
        public ServerStatus serverStatus { get; private set; }
        public ServerDataStatus serverDataStatus { get; private set; }
        public string fileToSend { get; private set; }


        public CommandRecognizer(MySqlConnection connection)
        {
            this.connection = connection;
        }

        public void GenerateDataFromRequest(string[] request)
        {
            tablesToSend = new List<DataTable>();
            switch (request[0])
            {
                case "Login":
                    {
                        Login(request);
                        break;
                    }
                case "ShowSchedule":
                    {
                        ShowSchedule(request);
                        break;
                    }

                case "ShowScheduleForGroup":
                    {
                        ShowScheduleForGroup(request);
                        break;
                    }
                case "ShowScheduleForSubject":
                    {
                        ShowScheduleForSubject(request);
                        break;
                    }
                case "ShowSubjectDetail":
                    {
                        SubjectDetails(request);
                        break;
                    }
                case "InsertStudentPerformance":
                    {
                        InsertStudentPerformance(request);
                        break;
                    }
                case "GetGroupPerformance":
                    {
                        GetGroupPerformance(request);
                        break;
                    }
                case "GetStudentPerformance":
                    {
                        GetStudentPerformance(request);
                        break;
                    }
                case "SendFile":
                    {
                        SendFile(request);
                        break;
                    }
                case "ReceiveFile":
                    {
                        ReceiveFile(request);
                        break;
                    }
                default:
                    {
                        serverStatus = ServerStatus.WRONG_COMMAND;
                        break;
                    }
            }
        }

        private void Login(string[] request)
        {
            serverStatus = ServerStatus.SEND_DB_INFO;
            UserDao userDao = new UserDao();
            userDao.SetConnection(connection);
            userDao.Login(request);
            tablesToSend.AddRange(userDao.tablesToSend);
            serverDataStatus = userDao.serverDataStatus;
        }

        private void ShowSchedule(string[] request)
        {
            serverStatus = ServerStatus.SEND_DB_INFO;
            ScheduleDao scheduleDao = new ScheduleDao();
            scheduleDao.SetConnection(connection);
            scheduleDao.ShowForPeriod(request);
            tablesToSend.AddRange(scheduleDao.tablesToSend);
            serverDataStatus = scheduleDao.serverDataStatus;
        }

        private void ShowScheduleForGroup(string[] request)
        {
            serverStatus = ServerStatus.SEND_DB_INFO;
            ScheduleDao scheduleDao = new ScheduleDao();
            scheduleDao.SetConnection(connection);
            scheduleDao.ShowForGroup(request);
            tablesToSend.AddRange(scheduleDao.tablesToSend);
            serverDataStatus = scheduleDao.serverDataStatus;
        }

        private void ShowScheduleForSubject(string[] request)
        {
            serverStatus = ServerStatus.SEND_DB_INFO;
            ScheduleDao scheduleDao = new ScheduleDao();
            scheduleDao.SetConnection(connection);
            scheduleDao.ShowForSubject(request);
            tablesToSend.AddRange(scheduleDao.tablesToSend);
            serverDataStatus = scheduleDao.serverDataStatus;
        }

        private void SubjectDetails(string[] request)
        {
            serverStatus = ServerStatus.SEND_DB_INFO;
            SubjectDao subjectDao = new SubjectDao();
            subjectDao.SetConnection(connection);
            subjectDao.ShowForSubject(request);
            tablesToSend.AddRange(subjectDao.tablesToSend);
            serverDataStatus = subjectDao.serverDataStatus;
        }

        private void InsertStudentPerformance(string[] request)
        {
            serverStatus = ServerStatus.INSERT_INFO;
            StudentPerformanceDao spDao = new StudentPerformanceDao();
            spDao.SetConnection(connection);
            spDao.InsertPerformance(request);
            serverDataStatus = spDao.serverDataStatus;
        }

        private void GetGroupPerformance(string[] request)
        {
            serverStatus = ServerStatus.SEND_DB_INFO;
            StudentPerformanceDao spDao = new StudentPerformanceDao();
            spDao.SetConnection(connection);
            spDao.GetGroupPerformance(request);
            tablesToSend.AddRange(spDao.tablesToSend);
            serverDataStatus = spDao.serverDataStatus;
        }

        private void GetStudentPerformance(string[] request)
        {
            serverStatus = ServerStatus.SEND_DB_INFO;
            StudentPerformanceDao spDao = new StudentPerformanceDao();
            spDao.SetConnection(connection);
            spDao.GetStudentPerformance(request);
            tablesToSend.AddRange(spDao.tablesToSend);
            serverDataStatus = spDao.serverDataStatus;
        }

        private void SendFile(string[] request)
        {
            serverStatus = ServerStatus.SEND_FILE;
            FileDao fileDao = new FileDao();
            fileDao.SetConnection(connection);
            fileDao.ReadFileLinq(request);
            fileToSend = fileDao.fileLinq;
            serverDataStatus = fileDao.serverDataStatus;
        }

        private void ReceiveFile(string[] request)
        {
            serverStatus = ServerStatus.SAVE_FILE;
            FileDao fileDao = new FileDao();
            fileDao.SetConnection(connection);
            fileDao.InsertFileLinq(request);
            serverDataStatus = fileDao.serverDataStatus;
        }

    }
}
