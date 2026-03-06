using AdminApi.Host.Controllers;
using AdminApi.Host.Data;
using AdminApi.Host.Models;
using AdminApi.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace AdminApi.Host.Tests;

public class MiniappUsersControllerTests
{
    [Fact]
    public async Task UpdatePhone_WhenEncryptedDataIsInvalid_ReturnsBadRequestAndDoesNotUpdatePhone()
    {
        var dbOptions = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase($"miniapp-users-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new AdminDbContext(dbOptions);
        dbContext.MiniappUsers.Add(new MiniappUserEntity
        {
            Id = Guid.NewGuid(),
            OpenId = "openid-test",
            SessionKey = "MTIzNDU2Nzg5MDEyMzQ1Ng==", // base64(16 bytes)
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var controller = new MiniappUsersController(
            dbContext,
            new FakeHttpClientFactory(),
            Options.Create(new WechatMiniappOptions()),
            new MiniappTokenService(Options.Create(new MiniappJwtOptions())),
            NullLogger<MiniappUsersController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [new Claim(MiniappTokenService.OpenIdClaim, "openid-test")],
                        authenticationType: "test"))
            },
        };

        var result = await controller.UpdatePhone(new UpdateMiniappUserPhoneRequest(
            EncryptedData: "invalid-base64",
            Iv: "MTIzNDU2Nzg5MDEyMzQ1Ng=="));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("手机号解析失败", badRequest.Value?.ToString());

        var user = await dbContext.MiniappUsers.SingleAsync(x => x.OpenId == "openid-test");
        Assert.Null(user.PhoneNumber);
    }

    [Fact]
    public async Task UpdatePhone_WhenSessionKeyMissing_ReturnsBadRequestWithSessionKeyMissingCode()
    {
        var dbOptions = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase($"miniapp-users-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new AdminDbContext(dbOptions);
        dbContext.MiniappUsers.Add(new MiniappUserEntity
        {
            Id = Guid.NewGuid(),
            OpenId = "openid-test",
            SessionKey = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var controller = new MiniappUsersController(
            dbContext,
            new FakeHttpClientFactory(),
            Options.Create(new WechatMiniappOptions()),
            new MiniappTokenService(Options.Create(new MiniappJwtOptions())),
            NullLogger<MiniappUsersController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [new Claim(MiniappTokenService.OpenIdClaim, "openid-test")],
                        authenticationType: "test"))
            },
        };

        var result = await controller.UpdatePhone(new UpdateMiniappUserPhoneRequest(
            EncryptedData: "invalid-base64",
            Iv: "MTIzNDU2Nzg5MDEyMzQ1Ng=="));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<MiniappErrorResponse>(badRequest.Value);
        Assert.Equal("SESSION_KEY_MISSING", payload.Code);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
