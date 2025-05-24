using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webUygulama.Data;
using webUygulama.Models;
using System;
using System.Threading.Tasks;

namespace webUygulama.Controllers
{
    public class AnnouncementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Ana sayfa için aktif duyuruları getiren metod
        public async Task<IActionResult> GetActiveAnnouncements()
        {
            var now = DateTime.Now;
            var activeAnnouncements = await _context.Announcements
                .Where(a => a.IsActive && 
                          (!a.EndDate.HasValue || a.EndDate.Value > now))
                .OrderByDescending(a => a.IsImportant)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(activeAnnouncements);
        }

        // Admin paneli için tüm duyuruları listeleyen action
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var activeAnnouncements = await _context.Announcements
                .Where(a => a.IsActive && 
                          (!a.EndDate.HasValue || a.EndDate.Value > now))
                .OrderByDescending(a => a.IsImportant)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(activeAnnouncements);
        }

        // Yeni duyuru oluşturma sayfası
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            var announcement = new Announcement
            {
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            return View(announcement);
        }

        // Yeni duyuru oluşturma post action'ı
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Announcement announcement)
        {
            if (ModelState.IsValid)
            {
                announcement.CreatedAt = DateTime.Now;
                _context.Add(announcement);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(announcement);
        }

        // Duyuru düzenleme sayfası
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }
            return View(announcement);
        }

        // Duyuru düzenleme post action'ı
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Announcement announcement)
        {
            if (id != announcement.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    announcement.UpdatedAt = DateTime.Now;
                    _context.Update(announcement);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(announcement);
        }

        // GET: Announcement/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(m => m.Id == id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // POST: Announcement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Duyuru başarıyla silindi.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.Id == id);
        }
    }
} 