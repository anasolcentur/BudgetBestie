using PocketBudget.Services;
using System.Net;
using System.Text;

namespace PocketBudget.Tests;

public class ApiServiceTests
{
    [Fact]
    public async Task GetCategoriesAsync_WhenApiReturnsCategories_ReturnsFormattedCategories()
    {
        var json = "[\"electronics\", \"jewelery\"]";

        var httpClient = new HttpClient(
            new FakeHttpMessageHandler(HttpStatusCode.OK, json));

        var service = new ApiService(httpClient);

        var categories = await service.GetCategoriesAsync();

        Assert.Equal(2, categories.Count);
        Assert.Equal("Electrónica", categories[0].Name);
        Assert.Equal("Accesorios", categories[1].Name);
    }

    [Fact]
    public async Task GetCategoriesAsync_WhenApiReturnsServerError_ThrowsHttpRequestException()
    {
        var httpClient = new HttpClient(
            new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, ""));

        var service = new ApiService(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetCategoriesAsync());

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}