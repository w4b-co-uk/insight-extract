using System.Collections.Generic;

namespace insight_extract {
    class ScheduleEntry {
        public int EntryNumber { get; }
        public string RegDateRef { get; }
        public string Description { get; }
        public string LeaseTerm { get; }
        public string Title { get; }
        public List<string> Notes { get; }

        public ScheduleEntry(int entryNumber, string regDateRef, string description, string leaseTerm, string title, List<string> notes) =>
            (EntryNumber, RegDateRef, Description, LeaseTerm, Title, Notes) = (entryNumber, regDateRef, description, leaseTerm, title, notes);
    }
}
