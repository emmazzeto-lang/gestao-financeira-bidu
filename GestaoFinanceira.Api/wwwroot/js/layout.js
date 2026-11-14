// Monta o menu lateral, a barra do topo e a barra inferior do celular.
// Cada página informa qual item está ativo em <body data-pagina="...">.

const MENU = [
  { itens: [{ id: 'painel', texto: 'Painel', icone: 'bi-grid-1x2', link: 'painel.html' }] },
  {
    grupo: 'Cadastros',
    itens: [
      { id: 'parcerias', texto: 'Parcerias', icone: 'bi-people', link: 'parcerias.html' },
      { id: 'planos', texto: 'Planos e metas', icone: 'bi-clipboard-check', link: 'planos.html' },
      { id: 'fornecedores', texto: 'Fornecedores', icone: 'bi-truck', link: 'fornecedores.html' },
      { id: 'contas', texto: 'Contas bancárias', icone: 'bi-bank', link: 'contas.html' }
    ]
  },
  {
    grupo: 'Movimento',
    itens: [
      { id: 'receitas', texto: 'Receitas', icone: 'bi-arrow-down-left', link: 'receitas.html' },
      { id: 'despesas', texto: 'Despesas', icone: 'bi-arrow-up-right', link: 'novo-lancamento.html', perfis: 'Administrador,Tesouraria' },
      { id: 'conciliacao', texto: 'Conciliação', icone: 'bi-arrow-left-right', link: 'conciliacao.html', contador: 'aConciliar' }
    ]
  },
  {
    grupo: 'Documentos',
    itens: [{ id: 'notas', texto: 'Notas e comprovantes', icone: 'bi-file-earmark-text', link: 'comprovantes.html', contador: 'semComprovante' }]
  },
  {
    grupo: 'Prestação',
    itens: [{ id: 'prestacao', texto: 'Relatórios e prestação', icone: 'bi-journal-text', link: 'prestacao-contas.html' }]
  },
  {
    grupo: 'Administração',
    perfis: 'Administrador',
    itens: [{ id: 'usuarios', texto: 'Usuários', icone: 'bi-person-gear', link: 'usuarios.html', perfis: 'Administrador' }]
  }
];

function montarMenu(paginaAtual) {
  let html = '';

  for (const bloco of MENU) {
    if (bloco.grupo) {
      const perfis = bloco.perfis ? ` data-perfis="${bloco.perfis}" hidden` : '';
      html += `<div class="menu-grupo"${perfis}>${bloco.grupo}</div>`;
    }

    for (const item of bloco.itens) {
      const ativo = item.id === paginaAtual ? ' ativo' : '';
      // O número vem da API depois do login; até lá o contador fica escondido
      const contador = item.contador ? `<span class="contador" data-contador="${item.contador}" hidden></span>` : '';
      const atual = ativo ? ' aria-current="page"' : '';
      const perfis = item.perfis ? ` data-perfis="${item.perfis}" hidden` : '';
      html += `<a class="menu-item${ativo}" href="${item.link}"${atual}${perfis}>
        <i class="bi ${item.icone}"></i><span>${item.texto}</span>${contador}
      </a>`;
    }
  }

  return `
    <aside class="lateral offcanvas-lg offcanvas-start" tabindex="-1" id="menuLateral" aria-label="Menu principal">
      <div class="offcanvas-body">
        <a class="marca" href="painel.html">
          <img src="img/logo.svg" width="34" height="29" alt="">
          <span><small>Fundação Amigos de</small><strong>João Bidu</strong></span>
        </a>
        <nav class="d-flex flex-column">${html}</nav>
        <div class="usuario">
          <div class="avatar" id="usuarioIniciais"></div>
          <div class="flex-grow-1 text-truncate"><strong id="usuarioNome"></strong><small id="usuarioPerfil"></small></div>
          <button class="btn btn-link sair" type="button" id="sair" title="Sair" aria-label="Sair"><i class="bi bi-box-arrow-right"></i></button>
        </div>
      </div>
    </aside>`;
}

function montarTopo() {
  return `
    <header class="topo">
      <label class="seletor-parceria">
        <small>Parceria</small>
        <select id="parceriaTopo" aria-label="Parceria"></select>
      </label>
      <div class="ms-auto"></div>
      <div class="busca input-group">
        <span class="input-group-text bg-transparent border-end-0"><i class="bi bi-search"></i></span>
        <input id="buscaTopo" class="form-control border-start-0 ps-0" type="search" placeholder="Buscar por Nº, fornecedor ou valor" aria-label="Buscar">
      </div>
    </header>`;
}

