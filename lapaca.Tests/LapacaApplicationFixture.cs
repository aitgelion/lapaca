using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lapaca.Tests;

internal class LapacaApplicationFixture : WebApplicationFactory<Program>
{
    private readonly string _environment;

    // public IConfiguration Configuration;

    public LapacaApplicationFixture(string environment = "Development")
    {
        _environment = environment;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
        });

        // TODO: add here mocking services
        builder.ConfigureServices(services =>
        {
            // Return the same body as request
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken token) => {
                string requestMessageContent = await request.Content.ReadAsStringAsync();
                HttpResponseMessage response = new HttpResponseMessage();
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(requestMessageContent);
                return response;
            })
            .Verifiable();

            // Mock client with the handler
            var client = new HttpClient(mockHttpMessageHandler.Object);
            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            services.AddSingleton<IHttpClientFactory>(mockFactory.Object);
        });

        return base.CreateHost(builder);
    }
}