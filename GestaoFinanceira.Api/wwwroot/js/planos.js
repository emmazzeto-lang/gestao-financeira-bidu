// Planos de trabalho, metas e fontes de recurso

const $ = id => document.getElementById(id);

let parcerias = [];
let planos = [];
let fontes = [];
let metasDoPlano = {};   // { idDoPlano: [metas] }
let planoEscolhido = null;

function aviso(texto) {
  const toast = $('aviso');
  toast.querySelector('.toast-body').textContent = texto;
  bootstrap.Toast.getOrCreateInstance(toast).show();
}

function falhou(erro) {
  console.error(erro);
  aviso(erro.message || 'Não foi possível falar com a API.');
}

const somaMetas = idPlano => (metasDoPlano[idPlano] || []).reduce((t, m) => t + m.valorPrevisto, 0);
const planoAtual = () => planos.find(p => p.id === planoEscolhido);

// ---------- Carregar ----------

async function carregarFontes() {
  fontes = await apiGet('FontesRecurso');

  $('listaFontes').innerHTML = fontes.length
    ? fontes.map(f => `<li class="mb-1"><i class="bi bi-dot"></i>${escaparHtml(f.nome)}</li>`).join('')
    : '<li class="text-secondary">Nenhuma fonte cadastrada</li>';

  $('fonteMeta').innerHTML = '<option value="">Fonte...</option>' +
    fontes.map(f => `<option value="${f.id}">${escaparHtml(f.nome)}</option>`).join('');
}

async function carregarParcerias() {
  parcerias = await apiGet('Parcerias');

  $('parceria').innerHTML = parcerias.length
    ? parcerias.map(p => `<option value="${p.id}">${escaparHtml(`${p.termoFomento} - ${p.nome}`)}</option>`).join('')
    : '<option value="">Nenhuma parceria cadastrada</option>';
    // Começa na parceria escolhida no topo
    if (parcerias.some(p => p.id == parceriaEscolhida())) $('parceria').value = parceriaEscolhida();
}

async function carregarPlanos() {
  const parceriaId = $('parceria').value;
  planos = parceriaId ? await apiGet(`PlanosTrabalho?parceriaId=${parceriaId}`) : [];

  // Busca as metas de todos os planos ao mesmo tempo
  const listas = await Promise.all(planos.map(p => apiGet(`Metas?planoTrabalhoId=${p.id}`)));
  metasDoPlano = {};
  planos.forEach((p, i) => { metasDoPlano[p.id] = listas[i]; });

  // Mantém o plano escolhido; se ele não existir mais, escolhe o primeiro
  if (!planos.some(p => p.id === planoEscolhido)) planoEscolhido = planos[0]?.id ?? null;

  const parceria = parcerias.find(p => p.id == parceriaId);
  $('resumo').textContent = parceria ? `Valor total da parceria: R$ ${moeda(parceria.valorTotal)}` : '';

  desenharPlanos();
  desenharMetas();
}

// ---------- Desenhar ----------

function desenharPlanos() {
  if (!planos.length) {
    $('listaPlanos').innerHTML = '<tr><td colspan="4" class="ausente">Nenhum plano para esta parceria.</td></tr>';
    return;
  }

  $('listaPlanos').innerHTML = planos.map(p => {
    const situacao = p.validado
      ? `<span class="conferencia ok"><i class="bi bi-check2"></i> validado em ${dataCurta(p.dataValidacao)}</span>`
      : '<span class="conferencia pendente"><i class="bi bi-circle"></i> aguardando validação</span>';

    return `
      <tr data-id="${p.id}" class="${p.id === planoEscolhido ? 'selecionada' : ''}" style="cursor: pointer">
        <td><div class="historico">${escaparHtml(p.descricao)}</div></td>
        <td class="valor">${moeda(p.valorAprovado)}</td>
        <td class="valor">${moeda(somaMetas(p.id))}</td>
        <td>${situacao}</td>
      </tr>`;
  }).join('');
}

