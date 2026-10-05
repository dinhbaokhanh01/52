using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class HrEndpoints
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };
    private static readonly HashSet<string> ProfileStatuses = new(StringComparer.Ordinal)
    {
        "Chờ hồ sơ", "Chờ duyệt", "Đang thực tập", "Chờ bắt đầu", "Đã hoàn thành"
    };
    private static readonly HashSet<string> DecisionStatuses = new(StringComparer.Ordinal)
    {
        "Đã duyệt", "Từ chối"
    };

    public static RouteGroupBuilder MapHrEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/hr").WithTags("Nhân sự");

        group.MapGet("/dashboard", async (CareerDbContext db) =>
        {
            var profiles = await db.InternProfiles.AsNoTracking()
                .OrderByDescending(profile => profile.CreatedAt).ToListAsync();
            var applications = await db.InternApplications.AsNoTracking()
                .OrderByDescending(application => application.AppliedAt).ToListAsync();
            var documents = await db.InternDocuments.AsNoTracking()
                .OrderByDescending(document => document.UploadedAt).ToListAsync();
            var reviewHistory = await db.InternReviewHistories.AsNoTracking()
                .OrderByDescending(history => history.ReviewedAt).ToListAsync();
            return Results.Ok(new HrDashboardResponse(
                profiles.Select(profile => profile.ToResponse()).ToArray(),
                applications.Select(application => application.ToResponse()).ToArray(),
                documents.Select(document => document.ToResponse()).ToArray(),
                reviewHistory.Select(history => history.ToResponse()).ToArray()));
        })
        .WithName("GetHrDashboard");

        group.MapPost("/profiles", async (
            CreateHrProfileRequest request,
            CareerDbContext db,
            IPasswordHasher<InternProfile> passwordHasher) =>
        {
            if (request.EndDate < request.StartDate)
                return Results.BadRequest(new { message = "Ngày kết thúc phải sau ngày bắt đầu." });
            if (!ProfileStatuses.Contains(request.Status))
                return Results.BadRequest(new { message = "Trạng thái hồ sơ không hợp lệ." });
            if (await db.InternProfiles.AnyAsync(profile =>
                    profile.StudentId == request.StudentId.Trim() || profile.Email == request.Email.Trim()))
                return Results.Conflict(new { message = "Mã sinh viên hoặc email đã tồn tại." });

            var profile = new InternProfile
            {
                Name = request.Name.Trim(),
                StudentId = request.StudentId.Trim(),
                Email = request.Email.Trim(),
                School = request.School.Trim(),
                Major = request.Major.Trim(),
                PasswordHash = string.Empty,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status
            };
            profile.PasswordHash = passwordHasher.HashPassword(profile, Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
            db.InternProfiles.Add(profile);
            await db.SaveChangesAsync();
            return Results.Created($"/api/hr/profiles/{profile.Id}", profile.ToResponse());
        })
        .WithName("CreateHrProfile");

        group.MapPut("/profiles/{profileId:int}", async (
            int profileId, UpdateHrProfileRequest request, CareerDbContext db) =>
        {
            if (request.EndDate < request.StartDate)
                return Results.BadRequest(new { message = "Ngày kết thúc phải sau ngày bắt đầu." });
            if (!ProfileStatuses.Contains(request.Status))
                return Results.BadRequest(new { message = "Trạng thái hồ sơ không hợp lệ." });
            var profile = await db.InternProfiles.FindAsync(profileId);
            if (profile is null) return Results.NotFound();

            profile.Name = request.Name.Trim();
            profile.Email = request.Email.Trim();
            profile.School = request.School.Trim();
            profile.Major = request.Major.Trim();
            profile.StartDate = request.StartDate;
            profile.EndDate = request.EndDate;
            profile.Status = request.Status;
            await db.SaveChangesAsync();
            return Results.Ok(profile.ToResponse());
        })
        .WithName("UpdateHrProfile");

        group.MapPatch("/applications/{applicationId:int}", async (
            int applicationId, DecideStatusRequest request, CareerDbContext db) =>
        {
            if (!DecisionStatuses.Contains(request.Status))
                return Results.BadRequest(new { message = "Trạng thái đơn phải là Đã duyệt hoặc Từ chối." });
            var application = await db.InternApplications
                .Include(item => item.Profile)
                .FirstOrDefaultAsync(item => item.Id == applicationId);
            if (application is null) return Results.NotFound();

            application.Status = request.Status;
            if (request.Status == "Từ chối" && string.IsNullOrWhiteSpace(request.Note))
                return Results.BadRequest(new { message = "Nhập lý do từ chối." });
            PortalWorkflow.Notify(db, application.ProfileId, "Kết quả xét duyệt hồ sơ", request.Status + ": " + request.Note, true);
            if (application.Profile is not null)
                application.Profile.Status = request.Status == "Đã duyệt" ? "Đang thực tập" : "Từ chối";
            db.InternReviewHistories.Add(new InternReviewHistory
            {
                ProfileId = application.ProfileId,
                TargetType = "Đơn ứng tuyển",
                TargetId = application.Id,
                TargetLabel = "Đơn xin thực tập",
                Status = request.Status,
                Note = request.Note?.Trim()
            });
            await db.SaveChangesAsync();
            return Results.Ok(application.ToResponse());
        })
        .WithName("DecideInternApplication");

        group.MapPatch("/documents/{documentId:int}", async (
            int documentId, DecideStatusRequest request, CareerDbContext db) =>
        {
            if (!DecisionStatuses.Contains(request.Status))
                return Results.BadRequest(new { message = "Trạng thái tài liệu phải là Đã duyệt hoặc Từ chối." });
            var item = await db.InternDocuments.FindAsync(documentId);
            if (item is null) return Results.NotFound();
            item.Status = request.Status;
            item.Note = request.Note?.Trim();
            db.InternReviewHistories.Add(new InternReviewHistory
            {
                ProfileId = item.ProfileId,
                TargetType = "Tài liệu",
                TargetId = item.Id,
                TargetLabel = item.Type,
                Status = request.Status,
                Note = request.Note?.Trim()
            });
            await db.SaveChangesAsync();
            return Results.Ok(item.ToResponse());
        })
        .WithName("DecideInternDocument");

        group.MapPost("/profiles/{profileId:int}/contract", async (
            int profileId, HttpRequest request, CareerDbContext db) =>
        {
            var profile = await db.InternProfiles.FindAsync(profileId);
            if (profile is null) return Results.NotFound();
            var fileName = Path.GetFileName(request.Query["fileName"].ToString());
            var extension = Path.GetExtension(fileName);
            if (!AllowedExtensions.Contains(extension))
                return Results.BadRequest(new { message = "Chỉ hỗ trợ tệp PDF, DOC hoặc DOCX." });
            if (request.ContentLength is > MaxUploadBytes)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

            await using var buffer = new MemoryStream();
            await request.Body.CopyToAsync(buffer, request.HttpContext.RequestAborted);
            if (buffer.Length == 0 || buffer.Length > MaxUploadBytes)
                return Results.BadRequest(new { message = "Tệp rỗng hoặc vượt quá giới hạn 10 MB." });

            var oldContracts = await db.InternDocuments
                .Where(document => document.ProfileId == profileId && document.Type == "Hợp đồng thực tập")
                .ToListAsync();
            db.InternDocuments.RemoveRange(oldContracts);
            var contract = new InternDocument
            {
                ProfileId = profileId,
                Type = "Hợp đồng thực tập",
                FileName = fileName,
                ContentType = request.ContentType?.Split(';', 2)[0].Trim() ?? "application/octet-stream",
                Content = buffer.ToArray(),
                Status = "Chờ xác nhận",
                Note = "Hợp đồng do bộ phận nhân sự tải lên."
            };
            db.InternDocuments.Add(contract);
            await db.SaveChangesAsync();
            return Results.Created($"/api/documents/{contract.Id}/file", contract.ToResponse());
        })
        .WithName("UploadHrContract");

        routes.MapGet("/api/documents/{documentId:int}/file", async (int documentId, CareerDbContext db) =>
        {
            var item = await db.InternDocuments.AsNoTracking()
                .Where(document => document.Id == documentId)
                .Select(document => new { document.Content, document.ContentType, document.FileName })
                .FirstOrDefaultAsync();
            return item is null
                ? Results.NotFound()
                : Results.File(item.Content, item.ContentType, item.FileName, enableRangeProcessing: true);
        })
        .WithName("GetDocumentFile");

        return group;
    }
}
