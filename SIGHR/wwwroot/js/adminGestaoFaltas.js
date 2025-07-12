// wwwroot/js/adminGestaoFaltas.js

//
// Bloco de Variáveis Globais
// Armazenam as URLs da API e os dados carregados para evitar pedidos repetidos.
//
let API_URL_ADMIN_FALTAS_LISTAR;
let API_URL_ADMIN_FALTAS_EXCLUIR;
let todasAsFaltasAdminCarregadas = [];

/**
 * Função de inicialização, chamada a partir da View.
 * Recebe as URLs da API e configura os eventos de clique dos botões da página.
 * @param {object} urls - Um objeto contendo as URLs 'listar' e 'excluir'.
 */
function inicializarGestaoFaltasAdmin(urls) {
    API_URL_ADMIN_FALTAS_LISTAR = urls.listar;
    API_URL_ADMIN_FALTAS_EXCLUIR = urls.excluir;

    // Associa as funções aos respetivos botões de filtro e exclusão.
    document.querySelector('#gestaoFaltasAdminContent .filter-btn.apply')?.addEventListener('click', aplicarFiltrosFaltasAdmin);
    document.querySelector('#gestaoFaltasAdminContent .filter-btn.clear')?.addEventListener('click', limparFiltrosFaltasAdmin);

    document.getElementById('btn-ativar-excluir-faltas')?.addEventListener('click', ativarModoExclusaoFaltasAdmin);
    document.getElementById('btn-confirmar-excluir-faltas')?.addEventListener('click', excluirFaltasSelecionadasAdmin);
    document.getElementById('btn-cancelar-excluir-faltas')?.addEventListener('click', cancelarModoExclusaoFaltasAdmin);
    document.getElementById('selecionar-todas-faltas')?.addEventListener('change', function () { toggleSelecionarTodasFaltasAdmin(this); });

    // Carrega os dados iniciais na tabela ao entrar na página.
    carregarTodasAsFaltasDaApi();
}


//
// Bloco de Funções de Formatação de Dados
// Converte os dados recebidos da API para um formato legível.
//

/**
 * Formata uma data no formato ISO para "dd/mm/aaaa".
 * @param {string} dataISO - A data em formato ISO (ex: "2023-10-27T00:00:00").
 */
function formatarDataParaAdmin(dataISO) {
    if (!dataISO) return 'N/D';
    try {
        const data = new Date(dataISO);
        return data.toLocaleDateString('pt-PT', { day: '2-digit', month: '2-digit', year: 'numeric' });
    } catch (e) { return 'Data Inválida'; }
}

/**
 * Formata um TimeSpan C# (ex: "1.08:30:00" ou "08:30:00") para "hh:mm".
 * @param {string} horaTimeSpan - O valor do TimeSpan como string.
 */
function formatarHoraParaAdmin(horaTimeSpan) {
    if (!horaTimeSpan || horaTimeSpan === "00:00:00") return '--:--';
    try {
        // Remove a parte do dia, se existir (ex: "1.")
        const partesPrincipais = horaTimeSpan.includes('.') ? horaTimeSpan.split('.')[1] : horaTimeSpan;
        const partes = partesPrincipais.split(':');
        return `${partes[0].padStart(2, '0')}:${partes[1].padStart(2, '0')}`;
    } catch (e) { return 'Hora Inválida'; }
}


//
// Bloco de Comunicação com a API e Renderização da Tabela
//

/**
 * Pede os dados das faltas à API, considerando os filtros aplicados,
 * e depois chama a função para desenhar a tabela.
 */
async function carregarTodasAsFaltasDaApi() {
    const tbody = document.getElementById('tabela-gestao-todas-faltas')?.querySelector('tbody');
    const divNenhuma = document.getElementById('nenhuma-falta-admin-view');
    if (!tbody || !divNenhuma) return;

    tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;">A carregar faltas...</td></tr>';
    divNenhuma.style.display = 'none';

    const filtroNomeVal = document.getElementById('filtro-nome-falta').value;
    const filtroDataVal = document.getElementById('filtro-data-falta').value;
    let url = new URL(API_URL_ADMIN_FALTAS_LISTAR, window.location.origin);
    if (filtroNomeVal) url.searchParams.append('filtroNome', filtroNomeVal);
    if (filtroDataVal) url.searchParams.append('filtroData', filtroDataVal);

    try {
        const response = await fetch(url);
        if (!response.ok) throw new Error(`Erro HTTP: ${response.status}`);
        const data = await response.json();
        todasAsFaltasAdminCarregadas = data;
        renderizarTabelaAdminFaltas(todasAsFaltasAdminCarregadas);
    } catch (error) {
        console.error('Erro ao carregar todas as faltas (Admin):', error);
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; color:red;">Erro ao carregar faltas: ${error.message}</td></tr>`;
    }
}

/**
 * Constrói as linhas da tabela (TRs) com base nos dados recebidos.
 * @param {Array} faltas - O array de objetos de falta vindos da API.
 */
