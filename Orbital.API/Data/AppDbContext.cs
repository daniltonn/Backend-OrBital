using Microsoft.EntityFrameworkCore;
using Orbital.API.Models;

namespace Orbital.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // =========================
        // DBSETS
        // =========================
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Jerarquia> Jerarquias { get; set; }
        public DbSet<Galaxia> Galaxias { get; set; }
        public DbSet<TipoAtmosfera> TiposAtmosfera { get; set; }
        public DbSet<Planeta> Planetas { get; set; }
        public DbSet<CoordenadasPlaneta> CoordenadasPlanetas { get; set; }
        public DbSet<PlanetaEstado> PlanetaEstados { get; set; }
        public DbSet<PlanetaValoracion> PlanetaValoraciones { get; set; }
        public DbSet<MiembroEquipo> MiembrosEquipo { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<EstadoMision> EstadosMision { get; set; }
        public DbSet<Recurso> Recursos { get; set; }
        public DbSet<RecursoPlaneta> RecursosPlaneta { get; set; }
        public DbSet<Mision> Misiones { get; set; }
        public DbSet<Auditoria> Auditorias { get; set; }
        public DbSet<MercadoPlaneta> MercadoPlanetas { get; set; }
        public DbSet<AmenazaDeteccion> AmenazasDeteccion { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<HistoricoCicloPlanetario> HistoricosCiclo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // TABLE MAPPING
            // =========================
            modelBuilder.Entity<Usuario>().ToTable("usuario");
            modelBuilder.Entity<Rol>().ToTable("rol");
            modelBuilder.Entity<Jerarquia>().ToTable("jerarquia");
            modelBuilder.Entity<Galaxia>().ToTable("galaxia");
            modelBuilder.Entity<TipoAtmosfera>().ToTable("tipo_atmosfera");
            modelBuilder.Entity<Planeta>().ToTable("planeta");
            modelBuilder.Entity<CoordenadasPlaneta>().ToTable("coordenada_planeta");
            modelBuilder.Entity<PlanetaEstado>().ToTable("estado_planeta");
            modelBuilder.Entity<PlanetaValoracion>().ToTable("planeta_valoracion");
            modelBuilder.Entity<MiembroEquipo>().ToTable("miembro_equipo");
            modelBuilder.Entity<Equipo>().ToTable("equipo");
            modelBuilder.Entity<EstadoMision>().ToTable("estado_mision");
            modelBuilder.Entity<Recurso>().ToTable("recurso");

            // =========================
            // PRIMARY KEYS
            // =========================
            modelBuilder.Entity<Usuario>()
                .HasKey(x => x.Id_Usuario);

            modelBuilder.Entity<Rol>()
                .HasKey(x => x.Id_Rol);

            modelBuilder.Entity<Jerarquia>()
                .HasKey(x => x.Id_Jerarquia);

            modelBuilder.Entity<Galaxia>()
                .HasKey(x => x.Id_Galaxia);
            
            modelBuilder.Entity<TipoAtmosfera>()
                .HasKey(x => x.Id_Atmosfera);

            modelBuilder.Entity<Planeta>()
                .HasKey(x => x.Id_Planeta);

            modelBuilder.Entity<CoordenadasPlaneta>()
                .HasKey(x => x.Id_Coordenada);
            
            modelBuilder.Entity<PlanetaEstado>()
                .HasKey(x => x.Id_Estado);

            modelBuilder.Entity<PlanetaValoracion>()
                .HasKey(x => x.Id_Valoracion);

            modelBuilder.Entity<Recurso>()
                .HasKey(x => x.Id_Recurso);

            
            modelBuilder.Entity<RecursoPlaneta>()
                .HasKey(x => x.Id_Recurso_Planeta);
                
            modelBuilder.Entity<Mision>()
                .HasKey(x => x.Id_Mision);

            modelBuilder.Entity<Auditoria>()
                .HasKey(x => x.Id_Auditoria);

            modelBuilder.Entity<MercadoPlaneta>()
                .HasKey(x => x.Id_Publicacion);

            modelBuilder.Entity<AmenazaDeteccion>()
                .HasKey(x => x.Id_Amenaza);

            modelBuilder.Entity<Transaccion>()
                .HasKey(x => x.Id_Transaccion);

            // =========================
            // RELACIONES - Usuario
            // =========================
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.Id_Rol)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Jerarquia)
                .WithMany()
                .HasForeignKey(u => u.Id_Jerarquia)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // RELACIONES - Planeta
            // =========================
            modelBuilder.Entity<Planeta>()
                .HasOne(p => p.Estado)
                .WithMany()
                .HasForeignKey(p => p.Id_Estado)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Planeta>()
                .HasOne(p => p.AtmosferaNav)
                .WithMany()
                .HasForeignKey(p => p.Id_Atmosfera)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CoordenadasPlaneta>()
                .HasOne(c => c.Planeta)
                .WithOne(p => p.Coordenadas)
                .HasForeignKey<CoordenadasPlaneta>(c => c.Id_Planeta)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecursoPlaneta>()
                .HasOne(rp => rp.Planeta)
                .WithMany(p => p.Recursos)
                .HasForeignKey(rp => rp.Id_Planeta)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RecursoPlaneta>()
                .HasOne(rp => rp.Recurso)
                .WithMany()
                .HasForeignKey(rp => rp.Id_Recurso)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mision>()
                .HasOne<Planeta>()
                .WithMany(p => p.Misiones)
                .HasForeignKey(m => m.Id_Planeta)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // RELACIONES - PlanetaValoracion
            // =========================
            modelBuilder.Entity<PlanetaValoracion>()
                .HasOne(pv => pv.Planeta)
                .WithMany()
                .HasForeignKey(pv => pv.Id_Planeta)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlanetaValoracion>()
                .HasOne(pv => pv.Analista)
                .WithMany()
                .HasForeignKey(pv => pv.Id_Analista)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlanetaValoracion>()
                .HasOne(pv => pv.AprobadoPor)
                .WithMany()
                .HasForeignKey(pv => pv.Aprobado_Por)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MiembroEquipo>()
                .HasOne(m => m.Usuario)
                .WithMany()
                .HasForeignKey(m => m.Id_Usuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Equipo>()
                .HasKey(e => e.Id_Equipo);

            modelBuilder.Entity<EstadoMision>()
                .HasKey(e => e.Id_Estado_Mision);
            // =========================
            // RELACIONES - Planeta → Galaxia
            // =========================
            modelBuilder.Entity<Planeta>()
                .HasOne(p => p.GalaxiaNav)
                .WithMany()
                .HasForeignKey(p => p.Id_Galaxia)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // RELACIONES - MercadoPlaneta
            // =========================
            modelBuilder.Entity<MercadoPlaneta>()
                .HasOne(m => m.Planeta)
                .WithMany()
                .HasForeignKey(m => m.Id_Planeta)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MercadoPlaneta>()
                .HasOne(m => m.Valoracion)
                .WithMany()
                .HasForeignKey(m => m.Id_Valoracion)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // RELACIONES - Cliente
            // =========================
            modelBuilder.Entity<Cliente>()
                .HasKey(c => c.Id_Cliente);

            modelBuilder.Entity<Cliente>()
                .ToTable("cliente");

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Correo)
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .Property(c => c.Correo)
                .HasConversion(v => v.Trim().ToLower(), v => v);

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.GalaxiaOrigen)
                .WithMany()
                .HasForeignKey(c => c.Id_Galaxia_Origen)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // RELACIONES - HistoricoCicloPlanetario
            // =========================
            modelBuilder.Entity<HistoricoCicloPlanetario>()
                .HasKey(h => h.Id_Historico);

            modelBuilder.Entity<HistoricoCicloPlanetario>()
                .ToTable("historico_ciclo_planetario");

            modelBuilder.Entity<HistoricoCicloPlanetario>()
                .HasOne(h => h.Planeta)
                .WithMany()
                .HasForeignKey(h => h.Id_Planeta)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoricoCicloPlanetario>()
                .HasOne(h => h.Transaccion)
                .WithMany()
                .HasForeignKey(h => h.Id_Transaccion)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoricoCicloPlanetario>()
                .HasOne(h => h.ClienteAnterior)
                .WithMany()
                .HasForeignKey(h => h.Id_Cliente_Anterior)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoricoCicloPlanetario>()
                .HasOne(h => h.ClienteNuevo)
                .WithMany()
                .HasForeignKey(h => h.Id_Cliente_Nuevo)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // CONVERSIONES Y RESTRICCIONES
            // =========================
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Correo)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .Property(u => u.Correo)
                .HasConversion(
                    v => v.Trim().ToLower(),
                    v => v
                );

            modelBuilder.Entity<Planeta>()
                .Property(p => p.Nivel_Tecnologico)
                .HasConversion<int>();
        }
    }
}
