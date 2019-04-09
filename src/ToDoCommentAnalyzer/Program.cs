using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ToDoCommentAnalyzer
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var toDoItem = new ToDoItem
            {
                Repository = "00091701/ADFC-NewsApp-Mono",
                Path = "NewsAppDroid/NewsAppDroid/BusLog/Database/Rss.cs",
                Position = 7778
            };

            var result = await GetLineNumber(toDoItem);
            result = await GetCommitDate(toDoItem);
            Console.WriteLine(result.LineNumber);
        }

        private static async Task<ToDoItem> GetCommitDate(ToDoItem input)
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
            
            return input;
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
    }
}
