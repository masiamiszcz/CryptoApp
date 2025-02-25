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
    public class CryptoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CryptoController> _logger;
        private readonly CentralizedLoggerClient _centralizedLogger;

        public CryptoController(AppDbContext context, ILogger<CryptoController> logger, CentralizedLoggerClient centralizedLogger)
        {
            _context = context;
            _logger = logger;
            _centralizedLogger = centralizedLogger;
        }

        // GET: Crypto
        public async Task<IActionResult> Index()
        {
            // Pobieramy najnowszy rekord dla każdej nazwy kryptowaluty (CryptoName) z uwzględnieniem relacji
            var latestCryptos = await _context.Cryptos
                .Include(c => c.CryptoNameNavigation) // Dołącz tabelę CryptoNames
                .GroupBy(c => c.Crypto_Id) // Grupowanie po kluczu obcym (Crypto_Id)
                .Select(g => g.OrderByDescending(c => c.DateTime).FirstOrDefault()) // Najnowszy rekord w każdej grupie
                .ToListAsync();

            return View(latestCryptos);
        }

        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var cryptoRecords = await _context.Cryptos
                    .Include(c => c.CryptoNameNavigation)
                    .Where(c => c.Crypto_Id == id)
                    .OrderByDescending(c => c.DateTime)
                    .ToListAsync();

                if (!cryptoRecords.Any())
                {
                    return NotFound();
                }

                return View(cryptoRecords);
            }
            catch (Exception ex)
            {
                var message = $"Error fetching crypto details for ID {id}: {ex.Message}";
                _logger.LogError(message);
                await _centralizedLogger.SendLog(LogLevel.Error, message);
                return StatusCode(500);
            }
        }

        // GET: Crypto/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Crypto/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("High24,Low24,Symbol,CryptoName,CryptoPrice,PriceChange,DateTime")] CryptoModel cryptoModel)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cryptoModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cryptoModel);
        }

        // GET: Crypto/Edit/5
        public async Task<IActionResult> Edit(DateTime? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cryptoModel = await _context.Cryptos.FindAsync(id);
            if (cryptoModel == null)
            {
                return NotFound();
            }
            return View(cryptoModel);
        }

        // POST: Crypto/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DateTime id, [Bind("High24,Low24,Symbol,CryptoName,CryptoPrice,PriceChange,DateTime")] CryptoModel cryptoModel)
        {
            if (id != cryptoModel.DateTime)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cryptoModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CryptoModelExists(cryptoModel.DateTime))
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
            return View(cryptoModel);
        }

        // GET: Crypto/Delete/5
        public async Task<IActionResult> Delete(DateTime? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cryptoModel = await _context.Cryptos
                .FirstOrDefaultAsync(m => m.DateTime == id);
            if (cryptoModel == null)
            {
                return NotFound();
            }

            return View(cryptoModel);
        }

        // POST: Crypto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DateTime id)
        {
            var cryptoModel = await _context.Cryptos.FindAsync(id);
            if (cryptoModel != null)
            {
                _context.Cryptos.Remove(cryptoModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CryptoModelExists(DateTime id)
        {
            return _context.Cryptos.Any(e => e.DateTime == id);
        }
    }
}