function montarBarraInferior(paginaAtual) {
  const item = (id, link, icone, texto) =>
    `<a class="${id === paginaAtual ? 'ativo' : ''}" href="${link}"><i class="bi ${icone}"></i>${texto}</a>`;

  return `
    <nav class="barra-inferior d-lg-none" aria-label="Atalhos">
      ${item('painel', 'painel.html', 'bi-grid-1x2', 'Painel')}
      ${item('parcerias', 'parcerias.html', 'bi-people', 'Parcerias')}
      <a class="lancar" href="novo-lancamento.html" aria-label="Novo lançamento" data-perfis="Administrador,Tesouraria"><i class="bi bi-plus-lg"></i></a>
      ${item('conciliacao', 'conciliacao.html', 'bi-arrow-left-right', 'Conciliar')}
      ${item('prestacao', 'prestacao-contas.html', 'bi-journal-text', 'Prestação')}
    </nav>`;
}

document.addEventListener('DOMContentLoaded', () => {
  const pagina = document.body.dataset.pagina;
  const app = document.querySelector('.app');
  const conteudo = document.querySelector('.conteudo');
  if (!app || !conteudo) return;

  app.insertAdjacentHTML('afterbegin', montarMenu(pagina));
  conteudo.insertAdjacentHTML('afterbegin', montarTopo());
  document.body.insertAdjacentHTML('beforeend', montarBarraInferior(pagina));
  mostrarUsuario();

  // links de módulos que ainda não foram feitos
  document.querySelectorAll('.menu-item[href="#"]').forEach(link => {
    link.title = 'Em desenvolvimento';
    link.addEventListener('click', e => e.preventDefault());
  });
});


// "Emerson Mazzeto" -> "EM"
function iniciais(nome) {
  const partes = nome.trim().split(/\s+/);
  return (partes[0][0] + (partes.length > 1 ? partes[partes.length - 1][0] : '')).toUpperCase();
}

let usuarioLogado = null;

// Esconde o que o perfil não pode usar. A API também recusa: isto só evita botões inúteis.
// Telas que desenham botões depois de carregar chamam esta função de novo.
function aplicarPerfis() {
  if (!usuarioLogado) return;

  document.querySelectorAll('[data-perfis]').forEach(elemento => {
    elemento.hidden = !elemento.dataset.perfis.split(',').includes(usuarioLogado.perfil);
  });
}

// Pergunta à API quem está logado; sem sessão, volta para o login
async function mostrarUsuario() {
  const resposta = await fetch('/api/Auth/eu');

  if (resposta.status === 401) {
    window.location.href = 'index.html';
    return;
  }

  usuarioLogado = await resposta.json();
  document.getElementById('usuarioIniciais').textContent = iniciais(usuarioLogado.nome);
  document.getElementById('usuarioNome').textContent = usuarioLogado.nome;
  document.getElementById('usuarioPerfil').textContent = usuarioLogado.perfil;

  aplicarPerfis();
  carregarContadores();
  carregarParceriasTopo();
  // A busca do topo procura nas despesas, na tela de notas e comprovantes
  document.getElementById('buscaTopo').addEventListener('keydown', e => {
    const texto = e.target.value.trim();
    if (e.key !== 'Enter' || !texto) return;
    window.location.href = `comprovantes.html?busca=${encodeURIComponent(texto)}`;
  });

  document.getElementById('sair').addEventListener('click', async () => {
    await fetch('/api/Auth/sair', { method: 'POST' });
    window.location.href = 'index.html';
  });
}


// ---------- Contadores do menu ----------

async function carregarContadores() {
  const resposta = await fetch('/api/Painel/contadores');
  if (!resposta.ok) return;

  const contadores = await resposta.json();

  // Cada <span data-contador="..."> mostra o número com o mesmo nome; zero fica escondido
  document.querySelectorAll('[data-contador]').forEach(span => {
    const valor = contadores[span.dataset.contador];
    span.textContent = valor;
    span.hidden = !valor;
  });
}

// ---------- Parceria escolhida no topo ----------

// Fica guardada no navegador, para valer em todas as telas
function parceriaEscolhida() {
  return localStorage.getItem('parceria');
}

async function carregarParceriasTopo() {
  const resposta = await fetch('/api/Parcerias');
  if (!resposta.ok) return;

  const parcerias = await resposta.json();
  const seletor = document.getElementById('parceriaTopo');

  seletor.innerHTML = parcerias
    .map(p => `<option value="${p.id}">${escaparHtml(p.termoFomento)}</option>`).join('');

  // Se nada foi escolhido ainda, ou a parceria não existe mais, fica a primeira
  if (parcerias.length && !parcerias.some(p => p.id == parceriaEscolhida()))
    localStorage.setItem('parceria', parcerias[0].id);

  seletor.value = parceriaEscolhida();
  avisarParceria();

  seletor.addEventListener('change', () => {
    localStorage.setItem('parceria', seletor.value);
    avisarParceria();
  });
}

// A tela aberta escuta este evento para recarregar com a parceria escolhida
function avisarParceria() {
  document.dispatchEvent(new CustomEvent('parceriaEscolhida', { detail: parceriaEscolhida() }));
}
