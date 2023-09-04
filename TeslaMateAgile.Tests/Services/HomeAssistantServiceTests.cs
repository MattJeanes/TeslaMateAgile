using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Contrib.HttpClient;
using NUnit.Framework;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services;

namespace TeslaMateAgile.Tests.Services;

public class HomeAssistantServiceTests
{
    private HomeAssistantService _subject;
    private Mock<HttpMessageHandler> _handler;

    [SetUp]
    public void Setup()
    {
        _handler = new Mock<HttpMessageHandler>();
        var httpClient = _handler.CreateClient();
        var homeAssistantOptions = Options.Create(new HomeAssistantOptions { BaseUrl = "http://homeassistant", EntityId = "input_number.test" });
        httpClient.BaseAddress = new Uri(homeAssistantOptions.Value.BaseUrl);
        var teslaMateOptions = Options.Create(new TeslaMateOptions { });
        var logger = new Mock<ILogger<HomeAssistantService>>();
        _subject = new HomeAssistantService(httpClient, homeAssistantOptions, teslaMateOptions, logger.Object);
    }

    [Test]
    public async Task TestAsync()
    {
        var jsonFile = "ha_test.json";
        var json = File.ReadAllText(Path.Combine("Prices", jsonFile));

        _handler.SetupAnyRequest()
            .ReturnsResponse(json, "application/json");

        var startDate = DateTimeOffset.Parse("2023-08-24T23:43:53Z");
        var endDate = DateTimeOffset.Parse("2023-08-25T03:19:42Z");
        var prices = await _subject.GetPriceData(startDate, endDate);
        var priceList = prices.ToList();

        _handler.VerifyAnyRequest(Times.Once());

        Assert.That(priceList.Count, Is.EqualTo(1));
        Assert.That(priceList[0].ValidFrom, Is.EqualTo(startDate));
        Assert.That(priceList[0].ValidTo, Is.EqualTo(endDate));
        Assert.That(priceList[0].Value, Is.EqualTo(0.2748M));
    }
}
