// wwwroot/js/registoPontoColaborador.js

document.addEventListener('DOMContentLoaded', function () {
    //
    // Bloco de Inicialização e Seleção de Elementos
    // Guarda referências aos elementos do DOM para evitar procurá-los repetidamente.
    //
    const loadingMsg = document.getElementById('loading-message-ponto');
    const successMsg = document.getElementById('success-message-ponto');
    const errorMsg = document.getElementById('error-message-ponto');

    /**
     * Obtém o token anti-falsificação (anti-forgery) do input oculto na página.
     * Essencial para a segurança dos pedidos POST.
     * @returns {string} O valor do token ou uma string vazia se não for encontrado.
     */
    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }


    //
    // Bloco de Funções de Feedback Visual (Notificações)
    // Controla a apresentação de mensagens ao utilizador.
    //

    /**
     * Mostra o indicador de "A processar..." e esconde outras mensagens.
     */
    function showLoading() {
        if (loadingMsg) loadingMsg.style.display = 'block';
        if (successMsg) successMsg.style.display = 'none';
        if (errorMsg) errorMsg.style.display = 'none';
    }

    /**
     * Mostra uma notificação (sucesso ou erro) com uma animação de fade.
     * @param {HTMLElement} element - O elemento da notificação (sucesso ou erro).
     * @param {string} message - A mensagem a ser exibida.
     * @param {number} duration - A duração em milissegundos que a mensagem ficará visível.
     */
    function showNotification(element, message, duration) {
        if (element) {
            element.textContent = message;
            element.style.display = 'block';

            window.requestAnimationFrame(() => {
                element.classList.add('show');
            });

            setTimeout(() => {
                element.classList.remove('show');
                setTimeout(() => {
                    if (!element.classList.contains('show')) {
                        element.style.display = 'none';
                    }
                }, 500);
            }, duration);
        }
    }

    function showSuccess(message) {
        if (loadingMsg) loadingMsg.style.display = 'none';
        showNotification(successMsg, message, 5000); // Mostra por 5 segundos.
    }

    function showError(message) {
        if (loadingMsg) loadingMsg.style.display = 'none';
        showNotification(errorMsg, message, 7000); // Mostra por 7 segundos.
    }


    //
    // Bloco de Comunicação com a API
    //

    /**
     * Função principal que envia o pedido para registar um ponto (Entrada, Saída, etc.).
     * @param {string} actionUrl - A URL da API para a ação específica.
     * @param {string} buttonId - O ID do botão que foi clicado.
     */
    async function registarPonto(actionUrl, buttonId) {
        const button = document.getElementById(buttonId);
        if (button) button.disabled = true;
        showLoading();

        const antiForgeryToken = getAntiForgeryToken();
        if (!antiForgeryToken) {
            showError("Erro de segurança. Por favor, atualize a página.");
            if (button) button.disabled = false;
            return;
        }

        try {
            const response = await fetch(actionUrl, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': antiForgeryToken,
                    'Accept': 'application/json'
                }
            });

            const data = await response.json();

            if (response.ok && data.success) {
                showSuccess(data.message || "Operação realizada com sucesso!");
                fetchPontoDoDia(); // Atualiza a informação do ponto na página.
            } else {
                showError(data.message || `Erro: Não foi possível concluir a operação.`);
            }

        } catch (error) {
            console.error('Erro na requisição de ponto:', error);
            showError('Erro de rede ou ao processar o pedido. Tente novamente.');
        } finally {
            if (button) button.disabled = false;
        }
    }

    /**
     * Pede os dados de ponto do dia atual à API e atualiza os campos na página.
     */
    async function fetchPontoDoDia() {
        const displayEntrada = document.getElementById('displayEntrada');
        const displaySaidaAlmoco = document.getElementById('displaySaidaAlmoco');
        const displayEntradaAlmoco = document.getElementById('displayEntradaAlmoco');
        const displaySaida = document.getElementById('displaySaida');

        if (!displayEntrada) return;

        if (typeof urls === 'undefined' || !urls.getPontoDoDia) {
            console.error("URL 'getPontoDoDia' não está definida.");
            return;
        }

        try {
            const response = await fetch(urls.getPontoDoDia);
            if (response.ok) {
                const data = await response.json();
                displayEntrada.textContent = data.horaEntrada && data.horaEntrada !== "00:00:00" ? data.horaEntrada.substring(0, 5) : '--:--';
                displaySaidaAlmoco.textContent = data.saidaAlmoco && data.saidaAlmoco !== "00:00:00" ? data.saidaAlmoco.substring(0, 5) : '--:--';
                displayEntradaAlmoco.textContent = data.entradaAlmoco && data.entradaAlmoco !== "00:00:00" ? data.entradaAlmoco.substring(0, 5) : '--:--';
                displaySaida.textContent = data.horaSaida && data.horaSaida !== "00:00:00" ? data.horaSaida.substring(0, 5) : '--:--';
            } else {
                console.error("Erro ao obter o ponto do dia:", response.statusText);
            }
        } catch (error) {
            console.error('Erro de rede ao obter o ponto do dia:', error);
        }
    }


    //
    // Bloco de Configuração de Eventos
    // Associa as funções aos cliques dos botões.
    //
    if (typeof urls !== 'undefined') {
        document.getElementById('btnEntrada')?.addEventListener('click', () => registarPonto(urls.registarEntrada, 'btnEntrada'));
        document.getElementById('btnSaidaAlmoco')?.addEventListener('click', () => registarPonto(urls.registarSaidaAlmoco, 'btnSaidaAlmoco'));
        document.getElementById('btnEntradaAlmoco')?.addEventListener('click', () => registarPonto(urls.registarEntradaAlmoco, 'btnEntradaAlmoco'));
        document.getElementById('btnSaida')?.addEventListener('click', () => registarPonto(urls.registarSaida, 'btnSaida'));
    } else {
        console.error("Objeto 'urls' não definido. Os eventos de clique não foram configurados.");
    }
});