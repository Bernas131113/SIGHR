// Controllers/EncomendasController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIGHR.Models; // Namespace para a entidade Encomenda e Material
using SIGHR.Models.ViewModels; // Namespace para os ViewModels
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIGHR.Areas.Identity.Data;

namespace SIGHR.Controllers
{
    [Authorize]
    public class EncomendasController : Controller
    {
        private readonly SIGHRContext _context;
        private readonly UserManager<SIGHRUser> _userManager;
        private readonly ILogger<EncomendasController> _logger;

        private static readonly List<SelectListItem> ListaDeMateriaisFixa = new List<SelectListItem>
        {
            new SelectListItem { Value = "Tijolo 7", Text = "Tijolo 7" },
            new SelectListItem { Value = "Tijolo 11", Text = "Tijolo 11" },
            new SelectListItem { Value = "Tijolo 15", Text = "Tijolo 15" },
            new SelectListItem { Value = "Tijolo 22", Text = "Tijolo 22" },
            new SelectListItem { Value = "Areia do Rio", Text = "Areia do Rio" },
            new SelectListItem { Value = "Areia Amarela", Text = "Areia Amarela" },
            new SelectListItem { Value = "Blocos 10", Text = "Blocos 10" },
            new SelectListItem { Value = "Blocos 15", Text = "Blocos 15" },
            new SelectListItem { Value = "Blocos 20", Text = "Blocos 20" },
            new SelectListItem { Value = "Cimento", Text = "Cimento" },
            new SelectListItem { Value = "Cal hidráulica", Text = "Cal hidráulica" }
        };

