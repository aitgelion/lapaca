using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace lapaca.Tests
{
    public class UnitTest
    {
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
    }
}

