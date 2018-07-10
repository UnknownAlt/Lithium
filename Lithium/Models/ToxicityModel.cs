namespace Lithium.Models
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Text;

    using Newtonsoft.Json;

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1404:CodeAnalysisSuppressionMustHaveJustification", Justification = "Reviewed. Suppression is OK here.")]
    public class Perspective
    {
        public class Api
        {
            private readonly string url;

            public Api(string apikey)
            {
                ApiKey = apikey;
                url = $"https://commentanalyzer.googleapis.com/v1alpha1/comments:analyze?key={ApiKey}";
            }

            private string ApiKey { get; }

            public AnalyzeCommentResponse SendRequest(AnalyzeCommentRequest request)
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                        "application/json");
                    var response = client.PostAsync(url, content).Result;
                    response.EnsureSuccessStatusCode();
                    var data = response.Content.ReadAsStringAsync().Result;
                    var result = JsonConvert.DeserializeObject<AnalyzeCommentResponse>(data);
                    return result;
                }
            }

            public string GetResponseString(AnalyzeCommentRequest request)
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                        "application/json");
                    var response = client.PostAsync(url, content).Result;
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().Result;
                }
            }

            public AnalyzeCommentResponse QueryToxicity(string input)
            {
                var requestedAttributes =
                    new Dictionary<string, RequestedAttributes> { { "TOXICITY", new RequestedAttributes() } };
                var req = new AnalyzeCommentRequest(input, requestedAttributes);
                var res = SendRequest(req);
                return res;
            }
        }

        public class AnalyzeCommentRequest
        {
            public string ClientToken { get; }

            public Comment Comment { get; }

            public bool DoNotStore { get; }

            public string[] Languages { get; } = { "en" };

            public Dictionary<string, RequestedAttributes> Attributes { get; }

            public AnalyzeCommentRequest(string comment,
                Dictionary<string, RequestedAttributes> requestedAttributes = null, bool doNotStore = true,
                string clientToken = null)
            {
                Comment = new Comment(comment);
                DoNotStore = doNotStore;
                if (requestedAttributes == null)
                {
                    Attributes.Add("TOXICITY", new RequestedAttributes());
                }
                else
                {
                    Attributes = requestedAttributes;
                }

                ClientToken = clientToken;
            }
        }

        [SuppressMessage("ReSharper", "StyleCop.SA1300")]
        public class AnalyzeCommentResponse
        {
            public AttributeScores attributeScores { get; set; }

            public List<string> languages { get; set; }

            public class AttributeScores
            {
                public _TOXICITY TOXICITY { get; set; }

                public class _TOXICITY
                {
                    public List<SpanScore> spanScores { get; set; }

                    public SummaryScore summaryScore { get; set; }

                    public class SpanScore
                    {
                        public int begin { get; set; }

                        public int end { get; set; }

                        public Score score { get; set; }

                        public class Score
                        {
                            public double value { get; set; }

                            public string type { get; set; }
                        }
                    }

                    public class SummaryScore
                    {
                        public double value { get; set; }

                        public string type { get; set; }
                    }
                }
            }
        }

        [SuppressMessage("ReSharper", "StyleCop.SA1300")]
        public class Comment
        {
            public Comment(string text, string type = "PLAIN_TEXT")
            {
                this.text = text;
                this.type = type;
            }

            public string text { get; set; }

            public string type { get; set; }

            public static implicit operator string(Comment v)
            {
                throw new NotImplementedException();
            }
        }

        [SuppressMessage("ReSharper", "StyleCop.SA1300")]
        public class RequestedAttributes
        {
            public float scoreThreshold { get; }

            public string scoreType { get; }

            public RequestedAttributes(string scoretype = "PROBABILITY", float scorethreshold = 0)
            {
                scoreType = scoretype;
                scoreThreshold = scorethreshold;
            }
        }
    }
}