<Query Kind="Program">
  <Namespace>System.Runtime.Serialization</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
</Query>

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

class EntryTextProcessor {
    const int DESCRIPTION_FIELD_LENGTH = 30,
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
    
    bool DescriptionDone { get; set; }
    bool LeaseTermDone { get; set; }
    bool LeaseTermTrails { get; set; }
    string LineText { get; set; }
    string RegDateRef { get; set; }
    bool RegDateRefDone { get; set; }
    bool TitleDone { get; set; }

    public List<ScheduleEntry> Errors { get; set; } = new List<ScheduleEntry>();

    public string Description { get; set; }
    public string LeaseTerm { get; set; }
    public List<string> Notes { get; set; }
    public string Title { get; set; }

    private (string newValue, bool lineAssigned) AssignLineTo(string field, bool condition) {
        if (condition) return (field + LineText + ' ', true);
        return (field, false);
    }

    private bool ExtractFixedWidthFields() {
        if (new[] { LENGTH_WHEN_ALL_FOUR, LENGTH_WHEN_LAST_THREE, LENGTH_WHEN_LAST_TWO }.Contains(LineText.Length)) {
            var offset = LENGTH_WHEN_ALL_FOUR - LineText.Length;
            (RegDateRef, RegDateRefDone) = ProcessField(REG_DATE_REF_START_INDEX - offset, REG_DATE_REF_FIELD_LENGTH, RegDateRef);
            (Description, DescriptionDone) = ProcessField(DESCRIPTION_START_INDEX - offset, DESCRIPTION_FIELD_LENGTH, Description);
            (LeaseTerm, LeaseTermDone) = ProcessField(LEASE_TERM_START_INDEX - offset, LEASE_TERM_FIELD_LENGTH, LeaseTerm);
            (Title, TitleDone) = ProcessField(TITLE_FIELD_START_INDEX - offset, TITLE_FIELD_LENGTH, Title);
            LeaseTermTrails = LineText.Length == LENGTH_WHEN_LAST_TWO;
            return true;
        }
        return false;
    }

    private bool ExtractNotes() {
        if (Notes.Count > 0 || LineText.ToUpper().StartsWith("NOTE")) {
            if (LineText.ToUpper().StartsWith("NOTE")) Notes.Add(LineText);
            else Notes[Notes.Count - 1] += '\n' + LineText;
            return true;
        }
        return false;
    }
    
    private void CleanUp() {
        Description = Description.Trim();
        LeaseTerm = LeaseTerm.Trim();
        RegDateRef = RegDateRef.Trim();
        Title = Title.Trim();
    }

    private (string newValue, bool fieldComplete) ProcessField(int index, int length, string fieldValue) {
        if (index < 0) return (fieldValue, true);
        var newValue = LineText.Substring(index, length).Trim();
        return (fieldValue + newValue + ' ', newValue.Length == 0);
    }

    private bool ProcessLine(string text) {    
        if (text is null) return false; //include this in notes about solution
        LineText = text;
        if (ExtractNotes()) return true;
        if (ExtractFixedWidthFields()) return true;
        if (ProcessShortLines()) return true;

        if (LineText.Length > LENGTH_WHEN_LAST_TWO) return false;
        //work out what to do about the rest - the wow factor - first record is a good example!
        // (and add more defences)
        return false; //just for now
    }
    
    private bool ProcessShortLines() {
        var lineAssigned = false;
        
        (RegDateRef, lineAssigned) = AssignLineTo(RegDateRef, (TitleDone && LeaseTermDone && DescriptionDone) ||
            RegDateRef.EndsWith(" in ") || RegDateRef.EndsWith(" supplementary "));
        if (lineAssigned) return true;
        
        (Description, lineAssigned) = AssignLineTo(Description, TitleDone && LeaseTermDone && RegDateRefDone);
        if (lineAssigned) return true;
        
        (LeaseTerm, lineAssigned) = AssignLineTo(LeaseTerm, (TitleDone && DescriptionDone && RegDateRefDone) || 
            LeaseTermTrails || LeaseTerm.EndsWith(" from ") || LeaseTerm.EndsWith(" including ") || LeaseTerm.EndsWith(" expiring on "));
        if (lineAssigned) return true;
        
        (Title, lineAssigned) = AssignLineTo(Title, LeaseTermDone && DescriptionDone && RegDateRefDone);
        if (lineAssigned) return true;

        return false;
    }

    private bool ProcessText(IEnumerable<JsonElement> entryText) {
        var success = true;
        foreach (var text in entryText) {
            var succeeded = ProcessLine(text.GetString());
            success = !succeeded ? false : success;
        }
        CleanUp(); //make this a sense check and throw any that are silly into errors
        return success;
    }

    private void Reset() {
        (Description, DescriptionDone, LeaseTerm, LeaseTermDone, LeaseTermTrails, Notes, RegDateRef, RegDateRefDone, Title, TitleDone)
                    = ("", false, "", false, false, new List<string>(), "", false, "", false);
    }
    
    public string GetRegDateRef(int entryNumber, IEnumerable<JsonElement> entryText) {
        Reset();
        if (!ProcessText(entryText)) {
            var error = new ScheduleEntry(entryNumber, RegDateRef, Description, LeaseTerm, Title, Notes);
            Errors.Add(error);
        }
        return RegDateRef;
    }
}

void Main() {
    
    var entryTextProcessor = new EntryTextProcessor();

    using var stream = File.OpenRead(@"C:\Users\colin\OneDrive\Documents\LINQPad Queries\data.json");
    using JsonDocument document = JsonDocument.Parse(stream);

    var query =
        from item in document.RootElement.EnumerateArray()
        select new 
        {
            LeaseSchedule = from entry in item.GetProperty("leaseschedule").GetProperty("scheduleEntry").EnumerateArray()
            where !entry.GetProperty("entryType").GetString().StartsWith("Cancel")
            select new ScheduleEntry(
                int.Parse(entry.GetProperty("entryNumber").GetString()), 
                entryTextProcessor.GetRegDateRef(int.Parse(entry.GetProperty("entryNumber").GetString()), 
                entry.GetProperty("entryText").EnumerateArray().AsEnumerable()),
                entryTextProcessor.Description, entryTextProcessor.LeaseTerm, entryTextProcessor.Title, entryTextProcessor.Notes
            )
        };

    //var options = new JsonSerializerOptions { WriteIndented = true };
    //JsonSerializer.Serialize(query.ToList(), options).Dump();
    //"----------".Dump();
    //"---errors:".Dump();
    //"----------".Dump();
    //JsonSerializer.Serialize(entryTextProcessor.Errors, options).Dump();

    query.ToListAsync().Dump();
    "----------".Dump();
    "---errors:".Dump();
    "----------".Dump();
    entryTextProcessor.Errors.Dump();
}