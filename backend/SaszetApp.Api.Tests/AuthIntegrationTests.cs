using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SaszetApp.Api.Tests
{
    public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AuthIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AdminEndpoint_ReturnsUnauthorized_WhenNoToken()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/AdminProvider");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CustomerEndpoint_ReturnsUnauthorized_WhenNoToken()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/Scan/search?query=123");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
