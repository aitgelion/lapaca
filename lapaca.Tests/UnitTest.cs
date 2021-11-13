using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace lapaca.Tests
{
    public class UnitTest
    {
        [Fact]
        public async Task CheckSignature()
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title}}}";

            var response = await client.PostAsJsonAsync("/api/wh", new CreateUrlDto("m8f6uFJ95yHRvyVDjYESPw==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { title = "lapaca" } });
            Assert.False(webHookResult.IsSuccessStatusCode);
        }

        [Fact]
        public async Task CreateWebHook()
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title}}}";

            var response = await client.PostAsJsonAsync("/api/wh", new CreateUrlDto("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { title = "lapaca" } });
            var webHookContent = await webHookResult.Content.ReadAsStringAsync();

            Assert.Equal("{\"title\": \"lapaca\"}", webHookContent);
        }

        [Fact]
        public async Task CreateWebHookDefaultNumberValue()
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title??12}}}";

            var response = await client.PostAsJsonAsync("/api/wh", new CreateUrlDto("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { subTitle = "lapaca" } });
            var webHookContent = await webHookResult.Content.ReadAsStringAsync();

            Assert.Equal("{\"title\": 12}", webHookContent);
        }

        [Fact]
        public async Task CreateWebHookDefaultStringValue()
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title??\"No title!\"}}}";

            var response = await client.PostAsJsonAsync("/api/wh", new CreateUrlDto("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { subTitle = "lapaca" } });
            var webHookContent = await webHookResult.Content.ReadAsStringAsync();

            Assert.Equal("{\"title\": \"No title!\"}", webHookContent);
        }

        [Fact]
        public async Task CreateWebHookDefaultJsonValue()
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title??\\{\"name\": \"title\"\\}}}}";

            var response = await client.PostAsJsonAsync("/api/wh", new CreateUrlDto("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { subTitle = "lapaca" } });
            var webHookContent = await webHookResult.Content.ReadAsStringAsync();

            Assert.Equal("{\"title\": {\"name\": \"title\"}}", webHookContent);
        }
    }
}

