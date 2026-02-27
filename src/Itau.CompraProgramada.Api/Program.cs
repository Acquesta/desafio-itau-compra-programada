using Itau.CompraProgramada.Application.UseCases;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Domain.Services;
using Itau.CompraProgramada.Infrastructure.B3;
using Itau.CompraProgramada.Infrastructure.Data;
using Itau.CompraProgramada.Infrastructure.Mensageria;
using Itau.CompraProgramada.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar os Controllers e o Swagger
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsSeguro", policy =>
    {
        var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() 
            ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(corsOrigins) 
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => 
{
    c.SwaggerDoc("v1", new() { Title = "Itaú - Compra Programada API", Version = "v1" });
});

// 2. Configurar a Base de Dados (MySQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 3. Registar as Injeções de Dependência (A magia da Clean Architecture!)
// Repositórios
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<ICestaRepository, CestaRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Integrações Externas (Infra)
builder.Services.AddScoped<ICotacaoB3Provider, CotacaoB3Provider>();
// O Publisher do Kafka é adicionado como Singleton por questões de performance do Producer
builder.Services.AddSingleton<IEventoIRPublisher, EventoIRPublisher>(); 

// Serviços de Domínio (puros, sem estado)
builder.Services.AddSingleton<CalculadoraLoteFracionarioService>();
builder.Services.AddSingleton<DistribuicaoProporcionalService>();

// Casos de Uso (Application)
builder.Services.AddScoped<IClienteUseCase, ClienteUseCase>();
builder.Services.AddScoped<IMotorCompraProgramadaUseCase, MotorCompraProgramadaUseCase>();
builder.Services.AddScoped<IRebalanceamentoUseCase, RebalanceamentoUseCase>();

var app = builder.Build();

// 4. Configurar o Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.UseCors("CorsSeguro");
app.MapControllers();

// --- BLOCO DE POPULAR DADOS DE TESTE ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Itau.CompraProgramada.Infrastructure.Data.AppDbContext>();
    
    // ESTA É A LINHA MÁGICA: Ela cria o banco e as tabelas automaticamente se não existirem!
    db.Database.Migrate();

    // Se não tiver nenhuma cesta, a gente cria os dados
    if (!db.CestasRecomendacao.Any())
    {
        // 1. Cria a Cesta Top Five
        var cesta = new Itau.CompraProgramada.Domain.Entities.CestaRecomendacao("Top Five", new List<Itau.CompraProgramada.Domain.Entities.ItemCesta>
        {
            new("PETR4", 20m), new("VALE3", 20m), new("ITUB4", 20m),
            new("BBDC4", 20m), new("WEGE3", 20m)
        });
        db.CestasRecomendacao.Add(cesta);

        // 2. Cria 2 Clientes (João com R$ 1000 e Maria com R$ 2000)
        var cliente1 = new Itau.CompraProgramada.Domain.Entities.Cliente("João Silva", "11111111111", "joao@itau.com", 1000m);
        cliente1.VincularContaGrafica(new Itau.CompraProgramada.Domain.Entities.ContaGrafica(null, "FLH-001", Itau.CompraProgramada.Domain.Enums.TipoContaGrafica.Filhote));

        var cliente2 = new Itau.CompraProgramada.Domain.Entities.Cliente("Maria Souza", "22222222222", "maria@itau.com", 2000m);
        cliente2.VincularContaGrafica(new Itau.CompraProgramada.Domain.Entities.ContaGrafica(null, "FLH-002", Itau.CompraProgramada.Domain.Enums.TipoContaGrafica.Filhote));

        db.Clientes.AddRange(cliente1, cliente2);

        // 3. Cria a Conta Master da Corretora
        var contaMaster = new Itau.CompraProgramada.Domain.Entities.ContaGrafica(null, "MASTER-000", Itau.CompraProgramada.Domain.Enums.TipoContaGrafica.Master);
        db.ContasGraficas.Add(contaMaster);

        db.SaveChanges();
    }
}
// ----------------------------------------

app.Run();