
// Em Controllers/EncomendasController.cs
using Microsoft.AspNetCore.Mvc;
using System; // Para DateTime
using System.Collections.Generic; // Para List
using System.Linq; // Para Any
using System.Threading.Tasks; // Para Task
// using Microsoft.AspNetCore.Authorization; // Se precisar de autorização

// [Authorize(Roles="Admin")] // Protege todo o controller se necessário
public class EncomendasController : Controller
{
    // GET: /Encomendas/Index
    public IActionResult Index()
    {
        ViewData["Title"] = "Gestão de Encomendas";
        return View(); // Isso vai procurar por Views/Encomendas/Index.cshtml
    }

    // API para listar encomendas
    // AJUSTE AS ROTAS CONFORME SUA NECESSIDADE E O JAVASCRIPT
    [HttpGet("api/Encomendas/Listar")]
    public IActionResult ListarEncomendasApi() // Adicione parâmetros de filtro se necessário
    {
        // LÓGICA PARA BUSCAR AS ENCOMENDAS DO BANCO DE DADOS E RETORNAR COMO JSON
        // Substitua isto com a sua lógica real
        var encomendasExemplo = new[] {
            new { encomendaId = 101, nomeCliente = "Construções XPTO", dataEncomenda = "2023-11-01T10:00:00Z", nomeObra = "Edifício Central", descricao = "Cimento, Tijolos (10 paletes)", estado = "Pendente" },
            new { encomendaId = 102, nomeCliente = "Obras Rápidas Lda.", dataEncomenda = "2023-11-05T14:30:00Z", nomeObra = "Remodelação Loja A", descricao = "Tintas (50L), Pincéis", estado = "Em Processamento" },
            new { encomendaId = 103, nomeCliente = "Construções XPTO", dataEncomenda = "2023-11-10T09:15:00Z", nomeObra = "Moradia Vilar", descricao = "Areia (2m³), Brita (1m³)", estado = "Entregue" }
        };
        return Ok(encomendasExemplo);
    }

    // API para excluir encomendas
    // AJUSTE AS ROTAS CONFORME SUA NECESSIDADE E O JAVASCRIPT
    [HttpPost("api/Encomendas/Excluir")]
    public IActionResult ExcluirEncomendasApi([FromBody] List<long> idsParaExcluir)
    {
        // LÓGICA PARA EXCLUIR AS ENCOMENDAS DO BANCO DE DADOS
        // Substitua isto com a sua lógica real
        if (idsParaExcluir == null || !idsParaExcluir.Any())
        {
            return BadRequest(new { message = "Nenhum ID de encomenda fornecido." });
        }
        Console.WriteLine($"API: Tentando excluir encomendas com IDs: {string.Join(", ", idsParaExcluir)}");
        // Simular a exclusão e responder
        return Ok(new { message = $"{idsParaExcluir.Count} encomenda(s) marcada(s) para exclusão (simulado via API)." });
    }

    // API para mudar o estado da encomenda
    [HttpPost("api/Encomendas/MudarEstado")]
    public IActionResult MudarEstadoEncomendaApi([FromBody] MudarEstadoEncomendaRequest request)
    {
        if (request == null || request.Id <= 0 || string.IsNullOrEmpty(request.NovoEstado))
        {
            return BadRequest(new { message = "Dados inválidos para mudar estado da encomenda." });
        }

        // LÓGICA PARA ENCONTRAR A ENCOMENDA PELO request.Id E ATUALIZAR O ESTADO PARA request.NovoEstado NO BANCO DE DADOS
        Console.WriteLine($"API: Tentando mudar estado da encomenda ID {request.Id} para '{request.NovoEstado}'");

        // Simular sucesso
        return Ok(new { message = $"Estado da encomenda ID {request.Id} atualizado para '{request.NovoEstado}' com sucesso." });
    }
}

// Modelo para o pedido de mudança de estado (pode ir num ficheiro separado em Models ou ViewModels)
public class MudarEstadoEncomendaRequest
{
    public long Id { get; set; }
    public required string NovoEstado { get; set; }
}
