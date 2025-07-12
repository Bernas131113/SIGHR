using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SIGHR.Models;

namespace SIGHR.Controllers
{
    /// <summary>
    /// Controlador principal para as páginas públicas e genéricas da aplicação,
    /// como a página inicial (Homepage) e a página de erro.
    /// </summary>
    public class HomeController : Controller
    {
        // O serviço de logging, injetado no construtor.
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Action para a página principal (Homepage) da aplicação.
        /// </summary>
        /// <returns>A View 'Index'.</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Action para a página de Política de Privacidade.
        /// </summary>
        /// <returns>A View 'Privacy'.</returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Action para a página de erro.
        /// Apresenta um identificador único do pedido para fins de depuração (debugging).
        /// </summary>
        // O atributo [ResponseCache] impede que esta página seja guardada na cache do browser,
        // garantindo que o utilizador veja sempre a informação de erro mais recente.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}