function renderizarTabelaAdminFaltas(faltas) {
    const tbody = document.getElementById('tabela-gestao-todas-faltas').querySelector('tbody');
    const divNenhuma = document.getElementById('nenhuma-falta-admin-view');
    tbody.innerHTML = '';

    if (faltas.length === 0) {
        divNenhuma.style.display = 'block';
    } else {
        divNenhuma.style.display = 'none';
        faltas.forEach(falta => {
            const tr = tbody.insertRow();
            tr.dataset.faltaId = falta.faltaId;
            tr.insertCell().innerHTML = `<input type="checkbox" class="delete-checkbox-row delete-checkbox" value="${falta.faltaId}" onchange="verificarSelecaoParaExcluirFaltasAdmin()">`;
            tr.insertCell().textContent = falta.nomeUtilizador;
            tr.insertCell().textContent = formatarDataParaAdmin(falta.dataFalta);
            tr.insertCell().textContent = formatarHoraParaAdmin(falta.inicio);
            tr.insertCell().textContent = formatarHoraParaAdmin(falta.fim);
            tr.insertCell().textContent = falta.motivo;
            tr.insertCell().textContent = formatarDataParaAdmin(falta.dataRegisto);
        });
    }
    atualizarVisibilidadeCheckboxesAdmin();
}


//
// Bloco de Funções de Filtro
//

function aplicarFiltrosFaltasAdmin() {
    carregarTodasAsFaltasDaApi();
}

function limparFiltrosFaltasAdmin() {
    document.getElementById('filtro-nome-falta').value = '';
    document.getElementById('filtro-data-falta').value = '';
    carregarTodasAsFaltasDaApi();
}


//
// Bloco de Funções para Exclusão em Massa
// Controla a interface para selecionar e excluir múltiplos registos.
//

function ativarModoExclusaoFaltasAdmin() {
    document.querySelectorAll('#tabela-gestao-todas-faltas .delete-checkbox').forEach(cb => cb.style.display = 'inline-block');
    document.getElementById('btn-ativar-excluir-faltas').style.display = 'none';
    document.getElementById('btn-confirmar-excluir-faltas').style.display = 'inline-block';
    document.getElementById('btn-cancelar-excluir-faltas').style.display = 'inline-block';
    document.getElementById('btn-confirmar-excluir-faltas').disabled = true;
    document.getElementById('selecionar-todas-faltas').style.display = 'inline-block';
}

function cancelarModoExclusaoFaltasAdmin() {
    document.querySelectorAll('#tabela-gestao-todas-faltas .delete-checkbox').forEach(cb => { cb.style.display = 'none'; cb.checked = false; });
    document.getElementById('btn-ativar-excluir-faltas').style.display = 'inline-block';
    document.getElementById('btn-confirmar-excluir-faltas').style.display = 'none';
    document.getElementById('btn-cancelar-excluir-faltas').style.display = 'none';
    document.getElementById('selecionar-todas-faltas').checked = false;
    document.getElementById('selecionar-todas-faltas').style.display = 'none';
}

function toggleSelecionarTodasFaltasAdmin(selectAllCheckbox) {
    document.querySelectorAll('#tabela-gestao-todas-faltas .delete-checkbox-row').forEach(cb => { cb.checked = selectAllCheckbox.checked; });
    verificarSelecaoParaExcluirFaltasAdmin();
}

function verificarSelecaoParaExcluirFaltasAdmin() {
    const algumSelecionado = Array.from(document.querySelectorAll('#tabela-gestao-todas-faltas .delete-checkbox-row:checked')).length > 0;
    document.getElementById('btn-confirmar-excluir-faltas').disabled = !algumSelecionado;
    const todasLinhas = document.querySelectorAll('#tabela-gestao-todas-faltas tbody .delete-checkbox-row');
    if (todasLinhas.length > 0) {
        document.getElementById('selecionar-todas-faltas').checked = Array.from(todasLinhas).every(cb => cb.checked);
    } else {
        document.getElementById('selecionar-todas-faltas').checked = false;
    }
}

async function excluirFaltasSelecionadasAdmin() {
    const checkboxes = document.querySelectorAll('#tabela-gestao-todas-faltas .delete-checkbox-row:checked');
    const ids = Array.from(checkboxes).map(cb => parseInt(cb.value));
    if (ids.length === 0 || !confirm(`Excluir ${ids.length} falta(s) selecionada(s)?`)) return;

    try {
        const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const headers = { 'Content-Type': 'application/json' };
        if (antiForgeryToken) headers['RequestVerificationToken'] = antiForgeryToken;

        const response = await fetch(API_URL_ADMIN_FALTAS_EXCLUIR, {
            method: 'POST', headers: headers, body: JSON.stringify(ids)
        });
        if (response.ok) {
            alert('Falta(s) excluída(s) com sucesso!');
            carregarTodasAsFaltasDaApi();
            cancelarModoExclusaoFaltasAdmin();
        } else {
            const err = await response.json().catch(() => ({ message: response.statusText }));
            alert(`Erro ao excluir: ${err.message}`);
        }
    } catch (error) {
        console.error('Erro ao excluir faltas (Admin):', error);
        alert('Erro de comunicação ao excluir faltas.');
    }
}

function atualizarVisibilidadeCheckboxesAdmin() {
    const emModoExclusao = document.getElementById('btn-confirmar-excluir-faltas')?.style.display !== 'none';
    document.querySelectorAll('#tabela-gestao-todas-faltas .delete-checkbox-row').forEach(cb => {
        cb.style.display = emModoExclusao ? 'inline-block' : 'none';
    });
    document.getElementById('selecionar-todas-faltas').style.display = emModoExclusao ? 'inline-block' : 'none';
    if (emModoExclusao) verificarSelecaoParaExcluirFaltasAdmin();
}