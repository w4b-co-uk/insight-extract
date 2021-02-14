using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace insight_extract {
    class Program {
        static int Main(string[] args) {
            if (args.Length == 0 || !File.Exists(args[0])) {
                Console.WriteLine(Constants.NO_VALID_INPUT_FILE);
                return Constants.FAILED_TO_LOAD_INPUT_FILE;
            }
            var inputFile = args[0];
            var outputFile = args.Length > 1 ? args[1] : "processed-data.json";
            var errorsFile = args.Length > 1 ? args[1] : "errors.json";
            var edgeCases = new EdgeCaseRegExEngine(args.Length > 2 ? args[2] : "edge-cases.json");
            var entryTextProcessor = new EntryTextProcessor(edgeCases);

            var leaseSchedules = ProcessExtract(inputFile, entryTextProcessor);

            OutputResults(leaseSchedules, outputFile, errorsFile, entryTextProcessor);

            return Constants.SUCCESS;
        }

        static private List<LeaseSchedules> ProcessExtract(string inputFile, EntryTextProcessor entryTextProcessor) {
            using var stream = File.OpenRead(inputFile);
            using var document = JsonDocument.Parse(stream);
            return (from item in document.RootElement.EnumerateArray()
                        select new LeaseSchedules {
                            LeaseSchedule = (from entry in item.GetProperty("leaseschedule").GetProperty("scheduleEntry").EnumerateArray()
                                            where !entry.GetProperty("entryType").GetString().StartsWith("Cancel")
                                            select new ScheduleEntry(
                                                int.Parse(entry.GetProperty("entryNumber").GetString()),
                                                entryTextProcessor.GetRegDateRef(int.Parse(entry.GetProperty("entryNumber").GetString()),
                                                entry.GetProperty("entryText").EnumerateArray().AsEnumerable()),
                                                entryTextProcessor.Description, entryTextProcessor.LeaseTerm, entryTextProcessor.Title, entryTextProcessor.Notes
                                            )).ToList()}).ToList();
        }

        static private void OutputResults(List<LeaseSchedules> leaseSchedules, string outputFile, string errorsFile, EntryTextProcessor entryTextProcessor) {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(outputFile, JsonSerializer.Serialize(leaseSchedules, options));
            File.WriteAllText(errorsFile, JsonSerializer.Serialize(entryTextProcessor.Errors, options));
        }
    }
}
