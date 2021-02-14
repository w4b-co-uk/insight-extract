using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace insight_extract {
    class MatchPair {
        public string AlreadyAssigned { get; }
        public string LastLine { get; }
    }

    class EdgeCases {
        public string LeaseTerm { get; }
        public string RegDateRef { get; }

        public EdgeCases(string leaseTerm, string regDateRef) => (LeaseTerm, RegDateRef) = (leaseTerm, regDateRef);
    }

    class EdgeCaseRegExEngine {
        public Regex LeaseTerm { get; private set; }
        public Regex CloseParenthesesMissing { get; } = new Regex(@"^[^(\r\n]*\).*?$");
        public Regex OpenParenthesesMissing { get; } = new Regex(@"^.*?\((?!.*?\))[^)]*$");
        public Regex RegDateRef { get; private set; }

        private void GenerateEdgeCaseFile(string edgeCaseFile) {
            var edgeCases = new EdgeCases(Constants.LEASE_TERM_REGEX, Constants.REG_DATE_REF_REGEX);
            var jsonString = JsonSerializer.Serialize(edgeCases, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(edgeCaseFile, jsonString);
        }

        public EdgeCaseRegExEngine(string edgeCaseFile) {
            if (!File.Exists(edgeCaseFile)) GenerateEdgeCaseFile(edgeCaseFile);
            using var stream = File.OpenRead(edgeCaseFile);
            using var document = JsonDocument.Parse(stream);
            LeaseTerm = new Regex(@document.RootElement.GetProperty("LeaseTerm").GetString());
            RegDateRef = new Regex(@document.RootElement.GetProperty("RegDateRef").GetString());
        }
    }
}
