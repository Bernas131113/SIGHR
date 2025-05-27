// wwwroot/js/registoPontoColaborador.js

document.addEventListener('DOMContentLoaded', function () {
    const loadingMsg = document.getElementById('loading-message-ponto');
    const successMsg = document.getElementById('success-message-ponto');
    const errorMsg = document.getElementById('error-message-ponto');

    const displayEntrada = document.getElementById('displayEntrada');
    const displaySaidaAlmoco = document.getElementById('displaySaidaAlmoco');
    const displayEntradaAlmoco = document.getElementById('displayEntradaAlmoco');
    const displaySaida = document.getElementById('displaySaida');

    // Função para obter o AntiForgeryToken do input oculto gerado por @Html.AntiForgeryToken()
    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput) {
            return tokenInput.value;
        }
        console.warn('AntiForgeryToken input não encontrado na página. As requisições POST podem falhar.');
        return '';
    }

    function showLoading() {
        if (loadingMsg) loadingMsg.style.display = 'block';
        if (successMsg) successMsg.style.display = 'none';
        if (errorMsg) errorMsg.style.display = 'none';
    }

    function showSuccess(message) {
        if (loadingMsg) loadingMsg.style.display = 'none';
        if (successMsg) {
            successMsg.textContent = message;
            successMsg.style.display = 'block';
            // Esconder a mensagem de sucesso após alguns segundos
            setTimeout(() => { successMsg.style.display = 'none'; }, 5000);
        }
        if (errorMsg) errorMsg.style.display = 'none';
        fetchPontoDoDia();
    }

    function showError(message) {
        if (loadingMsg) loadingMsg.style.display = 'none';
        if (successMsg) successMsg.style.display = 'none';
        if (errorMsg) {
            errorMsg.textContent = message;
            errorMsg.style.display = 'block';
            // Esconder a mensagem de erro após alguns segundos
            setTimeout(() => { errorMsg.style.display = 'none'; }, 7000);
        }
    }

    async function registarPonto(actionUrl, buttonId) {
        const button = document.getElementById(buttonId);
        if (button) button.disabled = true;
        showLoading();

        const antiForgeryToken = getAntiForgeryToken();

        try {
            const response = await fetch(actionUrl, {
                method: 'POST',
                headers: {
                    // Inclui o token no header da requisição
                    'RequestVerificationToken': antiForgeryToken
                    // 'Content-Type': 'application/json' // Adicione se estiver enviando um corpo JSON
                }
                // body: JSON.stringify({ someData: 'value' }) // Adicione se estiver enviando um corpo JSON
            });

            if (response.ok) {
                const message = await response.text();
                showSuccess(message || "Operação realizada com sucesso!");
            } else {
                const errorText = await response.text();
                showError(errorText || `Erro ${response.status}: Não foi possível completar a operação.`);
            }
        } catch (error) {
            console.error('Erro na requisição de ponto:', error);
            showError('Erro de rede ou ao processar o pedido. Tente novamente.');
        } finally {
            if (button) button.disabled = false;
        }
    }

    async function fetchPontoDoDia() {
        if (!displayEntrada || !displaySaidaAlmoco || !displayEntradaAlmoco || !displaySaida) {
            return;
        }
        // As URLs são passadas da view para o script através do objeto global 'urls'
        if (typeof urls === 'undefined' || !urls.getPontoDoDia) {
            console.error("Objeto 'urls' ou 'urls.getPontoDoDia' não definido. Verifique a view .cshtml.");
            return;
        }

        try {
            const response = await fetch(urls.getPontoDoDia); // Usa a URL do objeto 'urls'
            if (response.ok) {
                const data = await response.json();
                displayEntrada.textContent = data.horaEntrada !== "00:00:00" ? data.horaEntrada.substring(0, 5) : '--:--';
                displaySaidaAlmoco.textContent = data.saidaAlmoco !== "00:00:00" ? data.saidaAlmoco.substring(0, 5) : '--:--';
                displayEntradaAlmoco.textContent = data.entradaAlmoco !== "00:00:00" ? data.entradaAlmoco.substring(0, 5) : '--:--';
                displaySaida.textContent = data.horaSaida !== "00:00:00" ? data.horaSaida.substring(0, 5) : '--:--';
            } else {
                showError("Erro ao buscar os registos de ponto do dia.");
                console.error("Erro ao buscar ponto do dia:", response.statusText);
            }
        } catch (error) {
            showError("Erro de rede ao buscar os registos de ponto do dia.");
            console.error('Erro de rede ao buscar ponto do dia:', error);
        }
    }

    // Adicionar listeners aos botões usando as URLs do objeto global 'urls'
    // Verifique se o objeto 'urls' está disponível globalmente antes de usar
    if (typeof urls !== 'undefined') {
        document.getElementById('btnEntrada')?.addEventListener('click', () => registarPonto(urls.registarEntrada, 'btnEntrada'));
        document.getElementById('btnSaidaAlmoco')?.addEventListener('click', () => registarPonto(urls.registarSaidaAlmoco, 'btnSaidaAlmoco'));
        document.getElementById('btnEntradaAlmoco')?.addEventListener('click', () => registarPonto(urls.registarEntradaAlmoco, 'btnEntradaAlmoco'));
        document.getElementById('btnSaida')?.addEventListener('click', () => registarPonto(urls.registarSaida, 'btnSaida'));
    } else {
        console.error("Objeto 'urls' não definido. Não foi possível adicionar event listeners aos botões de ponto.");
    }

    fetchPontoDoDia();
});