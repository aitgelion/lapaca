using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace lapaca.Tests
{
    public class UnitTestEx
    {
        [Fact]
        public async Task CheckSignature()
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title}}}";

            var response = await client.PostAsJsonAsync("/api/whex", new WebHookConfig("m8f6uFJ95yHRvyVDjYESPw==", url, scheme));
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

            var response = await client.PostAsJsonAsync("/api/whex", new WebHookConfig("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
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

            var response = await client.PostAsJsonAsync("/api/whex", new WebHookConfig("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
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

            var response = await client.PostAsJsonAsync("/api/whex", new WebHookConfig("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
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

            var response = await client.PostAsJsonAsync("/api/whex", new WebHookConfig("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { subTitle = "lapaca" } });
            var webHookContent = await webHookResult.Content.ReadAsStringAsync();

            Assert.Equal("{\"title\": {\"name\": \"title\"}}", webHookContent);
        }

        [Fact]
        public async Task CompareBigUrls()
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"type\":\"AdaptiveCard\",\"version\":\"1.0\",\"body\":[{\"type\":\"TextBlock\",\"text\": ${{obj.msg}}},{\"type\":\"Image\",\"url\":\"http://adaptivecards.io/content/cats/1.png\"}]}";

            var response = await client.PostAsJsonAsync("/api/wh", new WebHookConfig("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            response = await client.PostAsJsonAsync("/api/whex", new WebHookConfig("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrlEx = await response.Content.ReadFromJsonAsync<string>();

            // Compare results:
            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { msg = "lapaca" } });
            var webHookContent = await webHookResult.Content.ReadAsStringAsync();

            webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { msg = "lapaca" } });
            var webHookContentEx = await webHookResult.Content.ReadAsStringAsync();

            Assert.Equal(webHookContent, webHookContentEx);

            Assert.True(finalUrl?.Length > finalUrlEx?.Length);
        }
    }
}

