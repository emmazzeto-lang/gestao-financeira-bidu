// Monta o menu lateral, a barra do topo e a barra inferior do celular.
// Cada página informa qual item está ativo em <body data-pagina="...">.

const MENU = [
  { itens: [{ id: 'painel', texto: 'Painel', icone: 'bi-grid-1x2', link: 'painel.html' }] },
  {
    grupo: 'Cadastros',
    itens: [
      { id: 'parcerias', texto: 'Parcerias', icone: 'bi-people', link: 'parcerias.html' },
      { id: 'planos', texto: 'Planos de trabalho', icone: 'bi-clipboard-check', link: '#' },
      { id: 'metas', texto: 'Fontes e metas', icone: 'bi-bullseye', link: '#' },
      { id: 'fornecedores', texto: 'Fornecedores', icone: 'bi-truck', link: '#' },
      { id: 'contas', texto: 'Contas bancárias', icone: 'bi-bank', link: '#' }
    ]
  },
  {
    grupo: 'Movimento',
    itens: [
      { id: 'receitas', texto: 'Receitas', icone: 'bi-arrow-down-left', link: '#' },
      { id: 'despesas', texto: 'Despesas', icone: 'bi-arrow-up-right', link: 'novo-lancamento.html', contador: 7 },
      { id: 'pagamentos', texto: 'Pagamentos', icone: 'bi-cash-stack', link: '#' },
      { id: 'conciliacao', texto: 'Conciliação', icone: 'bi-arrow-left-right', link: 'conciliacao.html', contador: 12 }
    ]
  },
  {
    grupo: 'Documentos',
    itens: [{ id: 'notas', texto: 'Notas e comprovantes', icone: 'bi-file-earmark-text', link: '#' }]
  },
  {
    grupo: 'Prestação',
    itens: [{ id: 'prestacao', texto: 'Relatórios e prestação', icone: 'bi-journal-text', link: 'prestacao-contas.html' }]
  }
];

function montarMenu(paginaAtual) {
  let html = '';

  for (const bloco of MENU) {
    if (bloco.grupo) html += `<div class="menu-grupo">${bloco.grupo}</div>`;

    for (const item of bloco.itens) {
      const ativo = item.id === paginaAtual ? ' ativo' : '';
      const contador = item.contador ? `<span class="contador">${item.contador}</span>` : '';
      const atual = ativo ? ' aria-current="page"' : '';
      html += `<a class="menu-item${ativo}" href="${item.link}"${atual}>
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
          <div class="avatar">EM</div>
          <div><strong>Emerson Mazzeto</strong><small>Tesouraria</small></div>
        </div>
      </div>
    </aside>`;
}

function montarTopo() {
  return `
    <header class="topo">
      <button class="btn btn-outline-secondary btn-icone d-lg-none" type="button"
              data-bs-toggle="offcanvas" data-bs-target="#menuLateral" aria-label="Abrir menu">
        <i class="bi bi-list"></i>
      </button>
      <button class="seletor-parceria" type="button">
        <small>Parceria</small> TF 012/2025 <i class="bi bi-chevron-down"></i>
      </button>
      <div class="ms-auto"></div>
      <div class="busca input-group">
        <span class="input-group-text bg-transparent border-end-0"><i class="bi bi-search"></i></span>
        <input class="form-control border-start-0 ps-0" type="search" placeholder="Buscar por Nº, fornecedor ou valor" aria-label="Buscar">
      </div>
      <button class="btn btn-outline-secondary btn-icone" type="button" aria-label="Notificações">
        <i class="bi bi-bell"></i>
      </button>
    </header>`;
}

function montarBarraInferior(paginaAtual) {
  const item = (id, link, icone, texto) =>
    `<a class="${id === paginaAtual ? 'ativo' : ''}" href="${link}"><i class="bi ${icone}"></i>${texto}</a>`;

  return `
    <nav class="barra-inferior d-lg-none" aria-label="Atalhos">
      ${item('painel', 'painel.html', 'bi-grid-1x2', 'Painel')}
      ${item('parcerias', 'parcerias.html', 'bi-people', 'Parcerias')}
      <a class="lancar" href="novo-lancamento.html" aria-label="Novo lançamento"><i class="bi bi-plus-lg"></i></a>
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

  // links de módulos que ainda não foram feitos
  document.querySelectorAll('.menu-item[href="#"]').forEach(link => {
    link.title = 'Em desenvolvimento';
    link.addEventListener('click', e => e.preventDefault());
  });
});
