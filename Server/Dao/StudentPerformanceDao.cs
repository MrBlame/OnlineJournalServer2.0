using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Server.Dao
{
    class StudentPerformanceDao : GeneralDao
    {
        const int labWork = 1;
        const int firstRating = 4;
        const int secondRating = 9;
        const int RGR = 6;
        const int courseWork = 7;
        const int graduateWork = 8;

        private string subjectId;
        private string lessonDate;
        private string[] studentList;
        private string[] presenceValueList;
        private string[] markValueList;
        private string[] markTypesList;
        private string[] descriptionList;
        private string groupName;
        private string groupId;
        private string workTypeName;
        private string creditTypeName;
        private int studentId;

        public StudentPerformanceDao()
        {
            tablesToSend = new List<DataTable>();
        }

        public void InsertPerformance(string[] request)
        {
            // Standart template: "InsertStudentPresence;Subject_Id;Date;List<StudentId>;List<PresenceValue>;List<MarkValue>; List<Mark_Types>; List<Description>"
            // InsertStudentPerformance;5;2017-03-23 13:20:00;7,8;0,0;80,80;1,1;Lab 2, Lab 2

            SetDataForInsert(request);
            InsertData();

        }

        private void SetDataForInsert(string[] request)
        {
            subjectId = request[1];
            lessonDate = request[2];
            studentList = SelectObjectsListFromRequest(request[3]);
            presenceValueList = SelectObjectsListFromRequest(request[4]);
            markValueList = SelectObjectsListFromRequest(request[5]);
            markTypesList = SelectObjectsListFromRequest(request[6]);
            descriptionList = SelectObjectsListFromRequest(request[7]);
        }

        private void InsertData()
        {
            for (int i = 0; i < studentList.Count(); i++)
            {
                query = "UPDATE studentacademicperformance" +
                    " SET PRESENCE = " + presenceValueList[i] + "," +
                    "MARK = " + markValueList[i] + "," +
                    "MARK_TYPE = " + markTypesList[i] + "," +
                    "MARK_DESCRIPTION = '" + descriptionList[i] + "'" +
                    " WHERE USER_ID = " + studentList[i] +
                    " AND SUBJECT_ID = " + subjectId +
                    " AND LESSON_DATE = '" + lessonDate + "';";
                InsertDataInDataBase();
            }
        }

        public void GetGroupPerformance(string[] request)
        {
            // format: "GetGroupPerformance;GroupName"
            // Example: GetGroupPerformance;КМ-135

            SetDataForGroupPerformance(request);
            List<string> groupStudents = ConvertToListUserId(SelectStudentsInGroup(groupId));
            DataTable subjects = GetSubjectInfoByGroup(groupId);

            tablesToSend.Add(GetSubjectInfo(subjects));

            for (int i = 0; i < subjects.Rows.Count; i++)
            {
                tablesToSend.Add(CreateTableForSubject(Int32.Parse(subjects.Rows[i]["LABWORKS_NUMBER"].ToString()), subjects.Rows[i]["SUBJECT_ID"].ToString()));
            }

            serverDataStatus = ServerDataStatus.DB_CORRECT_DATA;
        }

        private DataTable GetSubjectInfo(DataTable subjects)
        {
            string subjectsId = "";

            for (int i = 0; i < subjects.Rows.Count; i++)
            {
                subjectsId = subjectsId + subjects.Rows[i]["SUBJECT_ID"].ToString();
                if (i != (subjects.Rows.Count - 1)) { subjectsId = subjectsId + ","; }
            }

            query = "SELECT NAME, SHORT_NAME FROM subjects" +
                " WHERE ID IN (" + subjectsId + ");";

            return ReadDataFromDataBase();
        }

        private void SetDataForGroupPerformance(string[] request)
        {
            groupName = request[1];
            groupId = ReplaceGroupNamesById(request[1]);
        }

        private List<string> ConvertToListUserId(DataTable dataTable)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                result.Add(dataTable.Rows[i]["USER_ID"].ToString());
            }
            return result;
        }

        private DataTable CreateTableForSubject(int labworkNumber, string subjectId)
        {
            DataTable result = SelectStudentInfoForGroup();

            for (int i = 0; i < labworkNumber; i++)
            {
                query = "SELECT USER_ID,MARK as Lab" + (i + 1) + "" +
                    " FROM studentacademicperformance" +
                    " WHERE SUBJECT_ID = " + subjectId +
                    " AND MARK_TYPE = " + labWork +
                    " AND MARK_DESCRIPTION = 'Lab " + (i + 1) + "';";
                result.Columns.Add("Lab" + (i + 1), typeof(int));

                result = JoinDataTable(result, ReadDataFromDataBase(), "Lab" + (i + 1));
            }

            result.Columns.Add("AverageMark", typeof(double));
            result = CalculateAverageMark(result,4);

            result.Columns.Add("FirstRating", typeof(int));
            result.Columns.Add("SecondRating", typeof(int));
            result = JoinDataTable(result, AddRating(subjectId, firstRating), "FirstRating");
            result = JoinDataTable(result, AddRating(subjectId, secondRating), "SecondRating");

            result.Columns.Add("ExaminationWorkType", typeof(string));
            result.Columns.Add("ExaminationWorkMark", typeof(int));

            var t1 = AddExaminationWork(subjectId, groupId);

            result = JoinDataTable(result, t1, "ExaminationWorkType");
            result = JoinDataTable(result, t1, "ExaminationWorkMark");
            result = FillWorkType(result);

            result.Columns.Add("CreditType", typeof(string));
            result.Columns.Add("CreditMark", typeof(int));
            result.Columns.Add("ECTSMark", typeof(string));
            var t2 = AddCredit(subjectId, groupId);

            result = JoinDataTable(result, t2, "CreditType");
            result = JoinDataTable(result, t2, "CreditMark");
            result = JoinDataTable(result, t2, "ECTSMark");
            result = FillCreditType(result);

            ShowResults(result);

            return result;
        }

        private DataTable SelectStudentInfoForGroup()
        {
            query = "SELECT USER_ID,ID_IN_GROUP,NAME,SURNAME FROM students" +
               " WHERE GROUP_ID = " + groupId + " ORDER BY ID_IN_GROUP ASC;";

            return ReadDataFromDataBase();
        }

        private DataTable AddRating(string subjectId, int markType)
        {
            string markTypeName = "";

            if (markType == 4)
            {
                markTypeName = "FirstRating";
            }
            else
            {
                markTypeName = "SecondRating";
            }

            query = " SELECT USER_ID,MARK as " + markTypeName +
                " FROM studentacademicperformance" +
                " WHERE SUBJECT_ID = " + subjectId +
                " AND MARK_TYPE = " + markType + ";";
            var result = ReadDataFromDataBase();

            return result;
        }

        private DataTable AddExaminationWork(string subjectId, string groupId)
        {

            var workTypeDB = SelectExaminationWorkType(subjectId, groupId);
            int workTypeId = Int32.Parse(workTypeDB.Rows[0][0].ToString());
            workTypeName = SelectWork(workTypeId);

            if (workTypeName != "WorkNotPinned")
            {
                query = " SELECT USER_ID," +
               "MARK_DESCRIPTION as ExaminationWorkType," +
               "MARK as ExaminationWorkMark" +
               " FROM studentacademicperformance" +
               " WHERE SUBJECT_ID = " + Int32.Parse(subjectId) +
               " AND MARK_TYPE = " + workTypeId + ";";
            }
            else
            {
                query = " SELECT USER_ID," +
               "MARK_DESCRIPTION as ExaminationWorkType," +
               "MARK as ExaminationWorkMark" +
               " FROM studentacademicperformance" +
               " WHERE SUBJECT_ID = " + Int32.Parse(subjectId) +
               " AND MARK_TYPE = " + RGR + ";";
            }

            return ReadDataFromDataBase();
        }

        private DataTable SelectExaminationWorkType(string subjectId, string groupId)
        {
            query = " SELECT EXAMINATION_WORK" +
                " FROM subjectsingroups" +
                " WHERE SUBJECT_ID = " + Int32.Parse(subjectId) +
                " AND GROUP_ID = " + Int32.Parse(groupId) + ";";

            return ReadDataFromDataBase();
        }

        private string SelectWork(int workType)
        {

            switch (workType)
            {
                case RGR:
                    {
                        return "RGR";
                    }
                case courseWork:
                    {
                        return "CorseWork";
                    }
                case graduateWork:
                    {
                        return "GraduateWork";
                    }

                default:
                    {
                        return "WorkNotPinned";
                    }
            }
        }

        private DataTable FillWorkType(DataTable result)
        {
            for (int i = 0; i < result.Rows.Count; i++)
            {
                result.Rows[i]["ExaminationWorkType"] = workTypeName;
            }

            return result;
        }

        private DataTable AddCredit(string subjectId, string groupId)
        {
            DataTable creditTypeDB = SelectCreditType(subjectId, groupId);
            int creditTypeId = Int32.Parse(creditTypeDB.Rows[0][0].ToString());
            creditTypeName = SelectCreditName(creditTypeId);
            if (creditTypeName != "Error")
            {
                query = " SELECT USER_ID,MARK_DESCRIPTION as CreditType," +
               "MARK as CreditMark" +
               " FROM studentacademicperformance" +
               " WHERE SUBJECT_ID = " + subjectId +
               " AND MARK_TYPE = " + creditTypeId + ";";
            }

            DataTable result = ReadDataFromDataBase();

            result.Columns.Add("ECTSMark", typeof(string));

            return CalculateETCSMark(result);
        }

        private DataTable SelectCreditType(string subjectId, string groupId)
        {
            query = " SELECT CREDIT_TYPE" +
              " FROM subjectsingroups" +
              " WHERE SUBJECT_ID = " + Int32.Parse(subjectId) +
              " AND GROUP_ID = " + Int32.Parse(groupId) + ";";

            return ReadDataFromDataBase();
        }

        private string SelectCreditName(int creditTypeId)
        {
            switch (creditTypeId)
            {
                case 2:
                    {
                        return "Credit";
                    }
                case 3:
                    {
                        return "Exam";
                    }
                default:
                    {
                        return "Error";
                    }
            }
        }

        private DataTable CalculateETCSMark(DataTable result)
        {
            for (int i = 0; i < result.Rows.Count; i++)
            {
                result.Rows[i]["ECTSMark"] = TransformMark(Int32.Parse(result.Rows[i]["CreditMark"].ToString()));
            }

            return result;
        }

        private string TransformMark(int digit)
        {

            if (digit >= 90) { return "A"; }
            else if (digit >= 80) { return "B"; }
            else if (digit >= 75) { return "C"; }
            else if (digit >= 70) { return "D"; }
            else if (digit >= 60) { return "E"; }
            else return "F";

        }

        private DataTable FillCreditType(DataTable result)
        {
            for (int i = 0; i < result.Rows.Count; i++)
            {
                result.Rows[i]["CreditType"] = creditTypeName;
            }

            return result;
        }

        private DataTable CalculateAverageMark(DataTable table, int counter)
        {
            for (int j = 0; j < table.Rows.Count; j++)
            {
                int sum = 0;
                int k = 0;
                for (int i = counter; i < (table.Columns.Count - 1); i++)
                {

                    if (table.Rows[j][i].ToString() != "")
                    {
                        sum = sum + Int32.Parse(table.Rows[j][i].ToString());
                        k++;
                    }
                    if (k != 0)
                    {
                        table.Rows[j][(table.Columns.Count - 1)] = sum / k;
                    }
                }
            }

            return table;
        }



        public void GetStudentPerformance(string[] request)
        {
            // format: "GetStudentPerformance;StudentId"
            // Example: GetStudentPerformance;6

            studentId = Int32.Parse(request[1]);

            GetGroupdByStudentId();

            tablesToSend.Add(GetStudentInfoForStudent());

            DataTable result = new DataTable();

            DataTable subjects = GetSubjectInfoByGroup(groupId);

            for (int i = 0; i < subjects.Rows.Count; i++)
            {
                tablesToSend.Add(GeneratetSubjectsPerformance(Int32.Parse(subjects.Rows[i]["LABWORKS_NUMBER"].ToString()), subjects.Rows[i]["SUBJECT_ID"].ToString()));
            }

        }

        private void GetGroupdByStudentId()
        {
            query = "SELECT GROUP_ID FROM students" +
                " WHERE USER_ID = " + studentId + ";";

            groupId = ReadDataFromDataBase().Rows[0][0].ToString();
        }

        private DataTable GetStudentInfoForStudent()
        {
            query = "SELECT a.NAME, a.SURNAME, b.NAME, b.DEPARTMENT" +
                " FROM students a, groups b" +
                " WHERE USER_ID = " + studentId +
                " AND a.GROUP_ID = b.ID;";

            return ReadDataFromDataBase();
        }

        private DataTable GeneratetSubjectsPerformance(int labworkNumber, string subjectId)
        {
            DataTable result = SelectSubjectInfoForStudent(subjectId);

            DataTable subjectInfo = GetSubjectInfoByGroup(groupId);

            for (int i = 0; i < labworkNumber; i++)
            {
                query = "SELECT USER_ID,MARK as Lab" + (i + 1) + "" +
                    " FROM studentacademicperformance" +
                    " WHERE SUBJECT_ID = " + subjectId +
                    " AND USER_ID = " + studentId +
                    " AND MARK_TYPE = " + labWork +
                    " AND MARK_DESCRIPTION = 'Lab " + (i + 1) + "';";
                result.Columns.Add("Lab" + (i + 1), typeof(int));

                result = JoinDataTable(result, ReadDataFromDataBase(), "Lab" + (i + 1));
            }

            result.Columns.Add("AverageMark", typeof(double));
            result = CalculateAverageMark(result,5);

            result.Columns.Add("FirstRating", typeof(int));
            result.Columns.Add("SecondRating", typeof(int));
            result = JoinDataTable(result, AddRating(subjectId, firstRating, studentId), "FirstRating");
            result = JoinDataTable(result, AddRating(subjectId, secondRating, studentId), "SecondRating");

            result.Columns.Add("ExaminationWorkType", typeof(string));
            result.Columns.Add("ExaminationWorkMark", typeof(int));

            var t1 = AddExaminationWork(subjectId, groupId, studentId);

            result = JoinDataTable(result, t1, "ExaminationWorkType");
            result = JoinDataTable(result, t1, "ExaminationWorkMark");
            result = FillWorkType(result);

            result.Columns.Add("CreditType", typeof(string));
            result.Columns.Add("CreditMark", typeof(int));
            result.Columns.Add("ECTSMark", typeof(string));
            var t2 = AddCredit(subjectId, groupId, studentId);

            result = JoinDataTable(result, t2, "CreditType");
            result = JoinDataTable(result, t2, "CreditMark");
            result = JoinDataTable(result, t2, "ECTSMark");
            result = FillCreditType(result);

            ShowResults(result);

            return result;
        }

        private DataTable SelectSubjectInfoForStudent(string subjectId)
        {
            query = "SELECT c.USER_ID, a.ID, a.NAME, a.SHORT_NAME, b.SEMESTER" +
                " FROM subjects a, subjectsingroups b, students c" +
                " WHERE b.GROUP_ID = " + Int32.Parse(groupId) +
                " AND a.ID = " + Int32.Parse(subjectId) +
                " AND c.USER_ID = " + studentId +
                " AND c.GROUP_ID = b.GROUP_ID" +
                " AND a.ID = b.SUBJECT_ID;";

            return ReadDataFromDataBase();
        }

        private DataTable AddRating(string subjectId, int markType, int studentId)
        {
            string markTypeName = "";

            if (markType == 4)
            {
                markTypeName = "FirstRating";
            }
            else
            {
                markTypeName = "SecondRating";
            }

            query = " SELECT USER_ID,MARK as " + markTypeName +
                " FROM studentacademicperformance" +
                " WHERE SUBJECT_ID = " + subjectId +
                " AND USER_ID = " + studentId +
                " AND MARK_TYPE = " + markType + ";";
            var result = ReadDataFromDataBase();

            return result;
        }

        private DataTable AddExaminationWork(string subjectId, string groupId, int studentId)
        {

            var workTypeDB = SelectExaminationWorkType(subjectId, groupId);
            int workTypeId = Int32.Parse(workTypeDB.Rows[0][0].ToString());
            workTypeName = SelectWork(workTypeId);

            if (workTypeName != "WorkNotPinned")
            {
                query = " SELECT USER_ID," +
               "MARK_DESCRIPTION as ExaminationWorkType," +
               "MARK as ExaminationWorkMark" +
               " FROM studentacademicperformance" +
               " WHERE SUBJECT_ID = " + Int32.Parse(subjectId) +
               " AND USER_ID = " + studentId +
               " AND MARK_TYPE = " + workTypeId + ";";
            }
            else
            {
                query = " SELECT USER_ID," +
               "MARK_DESCRIPTION as ExaminationWorkType," +
               "MARK as ExaminationWorkMark" +
               " FROM studentacademicperformance" +
               " WHERE SUBJECT_ID = " + Int32.Parse(subjectId) +
               " AND USER_ID = " + studentId +
               " AND MARK_TYPE = " + RGR + ";";
            }

            return ReadDataFromDataBase();
        }

        private DataTable AddCredit(string subjectId, string groupId, int studentId)
        {
            DataTable creditTypeDB = SelectCreditType(subjectId, groupId);
            int creditTypeId = Int32.Parse(creditTypeDB.Rows[0][0].ToString());
            creditTypeName = SelectCreditName(creditTypeId);
            if (creditTypeName != "Error")
            {
                query = " SELECT USER_ID,MARK_DESCRIPTION as CreditType," +
               "MARK as CreditMark" +
               " FROM studentacademicperformance" +
               " WHERE SUBJECT_ID = " + subjectId +
               " AND USER_ID = " + studentId +
               " AND MARK_TYPE = " + creditTypeId + ";";
            }

            DataTable result = ReadDataFromDataBase();

            result.Columns.Add("ECTSMark", typeof(string));

            return CalculateETCSMark(result);
        }

    }
}
