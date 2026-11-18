using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Privatni_casovi.Data;
using Privatni_casovi.Identity;
using Privatni_casovi.ModelBinding;
using Privatni_casovi.Models;
using Privatni_casovi.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<Korisnik, IdentityRole>(options =>
{
    // A demo password policy: long enough to be a password, with no complexity rules, so the
    // credentials handed in with the project stay short enough to type.
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    // There is no mail server and no confirmation flow — a korisnik signs in the moment they
    // register. Whether they may sign in at all is Aktivan, enforced in KorisnikSignInManager.
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // The email is the username, so it has to be unique in its own right.
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager<KorisnikSignInManager>();

// Everything requires a signed-in korisnik unless it says otherwise: the landing page,
// registration and prijava are the only [AllowAnonymous] endpoints in the application.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Data access, all of it. A controller composes no query of its own: business logic sits in
// the controllers, LINQ sits behind these interfaces, and the Aktivan filter of ADR-0001 sits
// with the LINQ so no call site can forget it.
//
// IRepository<T> is deliberately not registered as an open generic. It is the shared base of
// the interfaces below and is injected through them; registering it on its own would hand a
// caller who asked for IRepository<Oglas> the unfiltered Repository<Oglas> instead of
// OglasRepository, and deactivated oglasi would come back out of GetAllAsync.
builder.Services.AddScoped<IOglasRepository, OglasRepository>();
builder.Services.AddScoped<IOcenaRepository, OcenaRepository>();
builder.Services.AddScoped<IKorisnikRepository, KorisnikRepository>();
builder.Services.AddScoped<IPredmetRepository, PredmetRepository>();
builder.Services.AddScoped<IOmiljeniRepository, OmiljeniRepository>();

builder.Services.AddControllersWithViews(options =>
{
    // Inserted at the front so it wins over the built-in simple-type binder: a cena posted by
    // an <input type="number"> is dot-decimal by definition, and must not be read as Serbian
    // thousands grouping on a machine whose culture says otherwise. See DecimalModelBinder.
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());

    // A value that cannot become its property at all — Lokacija=77, Uloga=Administrator —
    // fails here, before [Required] or [Range] runs, so the ViewModel never gets to word it.
    // Without this the framework answers in English on a form that is Serbian throughout.
    PorukeVezivanja.NaSrpskom(options);
});

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

// Startup brings the database up to date and then fills it, in that order and only that
// order: migrating first is what lets a fresh clone — or a dropped database — boot straight
// into a populated app with no manual `dotnet ef database update`, and UlogeSeed has to run
// before DbInitializer because the seeded korisnici are put into the roles it creates.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    await UlogeSeed.EnsureAsync(scope.ServiceProvider);
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

app.Run();
