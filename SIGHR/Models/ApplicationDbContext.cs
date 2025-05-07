using Microsoft.EntityFrameworkCore;
namespace SIGHR.Models
{



    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Utilizadores> Utilizadores { get; set; }
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<Falta> Faltas { get; set; }
        public DbSet<Encomenda> Encomendas { get; set; }
        public DbSet<Material> Materiais { get; set; }
        public DbSet<Requisicao> Requisicoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Chave composta da Requisição
            modelBuilder.Entity<Requisicao>()
                .HasKey(r => new { r.MaterialId, r.EncomendaId });

            // Relações
            modelBuilder.Entity<Requisicao>()
                .HasOne(r => r.Material)
                .WithMany(m => m.Requisicoes)
                .HasForeignKey(r => r.MaterialId);

            modelBuilder.Entity<Requisicao>()
                .HasOne(r => r.Encomenda)
                .WithMany(e => e.Requisicoes)
                .HasForeignKey(r => r.EncomendaId);

            modelBuilder.Entity<Horario>()
                .HasOne(h => h.Utilizador)
                .WithMany(u => u.Horarios)
                .HasForeignKey(h => h.UtilizadorId);

            modelBuilder.Entity<Falta>()
                .HasOne(f => f.Utilizador)
                .WithMany(u => u.Faltas)
                .HasForeignKey(f => f.UtilizadorId);

            modelBuilder.Entity<Encomenda>()
                .HasOne(e => e.Utilizador)
                .WithMany(u => u.Encomendas)
                .HasForeignKey(e => e.UtilizadorId);

            // Unique Constraints se quiseres manter os que estavam no SQL (opcional):
            modelBuilder.Entity<Horario>()
                .HasIndex(h => h.UtilizadorId)
                .IsUnique();

            modelBuilder.Entity<Falta>()
                .HasIndex(f => f.UtilizadorId)
                .IsUnique();

            modelBuilder.Entity<Encomenda>()
                .HasIndex(e => e.UtilizadorId)
                .IsUnique();
        }
    }
}