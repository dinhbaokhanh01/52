using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class InternEndpoints
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };

    public static RouteGroupBuilder MapInternEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/interns").WithTags("Thực tập sinh");

        group.MapPost("/register", async (
            RegisterInternRequest request,
            IConfiguration configuration,
            CareerDbContext db,
            IPasswordHasher<InternProfile> passwordHasher) =>
        {
            var studentId = request.StudentId.Trim();
            var email = request.Email.Trim();
            if (await db.InternProfiles.AnyAsync(profile =>
                    profile.StudentId == studentId || profile.Email == email))
            {
                return Results.Conflict(new { message = "Mã sinh viên hoặc email đã được đăng ký." });
            }

            var profile = new InternProfile
            {
                Name = request.Name.Trim(),
                StudentId = studentId,
                Email = email,
                School = request.School.Trim(),
                Major = request.Major.Trim(),
                PasswordHash = string.Empty,
                Status = "Chờ hồ sơ"
            };
            profile.PasswordHash = passwordHasher.HashPassword(profile, request.Password);
            db.InternProfiles.Add(profile);
            await db.SaveChangesAsync();

            var verification = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            db.WorkItems.Add(new WorkItem { Kind = "intern-access", ProfileId = profile.Id, Title = "Email verification", Status = "Unverified", Detail = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(verification))), End = DateTimeOffset.UtcNow.AddDays(1), UniqueKey = $"access:{profile.Id}" });
            var publicUrl = (configuration["PublicUrl"] ?? "http://localhost:5134").TrimEnd('/');
            db.MailJobs.Add(new MailJob { Recipient = profile.Email, Subject = "Xác thực email đăng ký", Body = $"Mở liên kết để xác thực email (hiệu lực 24 giờ): {publicUrl}/index.html?verify={verification}&profileId={profile.Id}" });
            await db.SaveChangesAsync();

            return Results.Created($"/api/interns/{profile.Id}/workspace", profile.ToResponse());
        })
        .WithName("RegisterIntern");

        group.MapPost("/login", async (
            LoginInternRequest request,
            HttpContext context,
            CareerDbContext db,
            IPasswordHasher<InternProfile> passwordHasher) =>
        {
            var identity = request.Identity.Trim();
            var profile = await db.InternProfiles.FirstOrDefaultAsync(item =>
                item.StudentId == identity || item.Email == identity);
            if (profile is null || passwordHasher.VerifyHashedPassword(
                    profile, profile.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return Results.Json(
                    new { message = "Mã sinh viên/email hoặc mật khẩu không chính xác." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            if (await db.WorkItems.AnyAsync(x => x.Kind == "intern-access" && x.ProfileId == profile.Id && x.Status == "Disabled")) return Results.Unauthorized();

            await PortalSecurity.SignIn(context, profile.Id, "Intern", profile.Name);
            return Results.Ok(profile.ToResponse());
        })
        .WithName("LoginIntern");

        group.MapPut("/{profileId:int}", async (
            int profileId, UpdateInternProfileRequest request, CareerDbContext db) =>
        {
            var profile = await db.InternProfiles.FindAsync(profileId);
            if (profile is null) return Results.NotFound();
            var email = request.Email.Trim();
            if (await db.InternProfiles.AnyAsync(item => item.Id != profileId && item.Email == email))
                return Results.Conflict(new { message = "Email đã được tài khoản khác sử dụng." });

            profile.Name = request.Name.Trim();
            profile.Email = email;
            profile.School = request.School.Trim();
            profile.Major = request.Major.Trim();
            await db.SaveChangesAsync();
            return Results.Ok(profile.ToResponse());
        })
        .WithName("UpdateInternProfile");

        group.MapGet("/{profileId:int}/workspace", async (int profileId, CareerDbContext db) =>
        {
            var profile = await db.InternProfiles
                .Include(item => item.Documents)
                .Include(item => item.Application)
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == profileId);
            if (profile is null) return Results.NotFound();

            var reviewHistory = await db.InternReviewHistories.AsNoTracking()
                .Where(history => history.ProfileId == profileId)
                .OrderByDescending(history => history.ReviewedAt)
                .ToListAsync();

            return Results.Ok(new InternWorkspaceResponse(
                profile.ToResponse(),
                profile.Application?.ToResponse(),
                profile.Documents.OrderByDescending(document => document.UploadedAt)
                    .Select(document => document.ToResponse()).ToArray(),
                reviewHistory.Select(history => history.ToResponse()).ToArray()));
        })
        .WithName("GetInternWorkspace");

        group.MapPost("/{profileId:int}/documents", async (
            int profileId,
            HttpRequest request,
            CareerDbContext db) =>
        {
            var profile = await db.InternProfiles.FindAsync(profileId);
            if (profile is null) return Results.NotFound();

            var fileName = Path.GetFileName(request.Query["fileName"].ToString());
            var type = request.Query["type"].ToString().Trim();
            var extension = Path.GetExtension(fileName);
            var contentType = request.ContentType?.Split(';', 2)[0].Trim() ?? "application/octet-stream";
            if (!AllowedExtensions.Contains(extension) || type is not ("CV" or "Đơn xin thực tập"))
                return Results.BadRequest(new { message = "Loại tài liệu hoặc định dạng tệp không được hỗ trợ." });
            if (request.ContentLength is > MaxUploadBytes)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

            await using var buffer = new MemoryStream();
            await request.Body.CopyToAsync(buffer, request.HttpContext.RequestAborted);
            if (buffer.Length == 0 || buffer.Length > MaxUploadBytes)
                return Results.BadRequest(new { message = "Tệp rỗng hoặc vượt quá giới hạn 10 MB." });

            var previous = await db.InternDocuments
                .Where(document => document.ProfileId == profileId && document.Type == type)
                .ToListAsync();
            db.InternDocuments.RemoveRange(previous);
            var document = new InternDocument
            {
                ProfileId = profileId,
                Type = type,
                FileName = fileName,
                ContentType = contentType,
                Content = buffer.ToArray(),
                Status = "Chờ duyệt"
            };
            db.InternDocuments.Add(document);
            profile.Status = "Chờ duyệt";
            await db.SaveChangesAsync();

            return Results.Created($"/api/documents/{document.Id}/file", document.ToResponse());
        })
        .WithName("UploadInternDocument");

        group.MapPost("/{profileId:int}/submit", async (int profileId, CareerDbContext db) =>
        {
            if (await db.WorkItems.AnyAsync(x => x.Kind == "intern-access" && x.ProfileId == profileId && x.Status == "Unverified"))
                return Results.BadRequest(new { message = "Cần xác thực email qua liên kết đã gửi trước khi nộp hồ sơ." });
            var profile = await db.InternProfiles
                .Include(item => item.Documents)
                .Include(item => item.Application)
                .FirstOrDefaultAsync(item => item.Id == profileId);
            if (profile is null) return Results.NotFound();

            var hasCv = profile.Documents.Any(document => document.Type == "CV" && document.Status != "Từ chối");
            var hasLetter = profile.Documents.Any(document => document.Type == "Đơn xin thực tập" && document.Status != "Từ chối");
            if (!hasCv || !hasLetter)
                return Results.BadRequest(new { message = "Cần tải lên CV và đơn xin thực tập trước khi nộp." });

            if (profile.Application is null)
            {
                profile.Application = new InternApplication { ProfileId = profile.Id, Status = "Chờ duyệt" };
            }
            else
            {
                profile.Application.Status = "Chờ duyệt";
                profile.Application.AppliedAt = DateTimeOffset.UtcNow;
            }
            profile.Status = "Chờ duyệt";
            await db.SaveChangesAsync();
            return Results.Ok(profile.Application.ToResponse());
        })
        .WithName("SubmitInternApplication");

        group.MapPost("/{profileId:int}/documents/{documentId:int}/confirm-contract", async (
            int profileId, int documentId, CareerDbContext db) =>
        {
            var contract = await db.InternDocuments.FirstOrDefaultAsync(document =>
                document.Id == documentId && document.ProfileId == profileId && document.Type == "Hợp đồng thực tập");
            if (contract is null) return Results.NotFound();
            if (contract.Status != "Chờ xác nhận")
                return Results.Conflict(new { message = "Hợp đồng hiện không ở trạng thái chờ xác nhận." });

            contract.Status = "Đã xác nhận";
            contract.ConfirmedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(contract.ToResponse());
        })
        .WithName("ConfirmInternContract");

        return group;
    }
}