function desenharMetas() {
  const plano = planoAtual();
  const metas = plano ? metasDoPlano[plano.id] : [];
  const aberto = plano && !plano.validado;

  $('tituloMetas').textContent = plano ? `Metas - ${plano.descricao}` : 'Metas';

  if (!plano) {
    $('situacaoPlano').textContent = 'Crie um plano para cadastrar as metas.';
  } else {
    const falta = plano.valorAprovado - somaMetas(plano.id);
    $('situacaoPlano').textContent = plano.validado
      ? 'Plano validado: as metas não mudam mais.'
      : `Falta distribuir R$ ${moeda(falta)} do valor aprovado.`;
  }

  $('listaMetas').innerHTML = metas.length
    ? metas.map(m => `
      <tr>
        <td><div class="historico">${escaparHtml(m.descricao)}</div></td>
        <td>${escaparHtml(m.fonteRecurso?.nome ?? '-')}</td>
        <td class="valor">${moeda(m.valorPrevisto)}</td>
        <td class="text-end">${aberto
          ? `<button class="btn btn-link btn-sm text-danger" type="button" data-remover="${m.id}" data-perfis="Administrador,Tesouraria" aria-label="Remover meta"><i class="bi bi-trash"></i></button>`
          : ''}</td>
      </tr>`).join('')
    : '<tr><td colspan="4" class="ausente">Nenhuma meta cadastrada.</td></tr>';

  // O formulário e o botão de validar só fazem sentido com o plano ainda aberto
  $('formMeta').dataset.perfis = aberto ? 'Administrador,Tesouraria' : 'nenhum';
  $('validar').dataset.perfis = aberto ? 'Administrador,Diretoria' : 'nenhum';
  aplicarPerfis();
}

// ---------- Ações ----------

$('parceria').addEventListener('change', () => {
  planoEscolhido = null;
  carregarPlanos().catch(falhou);
});

$('listaPlanos').addEventListener('click', e => {
  const linha = e.target.closest('tr[data-id]');
  if (!linha) return;
  planoEscolhido = Number(linha.dataset.id);
  desenharPlanos();
  desenharMetas();
});

$('listaMetas').addEventListener('click', async e => {
  const botao = e.target.closest('[data-remover]');
  if (!botao || !confirm('Remover esta meta?')) return;

  try {
    await apiDelete(`Metas/${botao.dataset.remover}`);
    aviso('Meta removida.');
    await carregarPlanos();
  } catch (erro) {
    falhou(erro);
  }
});

$('formMeta').addEventListener('submit', async e => {
  e.preventDefault();
  const valor = lerMoeda($('valorMeta').value);

  if (!$('descricaoMeta').value.trim() || !$('fonteMeta').value || !(valor > 0)) {
    aviso('Preencha a descrição, a fonte e um valor maior que zero.');
    return;
  }

  try {
    await apiPost('Metas', {
      descricao: $('descricaoMeta').value.trim(),
      valorPrevisto: valor,
      planoTrabalhoId: planoEscolhido,
      fonteRecursoId: Number($('fonteMeta').value)
    });
    e.target.reset();
    aviso('Meta adicionada.');
    await carregarPlanos();
  } catch (erro) {
    falhou(erro);
  }
});

$('validar').addEventListener('click', async () => {
  const plano = planoAtual();
  if (!confirm(`Validar "${plano.descricao}"? Depois disso as metas não podem mais mudar.`)) return;

  try {
    await apiPatch(`PlanosTrabalho/${plano.id}/validar`);
    aviso('Plano validado. A parceria já pode receber aportes.');
    await carregarPlanos();
  } catch (erro) {
    falhou(erro);
  }
});

$('formFonte').addEventListener('submit', async e => {
  e.preventDefault();
  const nome = $('nomeFonte').value.trim();
  if (!nome) return;

  try {
    await apiPost('FontesRecurso', { nome });
    e.target.reset();
    aviso('Fonte cadastrada.');
    await carregarFontes();
  } catch (erro) {
    falhou(erro);
  }
});

$('novoPlano').addEventListener('show.bs.offcanvas', () => {
  const parceria = parcerias.find(p => p.id == $('parceria').value);
  $('parceriaDoPlano').textContent = parceria ? `Parceria ${parceria.termoFomento} - ${parceria.nome}` : '';
});

$('formPlano').addEventListener('submit', async e => {
  e.preventDefault();
  const valor = lerMoeda($('valorPlano').value);

  try {
    if (!$('descricaoPlano').value.trim() || !(valor > 0)) throw new Error('Preencha a descrição e um valor maior que zero.');

    const novo = await apiPost('PlanosTrabalho', {
      descricao: $('descricaoPlano').value.trim(),
      valorAprovado: valor,
      parceriaId: Number($('parceria').value)
    });

    bootstrap.Offcanvas.getInstance($('novoPlano')).hide();
    e.target.reset();
    $('erroPlano').classList.add('d-none');
    planoEscolhido = novo.id;
    aviso('Plano criado. Agora cadastre as metas.');
    await carregarPlanos();
  } catch (erro) {
    $('erroPlano').textContent = erro.message;
    $('erroPlano').classList.remove('d-none');
  }
});

// Formata o valor ao sair do campo: "1500" vira "1.500,00"
['valorMeta', 'valorPlano'].forEach(id => $(id).addEventListener('blur', e => {
  const valor = lerMoeda(e.target.value);
  if (valor > 0) e.target.value = moeda(valor);
}));

Promise.all([carregarFontes(), carregarParcerias()])
  .then(carregarPlanos)
  .catch(falhou);