        public EncomendasController(SIGHRContext context, UserManager<SIGHRUser> userManager, ILogger<EncomendasController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // --- VIEW DE GESTÃO DE TODAS AS ENCOMENDAS (ADMIN/OFFICE) ---
        [HttpGet]
        [Authorize(Roles = "Admin,Office", AuthenticationSchemes = "Identity.Application,AdminLoginScheme")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Gestão de Encomendas";
            return View(); // Views/Encomendas/Index.cshtml (usa JavaScript)
        }

        // --- VIEW "MINHAS ENCOMENDAS" (COLABORADOR) ---
        [HttpGet]
        [Authorize(Roles = "Admin,Office,Collaborator", AuthenticationSchemes = "Identity.Application,AdminLoginScheme,CollaboratorLoginScheme")]
        public async Task<IActionResult> MinhasEncomendas(DateTime? filtroData, string? filtroEstado)
        {
            ViewData["Title"] = "Minhas Encomendas";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Utilizador não autenticado.");

            IQueryable<Encomenda> query = _context.Encomendas
                .Include(e => e.Requisicoes)
                    .ThenInclude(r => r.Material)
                .Where(e => e.UtilizadorId == userId);

            if (filtroData.HasValue) query = query.Where(e => e.Data.Date == filtroData.Value.Date);
            if (!string.IsNullOrEmpty(filtroEstado)) query = query.Where(e => e.Estado == filtroEstado);

            var minhasEncomendasViewModels = await query
                .OrderByDescending(e => e.Data)
                .Select(e => new MinhaEncomendaViewModel
                {
                    EncomendaId = e.Id,
                    DataEncomenda = e.Data,
                    DescricaoResumida = (e.Requisicoes != null && e.Requisicoes.Any()) ?
                        string.Join(", ", e.Requisicoes
                            .Where(r => r.Material != null)
                            .Select(r => r.Material!.Descricao ?? "Item")
                            .Take(2)) + (e.Requisicoes.Count > 2 ? "..." : "") :
                        "Nenhum material",
                    QuantidadeTotalItens = (e.Requisicoes != null) ? e.Requisicoes.Sum(r => (int)r.Quantidade) : 0,
                    Estado = e.Estado ?? "Indefinido"
                })
                .ToListAsync();

            ViewData["FiltroDataAtual"] = filtroData?.ToString("yyyy-MM-dd");
            ViewData["FiltroEstadoAtual"] = filtroEstado;
            ViewBag.EstadosEncomenda = new List<string> { "Pendente", "Em Processamento", "Pronta para Envio", "Enviada", "Entregue", "Cancelada" };
            return View(minhasEncomendasViewModels); // Views/Encomendas/MinhasEncomendas.cshtml
        }

        // --- VIEW REGISTAR ENCOMENDA ---
        [HttpGet]
        [Authorize(Roles = "Admin,Office,Collaborator", AuthenticationSchemes = "Identity.Application,AdminLoginScheme,CollaboratorLoginScheme")]
        public async Task<IActionResult> Registar()
        {
            ViewData["Title"] = "Registar Nova Encomenda";
            var materiaisDisponiveis = await Task.FromResult(ListaDeMateriaisFixa);
            var viewModel = new RegistarEncomendaViewModel
            {
                DataEncomenda = DateTime.Today,
                ItensRequisicao = new List<ItemRequisicaoViewModel> { new ItemRequisicaoViewModel { Quantidade = 1 } },
                MateriaisDisponiveis = new SelectList(materiaisDisponiveis, "Value", "Text")
            };
            return View(viewModel); // Views/Encomendas/Registar.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Office,Collaborator", AuthenticationSchemes = "Identity.Application,AdminLoginScheme,CollaboratorLoginScheme")]
        public async Task<IActionResult> Registar(RegistarEncomendaViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Utilizador não autenticado.");

            model.MateriaisDisponiveis = new SelectList(ListaDeMateriaisFixa, "Value", "Text");

            bool itensSaoValidos = true;
            if (model.ItensRequisicao == null || !model.ItensRequisicao.Any())
            {
                ModelState.AddModelError("ItensRequisicao", "Adicione pelo menos um material.");
                itensSaoValidos = false;
            }
            else
            {
                foreach (var item in model.ItensRequisicao)
                {
                    if (string.IsNullOrEmpty(item.NomeMaterialOuId)) { ModelState.AddModelError("", "Selecione um material para todos os itens."); itensSaoValidos = false; break; }
                    if (item.Quantidade <= 0) { ModelState.AddModelError("", "A quantidade deve ser maior que zero."); itensSaoValidos = false; break; }
                }
            }

            if (ModelState.IsValid && itensSaoValidos)
            {
                var novaEncomenda = new Encomenda
                {
                    UtilizadorId = userId,
                    Data = model.DataEncomenda,
                    Estado = "Pendente",
                    Quantidade = model.ItensRequisicao!.Count,
                    DescricaoObra = model.DescricaoObra, // Mapeando do ViewModel
                    Requisicoes = new List<Requisicao>()
                };

                foreach (var itemVM in model.ItensRequisicao!)
                {
                    Material? materialEntity = null;
                    if (!string.IsNullOrEmpty(itemVM.NomeMaterialOuId))
                    {
                        materialEntity = await _context.Materiais.FirstOrDefaultAsync(m => m.Descricao == itemVM.NomeMaterialOuId);
                        if (materialEntity == null)
                        {
                            _logger.LogWarning("Material '{MaterialNome}' não encontrado. Criando novo.", itemVM.NomeMaterialOuId);
                            materialEntity = new Material { Descricao = itemVM.NomeMaterialOuId };
                            _context.Materiais.Add(materialEntity);
                        }
                    }
                    if (materialEntity == null)
                    {
                        ModelState.AddModelError("", $"Material '{itemVM.NomeMaterialOuId ?? "N/D"}' inválido.");
                        return View(model);
                    }
                    novaEncomenda.Requisicoes.Add(new Requisicao { Material = materialEntity, Quantidade = itemVM.Quantidade });
                }

                _context.Encomendas.Add(novaEncomenda);
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Encomenda registrada com sucesso!";
                    return RedirectToAction(nameof(MinhasEncomendas));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao salvar encomenda para UserID: {UserId}.", userId);
                    ModelState.AddModelError("", "Ocorreu um erro ao salvar. Tente novamente.");
                }
            }
            return View(model);
        }

        // --- API ENDPOINTS ---
        [HttpGet("api/Encomendas/Listar")]
        [Authorize(Roles = "Admin,Office", AuthenticationSchemes = "Identity.Application,AdminLoginScheme")]
        public async Task<IActionResult> ListarEncomendasApi(string? filtroClienteObra, DateTime? filtroData, string? filtroEstado)
        {
            _logger.LogInformation("API ListarEncomendasApi. Cliente/Obra: {FCO}, Data: {FD}, Estado: {FE}", filtroClienteObra, filtroData, filtroEstado);
            try
            {
                IQueryable<Encomenda> query = _context.Encomendas
                    .Include(e => e.User)
                    .Include(e => e.Requisicoes!).ThenInclude(r => r.Material);

                if (!string.IsNullOrEmpty(filtroClienteObra)) query = query.Where(e => e.User != null && ((e.User.NomeCompleto != null && e.User.NomeCompleto.Contains(filtroClienteObra)) || (e.User.UserName != null && e.User.UserName.Contains(filtroClienteObra))) || (e.DescricaoObra != null && e.DescricaoObra.Contains(filtroClienteObra)));
                if (filtroData.HasValue) query = query.Where(e => e.Data.Date == filtroData.Value.Date);
                if (!string.IsNullOrEmpty(filtroEstado)) query = query.Where(e => e.Estado == filtroEstado);

                var encomendas = await query.OrderByDescending(e => e.Data)
                    .Select(e => new {
                        encomendaId = e.Id,
                        nomeCliente = e.User != null ? (e.User.NomeCompleto ?? e.User.UserName ?? "N/D") : "N/D",
                        dataEncomenda = e.Data.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        nomeObra = e.DescricaoObra ?? "N/D", // Usando DescricaoObra da entidade
                        descricao = (e.Requisicoes != null && e.Requisicoes.Any()) ? string.Join(", ", e.Requisicoes.Where(r => r.Material != null).Select(r => r.Material!.Descricao ?? "").Take(3)) + (e.Requisicoes.Count > 3 ? "..." : "") : "Sem itens",
                        estado = e.Estado ?? "Indefinido"
                    }).ToListAsync();
                return Ok(encomendas);
            }
            catch (Exception ex) { _logger.LogError(ex, "Erro API ListarEncomendas"); return StatusCode(500, "Erro interno"); }
        }

        [HttpPost("api/Encomendas/Excluir")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = "Identity.Application,AdminLoginScheme")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirEncomendasApi([FromBody] List<long> idsParaExcluir)
        {
            if (idsParaExcluir == null || !idsParaExcluir.Any()) return BadRequest(new { message = "Nenhum ID." });
            try
            {
                var requisicoes = await _context.Requisicoes.Where(r => idsParaExcluir.Contains(r.EncomendaId)).ToListAsync();
                if (requisicoes.Any()) _context.Requisicoes.RemoveRange(requisicoes);
                var encomendas = await _context.Encomendas.Where(e => idsParaExcluir.Contains(e.Id)).ToListAsync();
                if (!encomendas.Any()) return NotFound(new { message = "Não encontradas." });
                _context.Encomendas.RemoveRange(encomendas);
                await _context.SaveChangesAsync();
                return Ok(new { message = $"{encomendas.Count} excluída(s)." });
            }
            catch (Exception ex) { _logger.LogError(ex, "Erro API Excluir"); return StatusCode(500, "Erro ao excluir."); }
        }

        [HttpPost("api/Encomendas/MudarEstado")]
        [Authorize(Roles = "Admin,Office", AuthenticationSchemes = "Identity.Application,AdminLoginScheme")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MudarEstadoEncomendaApi([FromBody] MudarEstadoEncomendaRequest request)
        {
            if (!ModelState.IsValid) // Valida o request com base nas DataAnnotations
            {
                return BadRequest(ModelState);
            }
            _logger.LogInformation("API MudarEstadoEncomenda: ID {Id}, Estado {NovoEstado}, por {User}", request.Id, request.NovoEstado, User.Identity?.Name);
            try
            {
                var encomenda = await _context.Encomendas.FindAsync(request.Id);
                if (encomenda == null) return NotFound(new { message = $"Encomenda {request.Id} não encontrada." });

                var estadosValidos = new List<string> { "Pendente", "Em Processamento", "Pronta para Envio", "Enviada", "Entregue", "Cancelada" };
                if (!estadosValidos.Contains(request.NovoEstado)) return BadRequest(new { message = $"Estado '{request.NovoEstado}' inválido." });

                encomenda.Estado = request.NovoEstado;
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Estado da encomenda {request.Id} atualizado para '{request.NovoEstado}'." });
            }
            catch (Exception ex) { _logger.LogError(ex, "Erro API MudarEstadoEncomenda ID: {Id}", request.Id); return StatusCode(500, "Erro ao mudar estado."); }
        }
    }
}