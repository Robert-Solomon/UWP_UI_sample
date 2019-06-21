using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC_Sample.Models;

namespace MVC_Sample.Controllers
{
    public class CardioMachinesController : Controller
    {
        private readonly CardioMachineContext _context;

        public CardioMachinesController(CardioMachineContext context)
        {
            _context = context;
        }

        // GET: CardioMachines
        public async Task<IActionResult> Index()
        {
            return View(await _context.CardioMachine.ToListAsync());
        }

        // GET: CardioMachines/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cardioMachine = await _context.CardioMachine
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cardioMachine == null)
            {
                return NotFound();
            }

            return View(cardioMachine);
        }

        // GET: CardioMachines/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CardioMachines/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,MachineType,MachineModel,MachineNumber,capabilities1,capabilities2,capabilities3,capabilities4,capabilities5")] CardioMachine cardioMachine)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cardioMachine);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cardioMachine);
        }

        // GET: CardioMachines/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cardioMachine = await _context.CardioMachine.FindAsync(id);
            if (cardioMachine == null)
            {
                return NotFound();
            }
            return View(cardioMachine);
        }

        // POST: CardioMachines/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MachineType,MachineModel,MachineNumber,capabilities1,capabilities2,capabilities3,capabilities4,capabilities5")] CardioMachine cardioMachine)
        {
            if (id != cardioMachine.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cardioMachine);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CardioMachineExists(cardioMachine.Id))
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
            return View(cardioMachine);
        }

        // GET: CardioMachines/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cardioMachine = await _context.CardioMachine
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cardioMachine == null)
            {
                return NotFound();
            }

            return View(cardioMachine);
        }

        // POST: CardioMachines/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cardioMachine = await _context.CardioMachine.FindAsync(id);
            _context.CardioMachine.Remove(cardioMachine);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CardioMachineExists(int id)
        {
            return _context.CardioMachine.Any(e => e.Id == id);
        }
    }
}
