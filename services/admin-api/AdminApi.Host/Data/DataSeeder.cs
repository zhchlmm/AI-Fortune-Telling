using Microsoft.EntityFrameworkCore;
using AdminApi.Host.Services;

namespace AdminApi.Host.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AdminDbContext dbContext)
    {
        await dbContext.Database.MigrateAsync();

        if (!await dbContext.ContentCategories.AnyAsync())
        {
            dbContext.ContentCategories.AddRange(
                new ContentCategoryEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "星座",
                    SortOrder = 10,
                    IsEnabled = true,
                    UpdatedAt = DateTime.UtcNow
                },
                new ContentCategoryEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "塔罗",
                    SortOrder = 20,
                    IsEnabled = true,
                    UpdatedAt = DateTime.UtcNow
                },
                new ContentCategoryEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "命理",
                    SortOrder = 30,
                    IsEnabled = true,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            await dbContext.SaveChangesAsync();
        }

        var zodiacCategoryId = await dbContext.ContentCategories
            .Where(x => x.Name == "星座")
            .Select(x => x.Id)
            .FirstAsync();

        var tarotCategoryId = await dbContext.ContentCategories
            .Where(x => x.Name == "塔罗")
            .Select(x => x.Id)
            .FirstAsync();

        if (!await dbContext.Contents.AnyAsync())
        {
            dbContext.Contents.AddRange(
                new ContentItemEntity
                {
                    Id = Guid.NewGuid(),
                    Title = "本周星座运势速览",
                    Summary = "本周十二星座在感情与事业上的关键提示。",
                    Content = "<p>本周整体运势平稳，建议你把重心放在长期目标上。</p><p><strong>事业：</strong>适合推进重要计划，保持节奏。</p><p><strong>感情：</strong>沟通是关键，表达更具体会更有帮助。</p>",
                    CategoryId = zodiacCategoryId,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-1)
                },
                new ContentItemEntity
                {
                    Id = Guid.NewGuid(),
                    Title = "塔罗入门：三张牌基础解读",
                    Summary = "通过过去、现在、未来三张牌快速建立解读框架。",
                    Content = "<p>三张牌解读适合新手快速上手。</p><p>第一张代表过去，第二张代表现在，第三张代表未来趋势。</p><p>建议结合提问背景，不要脱离现实情境。</p>",
                    CategoryId = tarotCategoryId,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-2)
                }
            );
        }

        if (!await dbContext.Templates.AnyAsync())
        {
            dbContext.Templates.AddRange(
                new FortuneTemplateEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "塔罗三牌基础模板",
                    FortuneType = "Tarot",
                    Prompt = "请根据过去、现在、未来三张牌，给出简明解读。",
                    IsEnabled = true,
                    UpdatedAt = DateTime.UtcNow
                },
                new FortuneTemplateEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "星座本周运势模板",
                    FortuneType = "Zodiac",
                    Prompt = "请围绕事业、感情、健康给出本周建议。",
                    IsEnabled = true,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }

        if (!await dbContext.AdminUsers.AnyAsync(x => x.Username == "admin"))
        {
            var passwordHasher = new PasswordHasher();
            var salt = passwordHasher.GenerateSalt();
            var hash = passwordHasher.HashPassword("admin123", salt);

            dbContext.AdminUsers.Add(new AdminUserEntity
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                PasswordSalt = salt,
                PasswordHash = hash,
                IsActive = true,
                RequirePasswordChange = true,
                FailedLoginCount = 0,
                LockoutEndTimeUtc = null,
                CreatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
