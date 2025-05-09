using Microsoft.EntityFrameworkCore;
using SIGHR.Models;

namespace SIGHR.Data
{
    public class SIGHRContext : DbContext
    {
        public SIGHRContext(DbContextOptions<SIGHRContext> options) : base(options)
        {
        }

        // DbSets para suas entidades
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<Requisicao> Requisicoes { get; set; }
        public DbSet<Utilizadores> Utilizadores { get; set; }  // Adicionando DbSet para Utilizadores

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurando chave composta para 'Requisicao'
            modelBuilder.Entity<Requisicao>()
                .HasKey(r => new { r.MaterialId, r.EncomendaId });  // Configurar chave composta

            // Outras configurações, se necessário
            base.OnModelCreating(modelBuilder);
        }
    }
}
