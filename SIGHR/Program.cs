using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Data;
using SIGHR.Models;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("SIGHRContextConnection") ?? throw new InvalidOperationException("Connection string 'SIGHRContextConnection' not found.");

builder.Services.AddDbContext<SIGHRContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<SIGHRUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<SIGHRContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SIGHRContext>();

    // Verifica se o utilizador já existe
    if (!context.Utilizadores.Any(u => u.Username == "bernardo.alves"))
    {
        var novoUtilizador = new Utilizadores
        {
            Nome = "Bernardo Alves",
            Username = "bernardo.alves",
            PIN = 1311,
            Tipo = "Admin" // ou "Admin", conforme o teu sistema
        };

        context.Utilizadores.Add(novoUtilizador);
        context.SaveChanges();
    }
}

app.Run();
