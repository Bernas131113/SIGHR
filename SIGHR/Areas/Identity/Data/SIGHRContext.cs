// Data/SIGHRContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIGHR.Models; // Para suas entidades customizadas (Horario, Material, etc.)

namespace SIGHR.Areas.Identity.Data
{
    public class SIGHRContext : IdentityDbContext<SIGHRUser> // Herda de IdentityDbContext com seu usuário customizado
    {
        public SIGHRContext(DbContextOptions<SIGHRContext> options)
            : base(options)
        {
        }

        // DbSets para suas entidades customizadas
        // Não precisa de DbSet<SIGHRUser>, IdentityDbContext já lida com isso.
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<Falta> Faltas { get; set; }
        public DbSet<Encomenda> Encomendas { get; set; }
        public DbSet<Material> Materiais { get; set; }
        public DbSet<Requisicao> Requisicoes { get; set; }
        // Adicione quaisquer outros DbSets de entidades que você tenha

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // ESSENCIAL para configurar o esquema do Identity

            // Configurações da Fluent API para suas entidades customizadas:

            // Chave composta para Requisicao
            modelBuilder.Entity<Requisicao>()
                .HasKey(r => new { r.MaterialId, r.EncomendaId });

            // Relacionamentos para Requisicao
            modelBuilder.Entity<Requisicao>()
                .HasOne(r => r.Material)
                .WithMany(m => m.Requisicoes)
                .HasForeignKey(r => r.MaterialId)
                .OnDelete(DeleteBehavior.Restrict); // Exemplo: Evitar deleção em cascata se houver requisições

            modelBuilder.Entity<Requisicao>()
                .HasOne(r => r.Encomenda)
                .WithMany(e => e.Requisicoes)
                .HasForeignKey(r => r.EncomendaId)
                .OnDelete(DeleteBehavior.Restrict); // Exemplo

            // Relacionamentos das suas entidades com SIGHRUser
            modelBuilder.Entity<Horario>(entity =>
            {
                entity.HasOne(h => h.User) // Propriedade de navegação em Horario
                      .WithMany(u => u.Horarios)   // Coleção em SIGHRUser
                      .HasForeignKey(h => h.UtilizadorId) // FK em Horario (deve ser string)
                      .IsRequired(); // Um Horario DEVE ter um Utilizador
                entity.HasIndex(h => h.UtilizadorId); // Índice para performance (não único)
            });

            modelBuilder.Entity<Falta>(entity =>
            {
                entity.HasOne(f => f.User)
                      .WithMany(u => u.Faltas)
                      .HasForeignKey(f => f.UtilizadorId)
                      .IsRequired();
                entity.HasIndex(f => f.UtilizadorId); // Não único
            });

            modelBuilder.Entity<Encomenda>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Encomendas)
                      .HasForeignKey(e => e.UtilizadorId)
                      .IsRequired();
                entity.HasIndex(e => e.UtilizadorId); // Não único
            });

            // Você pode adicionar outras configurações aqui:
            // - Nomes de tabelas/colunas customizados
            // - Tipos de dados específicos (ex: para decimais com precisão)
            // - Índices adicionais
            // - Dados iniciais (seeding) via modelBuilder.Entity<T>().HasData(...)

            // Exemplo para Material, se tiver um campo de preço decimal:
            // modelBuilder.Entity<Material>()
            //     .Property(m => m.Preco)
            //     .HasColumnType("decimal(18,2)");

            // Ajuste os Unique Constraints que você tinha:
            // Se um Horario deve ser único para um Utilizador numa Data específica:
            // modelBuilder.Entity<Horario>()
            //     .HasIndex(h => new { h.UtilizadorId, h.Data })
            //     .IsUnique();
            // Faça o mesmo para Falta se aplicável. Encomenda geralmente não precisa disso.
        }
    }
}