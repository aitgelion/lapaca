using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace lapaca.Tests
{
    public class UnitTest
    {
        [InlineData("wh")]
        [InlineData("whex")]
        public async Task CheckSignature(string segment)
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title}}}";

            var response = await client.PostAsJsonAsync($"/api/{segment}", new WebHookConfig("m8f6uFJ95yHRvyVDjYESPw==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { title = "lapaca" } });
            Assert.False(webHookResult.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData("wh")]
        [InlineData("whex")]
        public async Task CreateWebHook(string segment)
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title}}}";

            var response = await client.PostAsJsonAsync($"/api/{segment}", new WebHookConfig("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { title = "lapaca" } });
            var webHookContent = await webHookResult.Content.ReadAsStringAsync();

            Assert.Equal("{\"title\": \"lapaca\"}", webHookContent);
        }

        [Theory]
        [InlineData("wh")]
        [InlineData("whex")]
        public async Task CreateWebHookDefaultNumberValue(string segment)
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title??12}}}";

            var response = await client.PostAsJsonAsync($"/api/{segment}", new WebHookConfig("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { subTitle = "lapaca" } });
            var webHookContent = await webHookResult.Content.ReadAsStringAsync();

            Assert.Equal("{\"title\": 12}", webHookContent);
        }

        [Theory]
        [InlineData("wh")]
        [InlineData("whex")]
        public async Task CreateWebHookDefaultStringValue(string segment)
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title??\"No title!\"}}}";

            var response = await client.PostAsJsonAsync($"/api/{segment}", new WebHookConfig("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { subTitle = "lapaca" } });
            var webHookContent = await webHookResult.Content.ReadAsStringAsync();

            Assert.Equal("{\"title\": \"No title!\"}", webHookContent);
        }

        [Theory]
        [InlineData("wh")]
        [InlineData("whex")]
        public async Task CreateWebHookDefaultJsonValue(string segment)
        {
            await using var application = new LapacaApplicationFixture();
            var client = application.CreateClient();

            var url = "http://url.com/tokengrandote";
            var scheme = "{\"title\": ${{obj.title??\\{\"name\": \"title\"\\}}}}";

            var response = await client.PostAsJsonAsync($"/api/{segment}", new WebHookConfig("GuPrRON7FlSloWkUy1oDfQ==", url, scheme));
            var finalUrl = await response.Content.ReadFromJsonAsync<string>();

            var webHookResult = await client.PostAsJsonAsync(finalUrl, new { obj = new { subTitle = "lapaca" } });
            var webHookContent = await webHookResult.Content.ReadAsStringAsync();

            Assert.Equal("{\"title\": {\"name\": \"title\"}}", webHookContent);
        }
    }
}

