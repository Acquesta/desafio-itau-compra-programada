using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itau.CompraProgramada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSaidaToCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LucroLiquido",
                table: "Rebalanceamentos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoMedio",
                table: "Rebalanceamentos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Quantidade",
                table: "Rebalanceamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecoUnitario",
                table: "OrdensCompra",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "EventosIR",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoUnitario",
                table: "EventosIR",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Quantidade",
                table: "EventosIR",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Ticker",
                table: "EventosIR",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecoUnitario",
                table: "Distribuicoes",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataSaida",
                table: "Clientes",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistoricoValoresMensais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    ValorAnterior = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ValorNovo = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoValoresMensais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoValoresMensais_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Distribuicoes_CustodiaFilhoteId",
                table: "Distribuicoes",
                column: "CustodiaFilhoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Distribuicoes_OrdemCompraId",
                table: "Distribuicoes",
                column: "OrdemCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoValoresMensais_ClienteId",
                table: "HistoricoValoresMensais",
                column: "ClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Distribuicoes_Clientes_CustodiaFilhoteId",
                table: "Distribuicoes",
                column: "CustodiaFilhoteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Distribuicoes_OrdensCompra_OrdemCompraId",
                table: "Distribuicoes",
                column: "OrdemCompraId",
                principalTable: "OrdensCompra",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distribuicoes_Clientes_CustodiaFilhoteId",
                table: "Distribuicoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Distribuicoes_OrdensCompra_OrdemCompraId",
                table: "Distribuicoes");

            migrationBuilder.DropTable(
                name: "HistoricoValoresMensais");

            migrationBuilder.DropIndex(
                name: "IX_Distribuicoes_CustodiaFilhoteId",
                table: "Distribuicoes");

            migrationBuilder.DropIndex(
                name: "IX_Distribuicoes_OrdemCompraId",
                table: "Distribuicoes");

            migrationBuilder.DropColumn(
                name: "LucroLiquido",
                table: "Rebalanceamentos");

            migrationBuilder.DropColumn(
                name: "PrecoMedio",
                table: "Rebalanceamentos");

            migrationBuilder.DropColumn(
                name: "Quantidade",
                table: "Rebalanceamentos");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "EventosIR");

            migrationBuilder.DropColumn(
                name: "PrecoUnitario",
                table: "EventosIR");

            migrationBuilder.DropColumn(
                name: "Quantidade",
                table: "EventosIR");

            migrationBuilder.DropColumn(
                name: "Ticker",
                table: "EventosIR");

            migrationBuilder.DropColumn(
                name: "DataSaida",
                table: "Clientes");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecoUnitario",
                table: "OrdensCompra",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,5)",
                oldPrecision: 18,
                oldScale: 5);

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecoUnitario",
                table: "Distribuicoes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,5)",
                oldPrecision: 18,
                oldScale: 5);
        }
    }
}
