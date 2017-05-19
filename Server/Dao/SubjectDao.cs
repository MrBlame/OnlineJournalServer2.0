using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Server.Dao
{
    class SubjectDao : GeneralDao
    {
        private string teacherId;
        private string subjectId;
        private string date;
        private string[] groupsId;

        public SubjectDao()
        {
            tablesToSend = new List<DataTable>();
        }

        public void ShowForSubject(string[] request)
        {
            //Standart for request: "ShowSubjectDetail;Teacher_Id;Subject_Id;Date;Groups"
            //Exemple: ShowSubjectDetail;5;5; 2017-03-16 13:20:00;КМ-135, СП-136
            //ShowSubjectDetail;5;5; 2017-03-23 13:20:00;КМ-135, СП-136
            // Select subject info + all groups on this lesson
            SetData(request);
            SelectSubjectInfo();
            GenerateQueryForGroupSelecting();

            if (serverDataStatus == ServerDataStatus.DB_ERROR_IN_DATA)
            {
                FillDataForGroups(groupsId, subjectId, date);
                GenerateQueryForGroupSelecting();
            }
        }

        private void SetData(string[] request)
        {
            groupsId = SelectObjectsListFromRequest(request[4]);
            teacherId = request[1];
            subjectId = request[2];
            date = request[3];
            groupsId = ReplaceGroupNamesById(groupsId);
        }

        private void SelectSubjectInfo()
        {
            query = "SELECT a.NAME,b.SURNAME,b.FIRST_NAME,b.MIDDLE_NAME,b.POSITION,c.CLASSROOM," +
                "CONVERT(c.LESSON_DATE USING utf8),c.SUBJECT_TYPE,c.THEME_OF_LESSON,c.CONDUCTED" +
                " FROM subjects a, teachers b, schedule c" +
                " WHERE c.TEACHER_ID = " + teacherId +
                " AND c.SUBJECT_ID = " + subjectId +
                " AND c.LESSON_DATE = '" + date + "'" +
                " AND c.TEACHER_ID = b.USER_ID" +
                " AND c.SUBJECT_ID = a.ID";
            AddTableToResult();
        }

        private void GenerateQueryForGroupSelecting()
        {
            for (int i = 0; i < groupsId.Count(); i++)
            {
                List<string> requestForGroup = new List<string>();
                requestForGroup.Add("ShowForGroup");
                requestForGroup.Add(groupsId[i]);
                requestForGroup.Add(subjectId);
                requestForGroup.Add(date);
                ShowForGroup(requestForGroup.ToArray());
            }
        }

        private void ShowForGroup(string[] request)
        {
            query = "SELECT a.ID_IN_GROUP,a.SURNAME,a.NAME," +
                "b.PRESENCE, b.MARK, c.NAME, b.MARK_DESCRIPTION, b.FILE_OF_LABWORK" +
                " FROM students a, studentacademicperformance b, marktypes c" +
                " WHERE a.GROUP_ID = " + request[1] +
                " AND b.SUBJECT_ID = " + request[2] +
                " AND b.LESSON_DATE = '" + request[3] + "'" +
                " AND a.USER_ID = b.USER_ID" +
                " AND b.MARK_TYPE = c.ID ORDER BY ID_IN_GROUP ASC;";
            AddTableToResult();
        }

        private void FillDataForGroups(string[] groupsId, string subjectId, string date)
        {
            for (int i = 0; i < groupsId.Count(); i++)
            {
                List<string> request = new List<string>();
                request.Add(groupsId[i]);
                request.Add(subjectId);
                request.Add(date);
                InsertStudentPerformanceData(request.ToArray());
            }
        }

        private void InsertStudentPerformanceData(string[] request)
        {
            DataTable studentsTable = SelectStudentsInGroup(request[0]);

            query = "INSERT INTO studentacademicperformance VALUES";

            for (int i = 0; i < studentsTable.Rows.Count; i++)
            {
                query += " (" + studentsTable.Rows[i]["USER_ID"] + ","
                    + request[1] + ", '" + request[2] + "'," +
                    " 0, -1, 100,'empty','empty_link')";
                if (i != studentsTable.Rows.Count - 1)
                {
                    query += ",";
                }
            }

            InsertDataInDataBase();
        }

    }

}
