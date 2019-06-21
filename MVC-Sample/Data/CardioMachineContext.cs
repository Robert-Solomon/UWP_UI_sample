using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MVC_Sample.Models
{
    public class CardioMachineContext : DbContext
    {
        public CardioMachineContext (DbContextOptions<CardioMachineContext> options)
            : base(options)
        {
        }

        public DbSet<MVC_Sample.Models.CardioMachine> CardioMachine { get; set; }
    }
}
