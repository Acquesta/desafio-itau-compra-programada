using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Itau.CompraProgramada.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Itau.CompraProgramada.IntegrationTests;

public class ClientesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ClientesControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Aderir_RequisicaoValida_DeveRetornarCreatedECriarContaFilhote()
    {
        // Arrange
        var request = new AdesaoRequest(
            Nome: "Cliente Integração",
            Cpf: "12345678912",
            Email: "integra@teste.com",
            ValorMensal: 1000m
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes/adesao", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<AdesaoResponse>();
        result.Should().NotBeNull();
        result!.Nome.Should().Be("Cliente Integração");
        result.Ativo.Should().BeTrue();
        result.ContaGrafica.Should().NotBeNull();
        result.ContaGrafica.Tipo.Should().Be("Filhote");
        result.ContaGrafica.NumeroConta.Should().StartWith("FLH-");
    }

    [Fact]
    public async Task Aderir_CpfDuplicado_DeveRetornarConflict()
    {
        // Arrange
        var request = new AdesaoRequest("Cliente Duplicado", "22233344455", "dup@teste.com", 500m);
        await _client.PostAsJsonAsync("/api/clientes/adesao", request); // Primeira chamada

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes/adesao", request); // Segunda chamada com mesmo CPF

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var erro = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        erro!.Codigo.Should().Be("CLIENTE_CPF_DUPLICADO");
    }
}
