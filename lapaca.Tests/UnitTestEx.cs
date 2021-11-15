using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace lapaca.Tests
{
    public class UnitTestEx
    {
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

