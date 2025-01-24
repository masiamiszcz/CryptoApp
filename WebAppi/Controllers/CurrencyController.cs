using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebAppi.Models;

namespace WebAppi.Controllers
{
    public class CurrencyController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CurrencyController> _logger;
        private readonly CentralizedLoggerClient _centralizedLogger;

        public CurrencyController(AppDbContext context, ILogger<CurrencyController> logger, CentralizedLoggerClient centralizedLogger)
        {
            _context = context;
            _logger = logger;
            _centralizedLogger = centralizedLogger;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
            // Grupowanie i wybieranie najnowszych rekordów dla NBP Rates (API 1 i 2)
            var nbpRates = await _context.ExchangeRates
                .Include(e => e.CurrencyNameNavigation)
                .Include(e => e.CurrencyNameNavigation2)
                .Where(e => e.ApiId == 1 || e.ApiId == 2) // Dane dla NBP
                .GroupBy(e => e.Id) // Grupowanie po Id2
                .Select(g => g.OrderByDescending(e => e.DateTime).FirstOrDefault()) // Najnowszy rekord w każdej grupie
                .ToListAsync();

            // Grupowanie i wybieranie najnowszych rekordów dla EBC Rates (API 3)
            var ebcRates = await _context.ExchangeRates
                .Include(e => e.CurrencyNameNavigation)
                .Include(e => e.CurrencyNameNavigation2)
                .Where(e => e.ApiId == 3) // Dane dla EBC
                .GroupBy(e => e.Id2) // Grupowanie po Id2
                .Select(g => g.OrderByDescending(e => e.DateTime).FirstOrDefault()) // Najnowszy rekord w każdej grupie
                .ToListAsync();

            // Grupowanie i wybieranie najnowszych rekordów dla Fixer Rates (API 4)
            var fixerRates = await _context.ExchangeRates
                .Include(e => e.CurrencyNameNavigation)
                .Include(e => e.CurrencyNameNavigation2)
                .Where(e => e.ApiId == 4) // Dane dla Fixer
                .GroupBy(e => e.Id2) // Grupowanie po Id2
                .Select(g => g.OrderByDescending(e => e.DateTime).FirstOrDefault()) // Najnowszy rekord w każdej grupie
                .ToListAsync();
            
            if (!nbpRates.Any() || !ebcRates.Any() || !fixerRates.Any())
            {
                // Wypisz błąd lub sprawdź dane w tabelach
            }

            // Przekazanie wszystkich danych do widoku poprzez ViewData
            ViewData["NbpRates"] = nbpRates;
            ViewData["EbcRates"] = ebcRates;
            ViewData["FixerRates"] = fixerRates;

            // Widok nie potrzebuje jednej listy, ale można zwrócić wszystkie if needed
            return View();
            }
            catch (Exception ex)
            {
                var message = $"Error occurred while fetching Currency data for Index: {ex.Message}";
                _logger.LogError(message);
                await _centralizedLogger.SendLog(LogLevel.Error, message);
                return StatusCode(500);
            }
        }

        // GET: Currency/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var exchangeRates = await _context.ExchangeRates
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (exchangeRates == null)
                {
                    return NotFound();
                }

                return View(exchangeRates);
            }
            catch (Exception ex)
            {
                var message = $"Error occurred while fetching details for Currency ID {id}: {ex.Message}";
                _logger.LogError(message);
                await _centralizedLogger.SendLog(LogLevel.Error, message);
                return StatusCode(500);
            }
        }

        // GET: Currency/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Currency/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Id2,ExchangeRate")] ExchangeRates exchangeRates)
        {
            if (ModelState.IsValid)
            {
                _context.Add(exchangeRates);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(exchangeRates);
        }

        // GET: Currency/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exchangeRates = await _context.ExchangeRates.FindAsync(id);
            if (exchangeRates == null)
            {
                return NotFound();
            }
            return View(exchangeRates);
        }

        // POST: Currency/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Id2,ExchangeRate")] ExchangeRates exchangeRates)
        {
            if (id != exchangeRates.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(exchangeRates);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExchangeRatesExists(exchangeRates.Id))
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
            return View(exchangeRates);
        }

        // GET: Currency/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exchangeRates = await _context.ExchangeRates
                .FirstOrDefaultAsync(m => m.Id == id);
            if (exchangeRates == null)
            {
                return NotFound();
            }

            return View(exchangeRates);
        }

        // POST: Currency/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exchangeRates = await _context.ExchangeRates.FindAsync(id);
            if (exchangeRates != null)
            {
                _context.ExchangeRates.Remove(exchangeRates);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ExchangeRatesExists(int id)
        {
            return _context.ExchangeRates.Any(e => e.Id == id);
        }
    }
}
