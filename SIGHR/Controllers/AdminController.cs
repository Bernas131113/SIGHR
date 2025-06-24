// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authentication; // <<<< ADICIONADO/CONFIRMADO PARA SignOutAsync
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data; // Certifique-se que este é o namespace correto para SIGHRContext e SIGHRUser
using SIGHR.Models; // Para a entidade Horario
using SIGHR.Models.ViewModels; // Para HorarioAdminViewModel
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;

namespace SIGHR.Controllers
{
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminLoginScheme,Identity.Application")]
    public class AdminController : Controller
    {
        private readonly SIGHRContext _context;
        private readonly UserManager<SIGHRUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(SIGHRContext context, UserManager<SIGHRUser> userManager, ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Admin/ ou /Admin/Index
        public async Task<IActionResult> Index(string? filtroNome, DateTime? filtroData)
        {
            // ... (código da action Index como antes, já estava bom) ...
            _logger.LogInformation("AdminController/Index (Registo de Entradas) acessado. FiltroNome: {FiltroNome}, FiltroData: {FiltroData}", filtroNome, filtroData);
            ViewData["Title"] = "Registo de Entradas - Administração";

            IQueryable<Horario> query = _context.Horarios.Include(h => h.User);
            if (!string.IsNullOrEmpty(filtroNome))
            {
                query = query.Where(h => h.User != null && ((h.User.UserName != null && h.User.UserName.Contains(filtroNome)) || (h.User.NomeCompleto != null && h.User.NomeCompleto.Contains(filtroNome))));
            }
            if (filtroData.HasValue)
            {
                query = query.Where(h => h.Data.Date == filtroData.Value.Date);
            }
            var horarios = await query.OrderByDescending(h => h.Data).ThenBy(h => h.User != null ? h.User.UserName : "").ThenBy(h => h.HoraEntrada).ToListAsync();
            var viewModels = horarios.Select(h => {
                TimeSpan totalTrabalhado = TimeSpan.Zero; TimeSpan tempoAlmoco = TimeSpan.Zero;
                if (h.EntradaAlmoco != TimeSpan.Zero && h.SaidaAlmoco != TimeSpan.Zero && h.EntradaAlmoco > h.SaidaAlmoco) tempoAlmoco = h.EntradaAlmoco - h.SaidaAlmoco;
                if (h.HoraSaida != TimeSpan.Zero && h.HoraEntrada != TimeSpan.Zero && h.HoraSaida > h.HoraEntrada) { totalTrabalhado = (h.HoraSaida - h.HoraEntrada) - tempoAlmoco; if (totalTrabalhado < TimeSpan.Zero) totalTrabalhado = TimeSpan.Zero; }
                return new HorarioAdminViewModel { HorarioId = h.Id, NomeUtilizador = h.User != null ? (h.User.NomeCompleto ?? h.User.UserName ?? "Desconhecido") : "Desconhecido", Data = h.Data, HoraEntrada = h.HoraEntrada, SaidaAlmoco = h.SaidaAlmoco, EntradaAlmoco = h.EntradaAlmoco, HoraSaida = h.HoraSaida, TotalHorasTrabalhadas = totalTrabalhado > TimeSpan.Zero ? $"{(int)totalTrabalhado.TotalHours:D2}:{totalTrabalhado.Minutes:D2}" : "--:--" };
            }).ToList();
            ViewData["FiltroNomeAtual"] = filtroNome;
            ViewData["FiltroDataAtual"] = filtroData?.ToString("yyyy-MM-dd");
            return View(viewModels);
        }

        // GET: /Admin/DownloadHorariosExcel
        [HttpGet]
        public async Task<IActionResult> DownloadHorariosExcel(string? filtroNome, DateTime? filtroData)
        {
            _logger.LogInformation("DownloadHorariosExcel chamado com FiltroNome: {FiltroNome}, FiltroData: {FiltroData}", filtroNome, filtroData);

            IQueryable<Horario> query = _context.Horarios.Include(h => h.User);
            // ... (lógica de filtro igual à da action Index)
            if (!string.IsNullOrEmpty(filtroNome))
            {
                query = query.Where(h => h.User != null && ((h.User.UserName != null && h.User.UserName.Contains(filtroNome)) || (h.User.NomeCompleto != null && h.User.NomeCompleto.Contains(filtroNome))));
            }
            if (filtroData.HasValue)
            {
                query = query.Where(h => h.Data.Date == filtroData.Value.Date);
            }

            var horariosParaExportar = await query
                                .OrderByDescending(h => h.Data)
                                .ThenBy(h => h.User != null ? h.User.UserName : "")
                                .ThenBy(h => h.HoraEntrada)
                                .Select(h => new
                                {
                                    NomeUtilizador = h.User != null ? (h.User.NomeCompleto ?? h.User.UserName ?? "Desconhecido") : "Desconhecido",
                                    Data = h.Data,
                                    HoraEntrada = h.HoraEntrada,
                                    SaidaAlmoco = h.SaidaAlmoco,
                                    EntradaAlmoco = h.EntradaAlmoco,
                                    HoraSaida = h.HoraSaida
                                })
                                .ToListAsync();

            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string fileName = $"RegistosPonto_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            using (var workbook = new XLWorkbook())
            {
                IXLWorksheet worksheet = workbook.Worksheets.Add("Registos de Ponto");

                // Cabeçalhos
                worksheet.Cell(1, 1).Value = "Utilizador";
                worksheet.Cell(1, 2).Value = "Data";
                worksheet.Cell(1, 3).Value = "Hora Entrada";
                worksheet.Cell(1, 4).Value = "Saída Almoço";
                worksheet.Cell(1, 5).Value = "Entrada Almoço";
                worksheet.Cell(1, 6).Value = "Hora Saída";
                worksheet.Cell(1, 7).Value = "Total Horas";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRow.Height = 20; // Aumentar altura do cabeçalho
                worksheet.SheetView.FreezeRows(1); // Congelar a primeira linha (cabeçalho)

                int currentRow = 2;
                foreach (var item in horariosParaExportar)
                {
                    worksheet.Cell(currentRow, 1).SetValue(item.NomeUtilizador);
                    worksheet.Cell(currentRow, 2).SetValue(item.Data).Style.NumberFormat.Format = "dd/MM/yyyy";

                    // CORREÇÕES para células de TimeSpan:
                    if (item.HoraEntrada != TimeSpan.Zero) worksheet.Cell(currentRow, 3).SetValue(item.HoraEntrada).Style.NumberFormat.Format = "hh:mm";
                    else worksheet.Cell(currentRow, 3).SetValue(string.Empty);

                    if (item.SaidaAlmoco != TimeSpan.Zero) worksheet.Cell(currentRow, 4).SetValue(item.SaidaAlmoco).Style.NumberFormat.Format = "hh:mm";
                    else worksheet.Cell(currentRow, 4).SetValue(string.Empty);

                    if (item.EntradaAlmoco != TimeSpan.Zero) worksheet.Cell(currentRow, 5).SetValue(item.EntradaAlmoco).Style.NumberFormat.Format = "hh:mm";
                    else worksheet.Cell(currentRow, 5).SetValue(string.Empty);

                    if (item.HoraSaida != TimeSpan.Zero) worksheet.Cell(currentRow, 6).SetValue(item.HoraSaida).Style.NumberFormat.Format = "hh:mm";
                    else worksheet.Cell(currentRow, 6).SetValue(string.Empty);

                    TimeSpan totalTrabalhadoExcel = TimeSpan.Zero;
                    TimeSpan tempoAlmocoExcel = TimeSpan.Zero;
                    if (item.EntradaAlmoco != TimeSpan.Zero && item.SaidaAlmoco != TimeSpan.Zero && item.EntradaAlmoco > item.SaidaAlmoco)
                    {
                        tempoAlmocoExcel = item.EntradaAlmoco - item.SaidaAlmoco;
                    }
                    if (item.HoraSaida != TimeSpan.Zero && item.HoraEntrada != TimeSpan.Zero && item.HoraSaida > item.HoraEntrada)
                    {
                        totalTrabalhadoExcel = (item.HoraSaida - item.HoraEntrada) - tempoAlmocoExcel;
                        if (totalTrabalhadoExcel < TimeSpan.Zero) totalTrabalhadoExcel = TimeSpan.Zero;
                    }

                    if (totalTrabalhadoExcel > TimeSpan.Zero)
                    {
                        worksheet.Cell(currentRow, 7).SetValue(totalTrabalhadoExcel);
                        worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "[h]:mm";
                    }
                    else
                    {
                        worksheet.Cell(currentRow, 7).SetValue(string.Empty); // Célula vazia para zero horas
                    }
                    currentRow++;
                }

                for (int i = 1; i <= 7; i++)
                {
                    worksheet.Column(i).AdjustToContents(10, 60); // Ajusta com min e max width
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, contentType, fileName);
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string? userName = User.Identity?.Name ?? "Usuário desconhecido";
            // HttpContext.SignOutAsync é um método de extensão do namespace Microsoft.AspNetCore.Authentication
            await HttpContext.SignOutAsync("AdminLoginScheme");
            _logger.LogInformation("Usuário '{UserName}' deslogado do AdminLoginScheme. Redirecionando...", userName);
            return RedirectToPage("/Account/AdminLogin", new { area = "Identity" });
        }
    }
}