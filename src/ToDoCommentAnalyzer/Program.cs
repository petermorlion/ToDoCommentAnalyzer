using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ToDoCommentAnalyzer
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var csvConfiguration = new Configuration
                {
                    MissingFieldFound = null,
                    Delimiter = ",",
                    HeaderValidated = null
                };

                using (var streamReader = new StreamReader(@"C:\Users\peter\Projects\ToDoCommentAnalyzer\data\bq-results-20190409-112119-si88464mntq.csv"))
                using (var csvReader = new CsvReader(streamReader, csvConfiguration))
                using (var streamWriter = new StreamWriter(@"C:\Users\peter\Projects\ToDoCommentAnalyzer\data\bq-results-20190409-112119-si88464mntq-results.csv"))
                using (var csvWriter = new CsvWriter(streamWriter))
                using (var errorLogWriter = new StreamWriter(@"C:\Users\peter\Projects\ToDoCommentAnalyzer\data\bq-results-20190409-112119-si88464mntq-errors.log"))
                {
                    var toDoComments = csvReader.GetRecords<ToDoComment>();
                    int lineNumber = 1;
                    var results = new List<ToDoComment>();
                    foreach (var toDoComment in toDoComments)
                    {
                        try
                        {
                            var result = await GetLineNumber(toDoComment);
                            result = await GetCommitDateAndAge(result);
                            results.Add(result);
                            await streamWriter.WriteLineAsync($"{result.Repository},{result.Path},{result.LineNumber},{result.AgeInDays}");
                            lineNumber++;
                            Console.WriteLine($"Analyzed {result.Repository},{result.Path},{result.LineNumber},{result.AgeInDays}");
                        }
                        catch (Exception e)
                        {
                            await errorLogWriter.WriteLineAsync($"Error at line {lineNumber}: {e}");
                        }
                    }

                    csvWriter.WriteRecords(results);
                }
            }
            catch (TypeConverterException e)
            {
                Console.WriteLine($"Error at line {e.ReadingContext.RawRecord}");
                throw;
            }
        }

        private static async Task<ToDoComment> GetCommitDateAndAge(ToDoComment input)
        {
            var owner = input.Repository.Split("/")[0];
            var repository = input.Repository.Split("/")[1];
            var query = "{repository(owner: \\\"" + owner + "\\\", name: \\\"" + repository + "\\\") {"
                        + " defaultBranchRef {"
                            + "name"
                        + "}"
                        + " object(expression: \\\"master\\\") {"
                            + " ... on Commit {"
                                + " blame(path: \\\"" + input.Path + "\\\") {"
                                    + " ranges {"
                                        + " startingLine"
                                        + " endingLine" 
                                    + " commit {"
                                            + " committedDate"
                                    + " }"
                                + " }"
                            + " }"
                        + " }"
                    + " }"
                + " }"
            + " }";

            var httpClient = new HttpClient();
            var url = "https://api.github.com/graphql";
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent("{ \"query\": \"" + query + "\"")
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "");
            request.Headers.Add("User-Agent", "petermorlion");

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(content);
            var o = data.data.repository["object"] as dynamic;
            var ranges = o.blame.ranges as IEnumerable<dynamic>;
            var range = ranges.SingleOrDefault(r => (int)r.startingLine <= input.LineNumber && (int)r.endingLine >= input.LineNumber);
            var committedDate = (DateTime)range.commit.committedDate;
            var ageInDays = (DateTime.UtcNow - committedDate).Days;

            return new ToDoComment
            {
                Repository = input.Repository,
                Path = input.Path,
                Position = input.Position,
                LineNumber = input.LineNumber,
                CommitDate = committedDate,
                AgeInDays = ageInDays
            };
        }

        private static async Task<ToDoComment> GetLineNumber(ToDoComment input)
        {
            var httpClient = new HttpClient();
            var url = $"https://raw.githubusercontent.com/{input.Repository}/master/{input.Path}";
            var text = await httpClient.GetStringAsync(url);
            var lineNumber = text.Take(input.Position).Count(c => c == '\n') + 1;
            return new ToDoComment
            {
                Repository = input.Repository,
                Path = input.Path,
                Position = input.Position,
                LineNumber = lineNumber
            };
        }
    }

    internal class ToDoComment
    {
        [Index(0)]
        public string Repository { get; set; }

        [Index(1)]
        public string Path { get; set; }

        [Index(2)]
        public int Position { get; set; }

        public int LineNumber { get; set; }

        public DateTime CommitDate { get; set; }

        public int AgeInDays { get; set; }
    }
}
