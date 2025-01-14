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

        public CurrencyController(AppDbContext context)
        {
            _context = context;
        }
        // GET: Currency
        public async Task<IActionResult> Index()
        {
            return View(await _context.ExchangeRates.ToListAsync());
        }

        // GET: Currency/Details/5
        public async Task<IActionResult> Details(int? id)
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
