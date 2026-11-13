using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Controllers
{
    /// <summary>
    /// Manages the Choreography and Rehearsal Video Vault, scoping
    /// practice videos to specific classes for at-home rehearsal.
    /// </summary>
    [Authorize]
    public class VideoVaultController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public VideoVaultController(
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /VideoVault?danceClassId=5
        // Defaults to the first class if none is selected so videos
        // from different classes never bleed together.
        [HttpGet]
        public async Task<IActionResult> Index(int? danceClassId)
        {
            List<DanceClass> classes = await _context.DanceClasses
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            IQueryable<RehearsalVideo> query = _context.RehearsalVideos
                .Include(v => v.DanceClass)
                .OrderByDescending(v => v.UpLoadedAt);

            if (danceClassId.HasValue)
            {
                query = query.Where(
                    v => v.DanceClassId == danceClassId.Value);
            }

            ViewBag.ClassFilter = new SelectList(
                classes,
                "Id",
                "ClassName",
                danceClassId);

            List<RehearsalVideo> videos = await query.ToListAsync();
            return View(videos);
        }

        // GET: /VideoVault/Create
        // Renders the upload form for Directors and Instructors.
        [HttpGet]
        [Authorize(Roles = "Director,Instructor")]
        public async Task<IActionResult> Create()
        {
            await PopulateClassesDropdownAsync(null);
            RehearsalVideoUploadViewModel model =
                new RehearsalVideoUploadViewModel();
            return View(model);
        }

        // POST: /VideoVault/Create
        // Saves a formatted video file or link scoped to a class.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Director,Instructor")]
        [RequestSizeLimit(524_288_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
        public async Task<IActionResult> Create(
            RehearsalVideoUploadViewModel model)
        {
            bool hasFile = model.VideoFile != null
                && model.VideoFile.Length > 0;
            bool hasUrl = !string.IsNullOrWhiteSpace(
                model.ExternalOrLocalUrl);

            if (!hasFile && !hasUrl)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please upload a video file or provide a link.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateClassesDropdownAsync(model.DanceClassId);
                return View(model);
            }

            string savedVideoPath = string.Empty;
            bool isLocal = false;

            if (hasFile && model.VideoFile != null)
            {
                string uploadsFolder = Path.Combine(
                    _env.WebRootPath,
                    "videos",
                    "rehearsals");

                Directory.CreateDirectory(uploadsFolder);

                string safeFileName = Guid.NewGuid().ToString()
                    + Path.GetExtension(model.VideoFile.FileName);

                string fullPath = Path.Combine(
                    uploadsFolder,
                    safeFileName);

                using (FileStream stream = new FileStream(
                    fullPath,
                    FileMode.Create))
                {
                    await model.VideoFile.CopyToAsync(stream);
                }

                savedVideoPath = "/videos/rehearsals/" + safeFileName;
                isLocal = true;
            }
            else if (hasUrl && model.ExternalOrLocalUrl != null)
            {
                savedVideoPath = model.ExternalOrLocalUrl.Trim();
                isLocal = savedVideoPath.StartsWith("/videos/");
            }

            RehearsalVideo newVideo = new RehearsalVideo
            {
                Title = model.Title,
                Description = model.Description,
                DanceClassId = model.DanceClassId,
                VideoUrl = savedVideoPath,
                IsLocalFile = isLocal,
                UpLoadedAt = DateTime.UtcNow
            };

            _context.RehearsalVideos.Add(newVideo);
            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { danceClassId = model.DanceClassId });
        }

        // Helper to populate the class selection dropdown.
        private async Task PopulateClassesDropdownAsync(
            int? selectedClassId)
        {
            List<DanceClass> classes = await _context.DanceClasses
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            ViewBag.DanceClasses = new SelectList(
                classes,
                "Id",
                "ClassName",
                selectedClassId);
        }
    }
}