using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Itau.CompraProgramada.Infrastructure.Mensageria;

public class EventoIRPublisher : IEventoIRPublisher, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topico;

    public EventoIRPublisher(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
        };
        
        _topico = configuration["Kafka:TopicoIR"] ?? "eventos-ir-dedo-duro";
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublicarEventoAsync(EventoIR evento)
    {
        try
        {
            var mensagemJson = JsonSerializer.Serialize(evento);
            var mensagemKafka = new Message<Null, string> { Value = mensagemJson };

            var resultado = await _producer.ProduceAsync(_topico, mensagemKafka);

            if (resultado.Status == PersistenceStatus.Persisted)
            {
                evento.MarcarComoPublicado();
            }
        }
        catch (ProduceException<Null, string> e)
        {
            // TODO: Substituir por logging estruturado (Serilog)
            Console.WriteLine($"Erro ao publicar no Kafka: {e.Error.Reason}");
            throw; 
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}