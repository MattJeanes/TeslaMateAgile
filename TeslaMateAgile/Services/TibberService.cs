using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TeslaMateAgile.Data;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Services
{
    public class TibberService : IPriceDataService
    {
        private readonly HttpClient _client;
        private readonly GraphQLHttpClientOptions _graphQLHttpClientOptions;
        private readonly TibberOptions _options;
        private readonly IGraphQLJsonSerializer _graphQLJsonSerializer;

        public TibberService(
            HttpClient client,
            IGraphQLJsonSerializer graphQLJsonSerializer,
            IOptions<TibberOptions> options
            )
        {
            _client = client;
            _options = options.Value;
            _graphQLHttpClientOptions = new GraphQLHttpClientOptions { EndPoint = new Uri(_options.BaseUrl) };
            _graphQLJsonSerializer = graphQLJsonSerializer;
        }

        public async Task<IEnumerable<Price>> GetPriceData(DateTimeOffset from, DateTimeOffset to)
        {
            var first = (int)Math.Ceiling((to - from).TotalHours);
            var request = new GraphQLHttpRequest
            {
                Query = @"
query PriceData($after: String, $first: Int) {
    viewer {
        homes {
            currentSubscription {
                priceInfo{
                    range(resolution: HOURLY, after: $after, first: $first) {
                        nodes {
                            total
                            startsAt
                        }
                    }
                }
            }
        }
    }
}
",
                OperationName = "PriceData",
                Variables = new
                {
                    after = Convert.ToBase64String(Encoding.UTF8.GetBytes(from.AddHours(-1).ToString("o"))),
                    first
                }
            };
            var graphQLHttpResponse = await SendRequest(request);
            return graphQLHttpResponse
                .Data
                .Viewer
                .Homes
                .First()
                .CurrentSubscription
                .PriceInfo
                .Range
                .Nodes
                .Select(x => new Price
                {
                    ValidFrom = x.StartsAt,
                    ValidTo = x.StartsAt.AddHours(1),
                    Value = x.Total
                });
        }

        private async Task<GraphQLHttpResponse<ResponseType>> SendRequest(GraphQLHttpRequest request)
        {
            using var httpRequestMessage = request.ToHttpRequestMessage(_graphQLHttpClientOptions, _graphQLJsonSerializer);
            using var httpResponseMessage = await _client.SendAsync(httpRequestMessage);
            var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var graphQLResponse = await JsonSerializer.DeserializeAsync<GraphQLResponse<ResponseType>>(contentStream, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var graphQLHttpResponse = graphQLResponse.ToGraphQLHttpResponse(httpResponseMessage.Headers, httpResponseMessage.StatusCode);
                if (graphQLHttpResponse.Errors?.Any() ?? false)
                {
                    var errorMessages = string.Join(", ", graphQLHttpResponse.Errors.Select(x => x.Message));
                    throw new HttpRequestException($"Failed to call Tibber API: {errorMessages}");
                }
                return graphQLHttpResponse;
            }

            string content = null;
            if (contentStream != null)
            {
                using var sr = new StreamReader(contentStream);
                content = await sr.ReadToEndAsync();
            }

            throw new GraphQLHttpRequestException(httpResponseMessage.StatusCode, httpResponseMessage.Headers, content);
        }

        private class ResponseType
        {
            public Viewer Viewer { get; set; }
        }

        private class Viewer
        {
            public List<Home> Homes { get; set; }
        }

        private class Home
        {
            public Subscription CurrentSubscription { get; set; }
        }

        private class Subscription
        {
            public PriceInfo PriceInfo { get; set; }
        }

        private class PriceInfo
        {
            public RangeInfo Range { get; set; }
        }

        private class RangeInfo
        {
            public List<Node> Nodes { get; set; }
        }

        private class Node
        {
            public decimal Total { get; set; }
            public DateTimeOffset StartsAt { get; set; }
        }
    }
}
