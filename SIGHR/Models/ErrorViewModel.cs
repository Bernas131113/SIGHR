namespace SIGHR.Models
{
    /// <summary>
    /// ViewModel que representa os dados necessários para a página de erro da aplicação.
    /// É utilizado para passar o identificador do pedido (Request ID) para a View de erro.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// O identificador único do pedido HTTP que causou o erro.
        /// É útil para fins de depuração (debugging) e para correlacionar com os logs do servidor.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Uma propriedade de conveniência que retorna 'true' se o RequestId existir.
        /// É usada na View para decidir se deve ou não exibir o ID do pedido ao utilizador.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}