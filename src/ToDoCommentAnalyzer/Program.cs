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
            using (var fr = new FileStream(@"C:\Users\peter\Projects\ToDoCommentAnalyzer\data\bq-results-20190409-112119-si88464mntq.csv", FileMode.Open, FileAccess.Read))
            {
                using (var stringReader = new StreamReader(fr))
                {
                    string line;
                    while ((line = await stringReader.ReadLineAsync()) != null)
                    {
                        if (line.Contains("sample_repo_name"))
                        {
                            continue;
                        }

                        try
                        {
                            var toDoItem = new ToDoItem
                            {
                                Repository = line.Split(',')[0],
                                Path = line.Split(',')[1],
                                Position = int.Parse(line.Split(',')[2])
                            };

                            var result = await GetLineNumber(toDoItem);
                            result = await GetCommitDateAndAge(result);
                            Console.WriteLine(result.AgeInDays);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error");
                        }
                        
                    }
                }
            }
        }

        private static async Task<ToDoItem> GetCommitDateAndAge(ToDoItem input)
        {
            var owner = input.Repository.Split("/")[0];
            var repository = input.Repository.Split("/")[1];
            var query = "{repository(owner: \\\"" + owner + "\\\", name: \\\"" + repository + "\\\") {"
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

            return new ToDoItem
            {
                Repository = input.Repository,
                Path = input.Path,
                Position = input.Position,
                LineNumber = input.LineNumber,
                CommitDate = committedDate,
                AgeInDays = ageInDays
            };
        }

        private static async Task<ToDoItem> GetLineNumber(ToDoItem input)
        {
            var httpClient = new HttpClient();
            var url = $"https://raw.githubusercontent.com/{input.Repository}/master/{input.Path}";
            var text = await httpClient.GetStringAsync(url);
            var lineNumber = text.Take(input.Position).Count(c => c == '\n') + 1;
            return new ToDoItem
            {
                Repository = input.Repository,
                Path = input.Path,
                Position = input.Position,
                LineNumber = lineNumber
            };
        }
    }

    internal class ToDoItem
    {
        public string Repository { get; set; }
        public string Path { get; set; }
        public int Position { get; set; }
        public int LineNumber { get; set; }
        public DateTime CommitDate { get; set; }
        public int AgeInDays { get; set; }
    }
}
