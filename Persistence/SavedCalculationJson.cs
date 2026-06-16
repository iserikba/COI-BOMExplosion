using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ProductionCalculator.Core.Persistence
{
    internal static class SavedCalculationJson
    {
        // --- PUBLIC FILE API ---

        public static void WriteToFile(string path, SavedCalculationDocument document)
        {
            string contents = Serialize(document);
            File.WriteAllText(path, contents, Encoding.UTF8);
        }

        public static SavedCalculationDocument ReadFromFile(string path)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            return Deserialize(json);
        }

        // Cleans up a string to ensure it's a valid Windows/Linux file name
        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "calculation";

            var sb = new StringBuilder(name.Length);
            foreach (char c in name.Trim())
            {
                // Replace invalid filesystem characters with an underscore
                if ("<>:\"/\\|?*".Contains(c)) sb.Append('_');
                else sb.Append(c);
            }

            string sanitized = sb.ToString().Trim();
            return !string.IsNullOrEmpty(sanitized) ? sanitized : "calculation";
        }

        // Ensures we don't overwrite existing files by adding (2), (3), etc.
        public static string MakeUniqueFileName(string directory, string baseName)
        {
            string fileName = baseName + ".json";
            if (!File.Exists(Path.Combine(directory, fileName))) return fileName;

            for (int i = 2; i < 1000; i++)
            {
                fileName = $"{baseName} ({i}).json";
                if (!File.Exists(Path.Combine(directory, fileName))) return fileName;
            }

            return $"{baseName} {Guid.NewGuid():N}.json";
        }

        // --- SERIALIZATION (Saving) ---
        public static string Serialize(SavedCalculationDocument document)
        {
            var builder = new StringBuilder(256);
            builder.Append('{');

            appendProperty(builder, "Version", document.Version.ToString());
            builder.Append(',');
            appendStringProperty(builder, "Name", document.Name);
            builder.Append(',');
            appendStringProperty(builder, "IconProductId", document.IconProductId);

            builder.Append(",\"Rows\":[");
            if (document.Rows != null)
            {
                for (int i = 0; i < document.Rows.Count; i++)
                {
                    if (i > 0) builder.Append(',');
                    appendRow(builder, document.Rows[i]);
                }
            }
            builder.Append("]}");
            return builder.ToString();
        }

        // --- DESERIALIZATION (Loading) ---
        public static SavedCalculationDocument Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("Saved calculation file is empty.");

            int index = 0;
            skipWhitespace(json, ref index);
            expect(json, ref index, '{');

            var doc = new SavedCalculationDocument { Rows = new List<SavedTargetRowData>() };

            while (true)
            {
                skipWhitespace(json, ref index);
                if (tryConsume(json, ref index, '}')) return doc;

                string propertyName = readString(json, ref index);
                skipWhitespace(json, ref index);
                expect(json, ref index, ':');
                skipWhitespace(json, ref index);

                switch (propertyName)
                {
                    case "Version": doc.Version = readInt(json, ref index); break;
                    case "Name": doc.Name = readString(json, ref index); break;
                    case "IconProductId": doc.IconProductId = readString(json, ref index); break;
                    case "Rows": doc.Rows = readRows(json, ref index); break;
                    default: skipValue(json, ref index); break;
                }

                skipWhitespace(json, ref index);
                if (tryConsume(json, ref index, ',')) continue;

                expect(json, ref index, '}');
                return doc;
            }
        }

        // --- INTERNAL PARSING HELPERS ---

        private static void appendProperty(StringBuilder builder, string name, string value) => builder.Append('"').Append(name).Append("\":").Append(value);
        private static void appendStringProperty(StringBuilder builder, string name, string value) { builder.Append('"').Append(name).Append("\":"); appendJsonString(builder, value ?? string.Empty); }

        private static void appendRow(StringBuilder builder, SavedTargetRowData row)
        {
            builder.Append('{');
            appendStringProperty(builder, "Flow", row.Flow);
            builder.Append(',');
            appendStringProperty(builder, "ProductId", row.ProductId);
            builder.Append(',');
            appendStringProperty(builder, "RecipeId", row.RecipeId);
            builder.Append(',');
            appendProperty(builder, "Rate", row.Rate.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            appendProperty(builder, "Machines", row.Machines.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            appendProperty(builder, "IsFixed", row.IsFixed ? "true" : "false");
            builder.Append('}');
        }

        private static void appendJsonString(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(c); break;
                }
            }
            builder.Append('"');
        }

        private static void skipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;
        }

        private static void expect(string json, ref int index, char expected)
        {
            if (index >= json.Length || json[index] != expected)
                throw new InvalidDataException($"Expected '{expected}' at position {index}");
            index++;
        }

        private static bool tryConsume(string json, ref int index, char expected)
        {
            if (index < json.Length && json[index] == expected) { index++; return true; }
            return false;
        }

        private static string readString(string json, ref int index)
        {
            skipWhitespace(json, ref index);
            expect(json, ref index, '"');
            var sb = new StringBuilder();
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"') return sb.ToString();
                sb.Append(c);
            }
            throw new InvalidDataException("Unterminated string.");
        }

        private static int readInt(string json, ref int index) => int.Parse(readNumberToken(json, ref index), CultureInfo.InvariantCulture);
        private static float readFloat(string json, ref int index) => float.Parse(readNumberToken(json, ref index), CultureInfo.InvariantCulture);

        private static string readNumberToken(string json, ref int index)
        {
            skipWhitespace(json, ref index);
            int start = index;
            if (index < json.Length && (json[index] == '-' || json[index] == '+')) index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E')) index++;
            return json.Substring(start, index - start);
        }

        private static List<SavedTargetRowData> readRows(string json, ref int index)
        {
            var list = new List<SavedTargetRowData>();
            skipWhitespace(json, ref index);
            expect(json, ref index, '[');
            while (!tryConsume(json, ref index, ']'))
            {
                list.Add(readRow(json, ref index));
                skipWhitespace(json, ref index);
                tryConsume(json, ref index, ',');
            }
            return list;
        }

        private static SavedTargetRowData readRow(string json, ref int index)
        {
            skipWhitespace(json, ref index);
            expect(json, ref index, '{');
            var row = new SavedTargetRowData();
            while (!tryConsume(json, ref index, '}'))
            {
                string key = readString(json, ref index);
                expect(json, ref index, ':');
                switch (key)
                {
                    case "Flow": row.Flow = readString(json, ref index); break;
                    case "ProductId": row.ProductId = readString(json, ref index); break;
                    case "RecipeId": row.RecipeId = readString(json, ref index); break;
                    case "Rate": row.Rate = readFloat(json, ref index); break;
                    case "Machines": row.Machines = readFloat(json, ref index); break;
                    case "IsFixed": row.IsFixed = (readString(json, ref index) == "true"); break;
                }
                skipWhitespace(json, ref index);
                tryConsume(json, ref index, ',');
            }
            return row;
        }

        private static void skipValue(string json, ref int index)
        {
            while (index < json.Length && json[index] != ',' && json[index] != '}') index++;
        }
    }
}