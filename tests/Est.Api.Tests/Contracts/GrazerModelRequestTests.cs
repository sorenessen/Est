using System.Text.Json;
using Est.Api.Contracts;

namespace Est.Api.Tests.Contracts;

public sealed class GrazerModelRequestTests
{
    [Fact]
    public void Deserialize_EmptyObject_DoesNotEnableSurfaceWaterMortality()
    {
        var request =
            JsonSerializer.Deserialize<GrazerModelRequest>(
                "{}");

        Assert.NotNull(
            request);

        Assert.Equal(
            0,
            request.WaterAbsenceMortalityRatePerDay);
    }
}
