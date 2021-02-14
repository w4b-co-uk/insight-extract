namespace insight_extract {
    public static class Constants {
        public const int FAILED_TO_LOAD_INPUT_FILE = -1,
                         SUCCESS = 1;

        public const int DESCRIPTION_FIELD_LENGTH = 30,
                         DESCRIPTION_START_INDEX = 16,
                         LEASE_TERM_FIELD_LENGTH = 16,
                         LEASE_TERM_START_INDEX = 46,
                         LENGTH_WHEN_LAST_TWO = 27,
                         LENGTH_WHEN_LAST_THREE = 57,
                         LENGTH_WHEN_ALL_FOUR = 73,
                         REG_DATE_REF_FIELD_LENGTH = 16,
                         REG_DATE_REF_START_INDEX = 0,
                         TITLE_FIELD_LENGTH = 11,
                         TITLE_FIELD_START_INDEX = 62;

        public const string NO_VALID_INPUT_FILE = "Please pass the path to a valid input file as the first argument.";

        public const string LEASE_TERM_REGEX = " from $| including $| expiring on $",
                            REG_DATE_REF_REGEX = " in $| supplementary $";
    }
}