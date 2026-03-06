# Itaú - Desafio Compra Programada

Este repositório contém a solução para o **Desafio Itaú - Compra Programada**, um sistema de backend robusto projetado para gerenciar investimentos em ações e rebalanceamentos de carteira de forma automatizada, utilizando práticas modernas de engenharia de software e domínio de investimentos da B3.

## 🚀 Funcionalidades (Fases Concluídas 100%)

O projeto foi dividido em diversas fases lógicas, detalhadas na documentação original, cobrindo todo o fluxo da Compra Programada:
1. **Gestão de Clientes**: Adesão, Saída e Edição de Valor Mensal.
2. **Cesta Top Five (Admin)**: Criação de carteiras recomendadas padronizadas (pesos exatos).
3. **Motor de Compras (Core)**: Rateio financeiro considerando lote padrão vs. mercado fracionário.
4. **Custódia Master**: Contenção de resíduos e reuso inteligente de sobras na compra seguinte.
5. **Rebalanceamento por Mudança de Cesta**: Automático. Vendas imediatas quando ativos são removidos da recomendação.
6. **Rebalanceamento por Desvio (Drift)**: Restaura o equilíbrio da carteira frente a uma variação parametrizada de mercado.
7. **Regras Fiscais (Imposto de Renda)**: Cálculo de Dedo-Duro (0,005%) na fonte e mensuração de vendas para isenção (< R$ 20.000,00). Publicação direta no Kafka.
8. **Rentabilidade**: Consolidação financeira por cliente. Cálculo de Lucro/Prejuízo (P/L), P/L Geral e % de rentabilidade ponderada.

---

## 🏗 Arquitetura & Decisões de Design

O projeto acompanha **Clean Architecture** aliada aos princípios de **Domain-Driven Design (DDD)**.
A separação em camadas garante a testabilidade total (100% dos fluxos complexos testados) e o isolamento de tecnologias (banco, fila):

- **Domain**: Entidades de negócio ricas (`Cliente`, `EventoIR`, `Custodia`, `CestaRecomendacao`), Domain Services dedicados (`CalculoIRService`, `CalculoDesvioService`) e validações. Tolerância à infraestrutura.
- **Application**: Fluxos de orquestração via Use Cases (`RentabilidadeUseCase`, `MotorCompraProgramadaUseCase`). Definição estrita das DTOs de transição (Request, Response, Kafka DTOs).
- **Infrastructure**: Provedores concretos, implementação de Entity Framework Core para o MySQL, integrações e HealthChecks.
- **Api**: Superfície de contato simplificada (Controllers), injeção de dependências e Swagger. Setup do HealthCheck `/health`.

### Decisões Cruciais
- **Arredondamento Matemático (Math.Truncate)**: Ao lidar com ações B3, o quantitativo fracionário foi devidamente separado nos algoritmos matemáticos, para que o investidor compre apenas o maior número possível _inteiro_ via lote padrão antes de transbordar ao mercado fracionário.
- **Health Checks & Tokens**: Resiliência da stack via tokens de cancelamento nas queries primárias (Repo) e setup oficial do _AspNetCore.HealthChecks_.
- **TDD (Test-Driven Development)**: Construído gradualmente. Atingimos a marca de **58 testes passando e 82% de cobertura de código** focado em lógica de negócio (excl. Infra), usando NSubstitute + FluentAssertions.

---

## 🛠 Pré-requisitos e Instalação

* **.NET 9.0 SDK** ([Baixar aqui](https://dotnet.microsoft.com/download/dotnet/9.0))
* **Docker** e **Docker-Compose** (para MySQL & Kafka)

### 1. Subindo a Infraestrutura Local
No diretório `infrastructure/` (onde se encontra o *docker-compose.yml*), inicie o cluster do MySQL e do Kafka com o Zookeeper:

```bash
docker-compose up -d
```

### 2. Rodando o Projeto Backend
Rode a aplicação. O `EntityFramework (Code-First)` criará as tabelas do MySQL on-the-flight se configurado.

```bash
cd src/Itau.CompraProgramada.Api
dotnet run
```

A API iniciará por padrão em `https://localhost:5001` ou `http://localhost:5000`. 
**Acesse o Swagger** através do seu navegador em: 
👉 `http://localhost:5000/swagger/index.html`

---

## 🛣️ Principais Endpoints

### 👤 Clientes
- `POST /api/clientes/adesao`: Entra no programa com valor em R$ base.
- `GET /api/clientes/{id}/carteira`: Visualize a posse atual (`Custodia`).
- `GET /api/clientes/{id}/rentabilidade`: Sumário financeiro com P/L dinâmico gerando cotação faturada.
- `PUT /api/clientes/{id}/valor-mensal`: Altera a injeção financeira para os meses seguintes.
- `DELETE /api/clientes/{id}/saida`: Pausa/Encerra o envolvimento do cliente sem liqüidar os papéis.

### 🛡 Admin (B3)
- `POST /api/admin/cesta`: Cria a cesta e dispara _Rebalanceamento Por Cesta_ global automático nas carteiras.
- `GET /api/admin/conta-master/custodia`: Monitora as "sobras" e "resíduos" comprados pelo banco em atacado que servirão para rateio orgânico no período futuro.
- `POST /api/admin/rebalanceamento-desvio`: Varre os investidores e alinha ordens de compra/venda para reajuste das posições fora do linear.

### ❤️ Misc
- `GET /health`: Monitoramento Liveness e Readiness da Api + DB MySQL + fila Apache Kafka.

---

🌟 **Finalizado para o Desafio Itaú** : Todo o Core funcional foi entregue focado fortemente em código coeso, alta disponibilidade matemática e documentação pragmática.
