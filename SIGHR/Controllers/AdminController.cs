// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Models;
using SIGHR.Models.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using SIGHR.Areas.Identity.Data;

namespace SIGHR.Controllers
{
    /// <summary>
    /// Controlador para a área de administração da aplicação.
    /// Acesso restrito a utilizadores com a função "Admin", definido pela política "AdminAccessUI".
    /// </summary>
    [Authorize(Policy = "AdminAccessUI")]
    public class AdminController : Controller
    {
        //
        // Bloco: Injeção de Dependências
        // Serviços essenciais que o controlador utiliza.
        //
        private readonly SIGHRContext _context;
        private readonly UserManager<SIGHRUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(SIGHRContext context, UserManager<SIGHRUser> userManager, ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        //
        // Bloco: Gestão de Registos de Ponto
        //

        /// <summary>
        /// Action principal que exibe a página de gestão de todos os registos de ponto.
        /// Permite filtrar os resultados por nome de utilizador e/ou data.
        /// </summary>
        /// <param name="filtroNome">Texto para pesquisar no nome de utilizador ou nome completo.</param>
        /// <param name="filtroData">Data específica para filtrar os registos.</param>
        /// <returns>A View 'Index' com a lista de horários filtrada.</returns>
        [HttpGet] // Define que esta action responde a pedidos GET.
        public async Task<IActionResult> Index(string? filtroNome, DateTime? filtroData)
        {
            ViewData["Title"] = "Gestão de Registos de Ponto";

            // Inicia a consulta à base de dados, incluindo os dados do utilizador associado.
            IQueryable<Horario> query = _context.Horarios.Include(h => h.User);

            // Aplica o filtro de nome, se fornecido.
            if (!string.IsNullOrEmpty(filtroNome))
            {
                query = query.Where(h => h.User != null && ((h.User.UserName != null && h.User.UserName.Contains(filtroNome)) || (h.User.NomeCompleto != null && h.User.NomeCompleto.Contains(filtroNome))));
            }

            // Aplica o filtro de data, se fornecido.
            if (filtroData.HasValue)
            {
                query = query.Where(h => h.Data.Date == filtroData.Value.Date);
            }

            // Executa a consulta, ordena os resultados e converte para uma lista.
            var horarios = await query.OrderByDescending(h => h.Data)
                                      .ThenBy(h => h.User != null ? h.User.UserName ?? "" : "")
                                      .ThenBy(h => h.HoraEntrada)
                                      .ToListAsync();

            // Mapeia os dados da base de dados (Horario) para um ViewModel, que é mais adequado para a View.
            // O ViewModel calcula o total de horas trabalhadas.
            var viewModels = horarios.Select(h => {
                TimeSpan totalTrabalhado = TimeSpan.Zero, tempoAlmoco = TimeSpan.Zero;
                if (h.EntradaAlmoco > h.SaidaAlmoco && h.SaidaAlmoco != TimeSpan.Zero) tempoAlmoco = h.EntradaAlmoco - h.SaidaAlmoco;
                if (h.HoraSaida > h.HoraEntrada && h.HoraEntrada != TimeSpan.Zero)
                {
                    totalTrabalhado = (h.HoraSaida - h.HoraEntrada) - tempoAlmoco;
                    if (totalTrabalhado < TimeSpan.Zero) totalTrabalhado = TimeSpan.Zero;
                }
                return new HorarioAdminViewModel
                {
                    HorarioId = h.Id,
                    NomeUtilizador = h.User != null ? (h.User.NomeCompleto ?? h.User.UserName ?? "Desconhecido") : "Desconhecido",
                    Data = h.Data,
                    HoraEntrada = h.HoraEntrada,
                    SaidaAlmoco = h.SaidaAlmoco,
                    EntradaAlmoco = h.EntradaAlmoco,
                    HoraSaida = h.HoraSaida,
                    TotalHorasTrabalhadas = totalTrabalhado > TimeSpan.Zero ? $"{(int)totalTrabalhado.TotalHours:D2}:{totalTrabalhado.Minutes:D2}" : "--:--"
                };
            }).ToList();

            // Guarda os valores dos filtros para os reexibir nos campos de pesquisa.
            ViewData["FiltroNomeAtual"] = filtroNome;
            ViewData["FiltroDataAtual"] = filtroData?.ToString("yyyy-MM-dd");

            return View(viewModels);
        }

        /// <summary>
        /// Action para gerar e transferir um ficheiro Excel com os registos de ponto.
        /// Utiliza os mesmos filtros da página 'Index'.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadHorariosExcel(string? filtroNome, DateTime? filtroData)
        {
            // A lógica de filtragem é idêntica à da action Index.
            IQueryable<Horario> query = _context.Horarios.Include(h => h.User);
            if (!string.IsNullOrEmpty(filtroNome)) query = query.Where(h => h.User != null && ((h.User.UserName != null && h.User.UserName.Contains(filtroNome)) || (h.User.NomeCompleto != null && h.User.NomeCompleto.Contains(filtroNome))));
            if (filtroData.HasValue) query = query.Where(h => h.Data.Date == filtroData.Value.Date);

            var horariosParaExportar = await query
                .OrderByDescending(h => h.Data)
                .ThenBy(h => h.User != null ? h.User.UserName ?? "" : "")
                .ThenBy(h => h.HoraEntrada)
                .Select(h => new {
                    NomeUtilizador = h.User != null ? (h.User.NomeCompleto ?? h.User.UserName ?? "Desconhecido") : "Desconhecido",
                    h.Data,
                    h.HoraEntrada,
                    h.SaidaAlmoco,
                    h.EntradaAlmoco,
                    h.HoraSaida
                })
                .ToListAsync();

            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string fileName = $"RegistosPonto_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            // Utiliza a biblioteca ClosedXML para criar o ficheiro Excel em memória.
            using (var workbook = new XLWorkbook())
            {
                IXLWorksheet worksheet = workbook.Worksheets.Add("Registos de Ponto");

                // Cabeçalho da folha de cálculo
                worksheet.Cell(1, 1).Value = "Utilizador"; worksheet.Cell(1, 2).Value = "Data"; worksheet.Cell(1, 3).Value = "Entrada"; worksheet.Cell(1, 4).Value = "Saída Almoço"; worksheet.Cell(1, 5).Value = "Entrada Almoço"; worksheet.Cell(1, 6).Value = "Saída"; worksheet.Cell(1, 7).Value = "Total Horas";
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true; headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Preenche as linhas com os dados dos horários.
                int currentRow = 2;
                foreach (var item in horariosParaExportar)
                {
                    worksheet.Cell(currentRow, 1).SetValue(item.NomeUtilizador);
                    worksheet.Cell(currentRow, 2).SetValue(item.Data).Style.NumberFormat.Format = "dd/MM/yyyy";
                    if (item.HoraEntrada != TimeSpan.Zero) worksheet.Cell(currentRow, 3).SetValue(item.HoraEntrada).Style.NumberFormat.Format = "hh:mm";
                    if (item.SaidaAlmoco != TimeSpan.Zero) worksheet.Cell(currentRow, 4).SetValue(item.SaidaAlmoco).Style.NumberFormat.Format = "hh:mm";
                    if (item.EntradaAlmoco != TimeSpan.Zero) worksheet.Cell(currentRow, 5).SetValue(item.EntradaAlmoco).Style.NumberFormat.Format = "hh:mm";
                    if (item.HoraSaida != TimeSpan.Zero) worksheet.Cell(currentRow, 6).SetValue(item.HoraSaida).Style.NumberFormat.Format = "hh:mm";

                    // Recalcula o total de horas para o Excel.
                    TimeSpan totalTrabalhadoExcel = TimeSpan.Zero, tempoAlmocoExcel = TimeSpan.Zero;
                    if (item.EntradaAlmoco > item.SaidaAlmoco && item.SaidaAlmoco != TimeSpan.Zero) tempoAlmocoExcel = item.EntradaAlmoco - item.SaidaAlmoco;
                    if (item.HoraSaida > item.HoraEntrada && item.HoraEntrada != TimeSpan.Zero) { totalTrabalhadoExcel = (item.HoraSaida - item.HoraEntrada) - tempoAlmocoExcel; if (totalTrabalhadoExcel < TimeSpan.Zero) totalTrabalhadoExcel = TimeSpan.Zero; }
                    if (totalTrabalhadoExcel > TimeSpan.Zero) worksheet.Cell(currentRow, 7).SetValue(totalTrabalhadoExcel).Style.NumberFormat.Format = "[h]:mm";

                    currentRow++;
                }

                // Ajusta a largura das colunas ao conteúdo.
                for (int i = 1; i <= 7; i++) worksheet.Column(i).AdjustToContents();

                // Guarda o ficheiro numa stream de memória e retorna-o ao utilizador para transferência.
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), contentType, fileName);
                }
            }
        }

        //
        // Bloco: Autenticação
        //

        /// <summary>
        /// Action para realizar o logout do administrador.
        /// Remove o cookie de autenticação do esquema "AdminLoginScheme".
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminLoginScheme");
            return RedirectToPage("/Account/AdminLogin", new { area = "Identity" });
        }
    }
}