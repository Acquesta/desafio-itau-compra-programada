using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Itau.CompraProgramada.Infrastructure.Mensageria;

public class EventoIRPublisher : IEventoIRPublisher
{
    private readonly ProducerConfig _config;
    private readonly string _topico;

    // Recebemos as configurações (appsettings.json) via injeção de dependência
    public EventoIRPublisher(IConfiguration configuration)
    {
        _config = new ProducerConfig
        {
            // O endereço do Kafka vira uma configuração (geralmente localhost:9092 no Docker)
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
        };
        
        _topico = configuration["Kafka:TopicoIR"] ?? "eventos-ir-dedo-duro";
    }

    public async Task PublicarEventoAsync(EventoIR evento)
    {
        using var producer = new ProducerBuilder<Null, string>(_config).Build();

        try
        {
            // Transforma a nossa entidade em um JSON legível
            var mensagemJson = JsonSerializer.Serialize(evento);

            var mensagemKafka = new Message<Null, string> { Value = mensagemJson };

            // Publica no tópico
            var resultado = await producer.ProduceAsync(_topico, mensagemKafka);

            // Se o Kafka confirmar que salvou a mensagem, atualizamos a entidade
            if (resultado.Status == PersistenceStatus.Persisted)
            {
                evento.MarcarComoPublicado();
            }
        }
        catch (ProduceException<Null, string> e)
        {
            // Em um sistema real, aqui entraria um log de erro (ex: Serilog)
            Console.WriteLine($"Erro ao publicar no Kafka: {e.Error.Reason}");
            throw; 
        }
    }
}