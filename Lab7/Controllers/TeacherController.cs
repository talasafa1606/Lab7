using Lab7.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lab7.Controllers;

[ApiController]
[Route("api/teachers")]
public class TeacherController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly BlobStorageService _blobStorageService;

    public TeacherController(ApplicationDbContext context, BlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    [HttpPost("{teacherId}/uploadProfilePicture")]
    [Authorize(Policy = "TeacherOnly")]
    public async Task<IActionResult> UploadProfilePicture(int teacherId, IFormFile file)
    {
        var teacher = await _context.Teachers.FindAsync(teacherId);
        if (teacher == null)
            return NotFound("Teacher not found");

        using (var stream = file.OpenReadStream())
        {
            string url = await _blobStorageService.UploadProfilePictureAsync(teacherId.ToString(), stream, file.ContentType);
            teacher.ProfilePictureUrl = url;
            await _context.SaveChangesAsync();
        }

        return Ok(new { Message = "Profile picture uploaded successfully", ProfilePictureUrl = teacher.ProfilePictureUrl });
    }

    [HttpGet("{teacherId}/profilePicture")]
    [Authorize(Policy = "TeacherOnly")]
    public async Task<IActionResult> GetProfilePicture(int teacherId)
    {
        var teacher = await _context.Teachers.FindAsync(teacherId);
        if (teacher == null || string.IsNullOrEmpty(teacher.ProfilePictureUrl))
            return NotFound("Profile picture not found");

        Stream imageStream = await _blobStorageService.DownloadProfilePictureAsync(teacherId.ToString());
        return File(imageStream, "image/jpeg");
    }
}