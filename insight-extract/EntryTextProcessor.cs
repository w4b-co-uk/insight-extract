using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace insight_extract {
    class EntryTextProcessor {
        EdgeCaseRegExEngine EdgeCases { get; }

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

        public EntryTextProcessor(EdgeCaseRegExEngine edgeCases) => EdgeCases = edgeCases;

        private (string newValue, bool lineAssigned) AssignLineTo(string field, bool condition) {
            if (condition) return (field + LineText + ' ', true);
            return (field, false);
        }

        private bool AssignUnclosedBrackets() {
            if (EdgeCases.CloseParenthesesMissing.Match(LineText).Success) {
                if (EdgeCases.OpenParenthesesMissing.Match(RegDateRef).Success) {
                    RegDateRef += LineText;
                    return true;
                }
                if (EdgeCases.OpenParenthesesMissing.Match(Description).Success) {
                    Description += LineText;
                    return true;
                }
                if (EdgeCases.OpenParenthesesMissing.Match(LeaseTerm).Success) {
                    LeaseTerm += LineText;
                    return true;
                }
            }
            return false;
        }

        private bool CheckLeaseTermFromAndTo() {
            if (LineText.ToLower().Contains("to ") && !LineText.ToLower().Contains("from ") && LeaseTerm.ToLower().Contains("from ") && !LeaseTerm.ToLower().Contains("to ")) {
                LeaseTerm += LineText;
                return true;
            }
            return false;
        }

        private bool ExtractFixedWidthFields() {
            if (new[] { Constants.LENGTH_WHEN_ALL_FOUR, Constants.LENGTH_WHEN_LAST_THREE, Constants.LENGTH_WHEN_LAST_TWO }.Contains(LineText.Length)) {
                var offset = Constants.LENGTH_WHEN_ALL_FOUR - LineText.Length;
                (RegDateRef, RegDateRefDone) = ProcessField(Constants.REG_DATE_REF_START_INDEX - offset, Constants.REG_DATE_REF_FIELD_LENGTH, RegDateRef);
                (Description, DescriptionDone) = ProcessField(Constants.DESCRIPTION_START_INDEX - offset, Constants.DESCRIPTION_FIELD_LENGTH, Description);
                (LeaseTerm, LeaseTermDone) = ProcessField(Constants.LEASE_TERM_START_INDEX - offset, Constants.LEASE_TERM_FIELD_LENGTH, LeaseTerm);
                (Title, TitleDone) = ProcessField(Constants.TITLE_FIELD_START_INDEX - offset, Constants.TITLE_FIELD_LENGTH, Title);
                LeaseTermTrails = LineText.Length == Constants.LENGTH_WHEN_LAST_TWO;
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
            if (CheckLeaseTermFromAndTo()) return true;
            if (AssignUnclosedBrackets()) return true;

            if (LineText.Length > Constants.LENGTH_WHEN_LAST_TWO) return false;

            return false; //just for now
        }

        private bool ProcessShortLines() {
            var lineAssigned = false;

            (RegDateRef, lineAssigned) = AssignLineTo(RegDateRef, (TitleDone && LeaseTermDone && DescriptionDone) || EdgeCases.RegDateRef.Match(RegDateRef).Success);
            if (lineAssigned) return true;

            (Description, lineAssigned) = AssignLineTo(Description, TitleDone && LeaseTermDone && RegDateRefDone);
            if (lineAssigned) return true;

            (LeaseTerm, lineAssigned) = AssignLineTo(LeaseTerm, (TitleDone && DescriptionDone && RegDateRefDone) || LeaseTermTrails || EdgeCases.LeaseTerm.Match(LeaseTerm).Success);
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
            CleanUp();
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

}
