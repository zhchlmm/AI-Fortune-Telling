using AdminApi.Host.Models;
using AdminApi.Host.Services;
using AdminApi.Host.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace AdminApi.Host.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    JwtTokenService tokenService,
    PasswordHasher passwordHasher,
    AdminDbContext dbContext) : ControllerBase
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var user = await dbContext.AdminUsers.FirstOrDefaultAsync(x => x.Username == request.Username);
        if (user is null || !user.IsActive)
        {
            await WriteAuditAsync(request.Username, ipAddress, false, "user_not_found_or_inactive");
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        if (user.LockoutEndTimeUtc.HasValue && user.LockoutEndTimeUtc.Value > now)
        {
            var remainMinutes = (int)Math.Ceiling((user.LockoutEndTimeUtc.Value - now).TotalMinutes);
            await WriteAuditAsync(request.Username, ipAddress, false, "locked_out");
            return StatusCode(423, new { message = $"账号已被锁定，请在 {remainMinutes} 分钟后重试" });
        }

        var verified = passwordHasher.Verify(request.Password, user.PasswordSalt, user.PasswordHash);
        if (!verified)
        {
            user.FailedLoginCount += 1;

            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.FailedLoginCount = 0;
                user.LockoutEndTimeUtc = now.AddMinutes(LockoutMinutes);
                await dbContext.SaveChangesAsync();
                await WriteAuditAsync(request.Username, ipAddress, false, "locked_by_failed_attempts");
                return StatusCode(423, new { message = $"密码错误次数过多，账号已锁定 {LockoutMinutes} 分钟" });
            }

            await dbContext.SaveChangesAsync();
            var remain = MaxFailedAttempts - user.FailedLoginCount;
            await WriteAuditAsync(request.Username, ipAddress, false, "wrong_password");
            return Unauthorized(new { message = $"用户名或密码错误，还可尝试 {remain} 次" });
        }

        user.FailedLoginCount = 0;
        user.LockoutEndTimeUtc = null;
        await dbContext.SaveChangesAsync();
        await WriteAuditAsync(request.Username, ipAddress, true, "login_success");

        var (token, expiresAt) = tokenService.GenerateToken(user.Username);
        return Ok(new LoginResponse(token, expiresAt, user.RequirePasswordChange));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "新密码长度不能少于6位" });
        }

        var user = await dbContext.AdminUsers.FirstOrDefaultAsync(x => x.Username == username);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var currentPasswordVerified = passwordHasher.Verify(request.CurrentPassword, user.PasswordSalt, user.PasswordHash);
        if (!currentPasswordVerified)
        {
            return BadRequest(new { message = "当前密码错误" });
        }

        var newSalt = passwordHasher.GenerateSalt();
        var newHash = passwordHasher.HashPassword(request.NewPassword, newSalt);

        user.PasswordSalt = newSalt;
        user.PasswordHash = newHash;
        user.RequirePasswordChange = false;
        user.FailedLoginCount = 0;
        user.LockoutEndTimeUtc = null;

        await dbContext.SaveChangesAsync();

        return Ok(new { message = "密码修改成功" });
    }

    private async Task WriteAuditAsync(string username, string ipAddress, bool isSuccess, string reason)
    {
        dbContext.LoginAudits.Add(new LoginAuditEntity
        {
            Id = Guid.NewGuid(),
            Username = username,
            IpAddress = ipAddress,
            IsSuccess = isSuccess,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
}
