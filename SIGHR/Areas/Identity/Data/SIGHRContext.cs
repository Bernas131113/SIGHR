// Data/SIGHRContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIGHR.Models; // Namespace das suas entidades (Horario, Material, etc.)

namespace SIGHR.Areas.Identity.Data
{
    /// <summary>
    /// O contexto da base de dados para a aplicação.
    /// Funciona como a ponte principal entre as suas classes C# (entidades) e a base de dados.
    /// Herda de IdentityDbContext para incluir toda a funcionalidade do ASP.NET Core Identity.
    /// </summary>
    public class SIGHRContext : IdentityDbContext<SIGHRUser> // Usa a classe de utilizador personalizada SIGHRUser.
    {
        public SIGHRContext(DbContextOptions<SIGHRContext> options)
            : base(options)
        {
        }

        //
        // Bloco: Mapeamento de Entidades (DbSets)
        // Cada DbSet representa uma tabela na base de dados que o Entity Framework irá gerir.
        // O DbSet<SIGHRUser> já é gerido pelo IdentityDbContext.
        //
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<Falta> Faltas { get; set; }
        public DbSet<Encomenda> Encomendas { get; set; }
        public DbSet<Material> Materiais { get; set; }
        public DbSet<Requisicao> Requisicoes { get; set; }
        // Adicione aqui quaisquer outros DbSets de entidades que venha a criar.

        /// <summary>
        /// Configura o modelo de dados usando a Fluent API do Entity Framework.
        /// Este método é chamado uma vez durante a inicialização para construir o modelo e as suas relações.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ESSENCIAL: Chama a implementação base para configurar o esquema do Identity (tabelas de utilizadores, funções, etc.).
            base.OnModelCreating(modelBuilder);

            //
            // Bloco: Configuração da Entidade 'Requisicao' (Tabela de Junção)
            //

            // Define uma chave primária composta para a tabela Requisicao,
            // formada pela combinação do ID do Material e do ID da Encomenda.
            modelBuilder.Entity<Requisicao>()
                .HasKey(r => new { r.MaterialId, r.EncomendaId });

            // Define a relação "Muitos para Um" entre Requisicao e Material.
            modelBuilder.Entity<Requisicao>()
                .HasOne(r => r.Material)
                .WithMany(m => m.Requisicoes)
                .HasForeignKey(r => r.MaterialId)
                .OnDelete(DeleteBehavior.Restrict); // Evita a exclusão em cascata: um Material não pode ser apagado se estiver associado a uma Requisição.

            // Define a relação "Muitos para Um" entre Requisicao e Encomenda.
            modelBuilder.Entity<Requisicao>()
                .HasOne(r => r.Encomenda)
                .WithMany(e => e.Requisicoes)
                .HasForeignKey(r => r.EncomendaId)
                .OnDelete(DeleteBehavior.Restrict); // Evita a exclusão em cascata: uma Encomenda não pode ser apagada se tiver Requisições.


            //
            // Bloco: Configuração das Relações com o Utilizador (SIGHRUser)
            //

            // Relação entre Horario e SIGHRUser.
            modelBuilder.Entity<Horario>(entity =>
            {
                entity.HasOne(h => h.User)           // Propriedade de navegação em Horario.
                      .WithMany(u => u.Horarios)     // Coleção de Horarios em SIGHRUser.
                      .HasForeignKey(h => h.UtilizadorId) // Chave estrangeira em Horario.
                      .IsRequired();                 // Um Horario TEM de pertencer a um Utilizador.
                entity.HasIndex(h => h.UtilizadorId); // Adiciona um índice à coluna para melhorar o desempenho das consultas.
            });

            // Relação entre Falta e SIGHRUser.
            modelBuilder.Entity<Falta>(entity =>
            {
                entity.HasOne(f => f.User)
                      .WithMany(u => u.Faltas)
                      .HasForeignKey(f => f.UtilizadorId)
                      .IsRequired();
                entity.HasIndex(f => f.UtilizadorId);
            });

            // Relação entre Encomenda e SIGHRUser.
            modelBuilder.Entity<Encomenda>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Encomendas)
                      .HasForeignKey(e => e.UtilizadorId)
                      .IsRequired();
                entity.HasIndex(e => e.UtilizadorId);
            });
        }
    }
}