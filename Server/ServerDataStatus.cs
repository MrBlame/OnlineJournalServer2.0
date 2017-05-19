namespace Server
{
    public enum ServerDataStatus
    {
        EMPTY_RESULT,
        DB_ERROR_IN_DATA,
        DB_SOME_DATA_NOT_EXIST,
        DB_CORRECT_DATA,
        LOGIN,
        LOGIN_FAILED,
        LOGIN_SUCCESSFULL,
        INSERT_DATA,
        INSERT_DATA_FAILED,
        INSERT_DATA_SUCCESSFUL,
        SHOW_SCHEDULE,
        SHOW_SCHEDULE_FOR_GROUPS,
        SHOW_SCHEDULE_FOR_SUBJECTS,
        SHOW_SUBJECT_DETAIL,
        SHOW_GROUP_DETAIL,
        LOAD_FILE_FAILED,
        LOAD_FILE_SUCCESSFUL,
    }
}
