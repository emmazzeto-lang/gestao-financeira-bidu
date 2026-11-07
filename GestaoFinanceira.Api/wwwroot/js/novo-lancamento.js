// Lançamento de despesa ligado à API: a despesa só fica gravada junto com o comprovante

const $ = id => document.getElementById(id);

const campos = {
  parceria: $('parceria'), plano: $('plano'), fonte: $('fonte'), meta: $('meta'),
  fornecedor: $('fornecedor'), cnpj: $('cnpj'), conta: $('conta'), data: $('data'),
  competencia: $('competencia'), valor: $('valor'), historico: $('historico'),
  comprovante: $('comprovante'), tipoDocumento: $('tipoDocumento'), numeroDocumento: $('numeroDocumento')
};

let planos = [];
let metas = [];
let fornecedores = [];
let arquivo = null;
let docUsadoEm = null;
let salvando = false;

function opcoes(select, itens, textoVazio) {
  select.innerHTML = itens.length
    ? itens.map(i => `<option value="${i.id}">${escaparHtml(i.texto)}</option>`).join('')
    : `<option value="">${textoVazio}</option>`;
}

function planoAtual() { return planos.find(p => p.id == campos.plano.value); }
function metaAtual() { return metas.find(m => m.id == campos.meta.value); }
function fornecedorAtual() { return fornecedores.find(f => f.id == campos.fornecedor.value); }

// "75315333000109" -> "75.315.333/0001-09"
function formatarDocumento(numeros) {
  if (!numeros) return '';
  return numeros.length === 14
    ? numeros.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, '$1.$2.$3/$4-$5')
    : numeros.replace(/^(\d{3})(\d{3})(\d{3})(\d{2})$/, '$1.$2.$3-$4');
}

// ---------- Cadastros vindos da API ----------

async function carregarCadastros() {
  const [parcerias, listaFornecedores, contas] = await Promise.all([
    apiGet('Parcerias'),
    apiGet('Fornecedores'),
    apiGet('ContasBancarias')
  ]);

  fornecedores = listaFornecedores;

  opcoes(campos.parceria,
    parcerias.map(p => ({ id: p.id, texto: `${p.termoFomento} - ${p.nome}` })),
    'Nenhuma parceria cadastrada');

  campos.fornecedor.innerHTML = '<option value="">Sem fornecedor</option>' +
    fornecedores.map(f => `<option value="${f.id}">${escaparHtml(f.nome)}</option>`).join('');

  opcoes(campos.conta,
    contas.filter(c => c.ativo).map(c => ({ id: c.id, texto: `${c.nomeBanco} - Ag ${c.agencia} - CC ${c.numeroConta}` })),
    'Nenhuma conta cadastrada');

  await atualizarPlanos();
}

async function atualizarPlanos() {
  planos = campos.parceria.value
    ? await apiGet(`PlanosTrabalho?parceriaId=${campos.parceria.value}`)
    : [];

  opcoes(campos.plano,
    planos.map(p => ({ id: p.id, texto: `${p.descricao} - ${p.validado ? 'validado' : 'aguardando validação'}` })),
    'Nenhum plano');

  await atualizarMetas();
}

// Busca as metas do plano com o quanto já foi gasto em cada uma
async function atualizarMetas() {
  const plano = planoAtual();
  metas = plano?.validado ? await apiGet(`Metas/execucao?planoTrabalhoId=${plano.id}`) : [];

  // As fontes saem das próprias metas do plano, sem repetir
  const fontes = [...new Map(metas.map(m => [m.fonteRecursoId, { id: m.fonteRecursoId, texto: m.fonteRecurso }])).values()];

  const fonteEscolhida = campos.fonte.value;
  opcoes(campos.fonte, fontes, plano ? 'Plano ainda não validado' : '-');
  if (fontes.some(f => f.id == fonteEscolhida)) campos.fonte.value = fonteEscolhida;

  atualizarListaMetas();
}

function atualizarListaMetas() {
  const daFonte = metas.filter(m => m.fonteRecursoId == campos.fonte.value);

  const metaEscolhida = campos.meta.value;
  opcoes(campos.meta, daFonte.map(m => ({ id: m.id, texto: m.descricao })), '-');
  if (daFonte.some(m => m.id == metaEscolhida)) campos.meta.value = metaEscolhida;

  conferir();
}

// ---------- Comprovante ----------

function mostrarArquivo() {
  const area = $('arquivoAnexado');
  if (!arquivo) {
    area.innerHTML = '';
    return;
  }

  const kb = Math.max(1, Math.round(arquivo.size / 1024));
  area.innerHTML = `
    <div class="arquivo">
      <i class="bi bi-file-earmark-text"></i>
      <div class="flex-grow-1 text-truncate">
        <div class="nome text-truncate">${escaparHtml(arquivo.name)}</div>
        <small>${kb} KB - enviado junto com a despesa</small>
      </div>
      <button class="btn btn-link btn-sm" type="button" id="removerArquivo" aria-label="Remover arquivo"><i class="bi bi-x-lg"></i></button>
    </div>`;

  $('removerArquivo').addEventListener('click', () => {
    arquivo = null;
    campos.comprovante.value = '';
    mostrarArquivo();
    conferir();
  });
}

function receberArquivo(novo) {
  if (!novo) return;

  const tiposAceitos = ['application/pdf', 'image/jpeg', 'image/png'];
  if (!tiposAceitos.includes(novo.type) || novo.size > 10 * 1024 * 1024) {
    mostrarAviso('Envie um PDF, JPG ou PNG de até 10 MB.');
    return;
  }

  arquivo = novo;
  mostrarArquivo();
  conferir();
}

// Pergunta à API se o documento já comprova outra despesa.
// Espera a pessoa parar de digitar para não chamar a API a cada tecla.
let esperaDocumento;

function verificarDocumento() {
  clearTimeout(esperaDocumento);
  docUsadoEm = null;

  const numero = campos.numeroDocumento.value.trim();
  if (!numero) {
    conferir();
    return;
  }

  esperaDocumento = setTimeout(async () => {
    const parametros = new URLSearchParams({ tipo: campos.tipoDocumento.value, numero });
    if (campos.fornecedor.value) parametros.append('fornecedorId', campos.fornecedor.value);

    try {
      const resposta = await apiGet(`Documentos/repetido?${parametros}`);
      // só vale se o número não mudou enquanto a API respondia
      if (campos.numeroDocumento.value.trim() === numero) docUsadoEm = resposta.despesaId;
    } catch (erro) {
      console.error(erro);
    }
    conferir();
  }, 400);
}

// ---------- Conferência ----------

function conferir() {
  const plano = planoAtual();
  const meta = metaAtual();
  const valor = lerMoeda(campos.valor.value);
  const numeroDoc = campos.numeroDocumento.value.trim();

  const disponivelAntes = meta ? meta.valorPrevisto - meta.executado : 0;

  const itens = [
    { ok: !!plano?.validado, texto: plano?.validado ? 'Parceria com plano de trabalho validado' : 'O plano de trabalho desta parceria ainda não foi validado' },
    { ok: !!meta, texto: 'Meta pertence ao plano selecionado' },
    { ok: !!meta && meta.fonteRecursoId == campos.fonte.value, texto: 'Fonte de recurso cobre a meta' },
    {
      ok: !!arquivo && !!numeroDoc && !docUsadoEm,
      erro: !!docUsadoEm,
      texto: docUsadoEm
        ? `Comprovante único: documento ${numeroDoc} já usado na despesa Nº ${docUsadoEm}`
        : 'Comprovante anexado e numerado'
    },
    {
      ok: valor > 0 && !!meta && valor <= disponivelAntes,
      erro: valor > disponivelAntes && !!meta,
      texto: valor > disponivelAntes && meta ? 'O valor passa do saldo disponível na meta' : 'Valor cabe no saldo da meta'
    }
  ];

  $('checagens').innerHTML = itens.map(i => {
    const classe = i.ok ? 'ok' : i.erro ? 'erro' : '';
    const icone = i.ok ? 'bi-check2' : i.erro ? 'bi-exclamation-circle' : 'bi-circle';
    return `<li class="${classe}"><i class="bi ${icone}"></i><span>${escaparHtml(i.texto)}</span></li>`;
  }).join('');

  const passaram = itens.filter(i => i.ok).length;
  const placar = $('placarConferencia');
  placar.className = `conferencia ${passaram === itens.length ? 'ok' : itens.some(i => i.erro) ? 'falta' : 'pendente'}`;
  placar.innerHTML = `<i class="bi ${passaram === itens.length ? 'bi-check2' : 'bi-exclamation-circle'}"></i> ${passaram} de ${itens.length}`;

  const aviso = $('avisoDuplicado');
  if (docUsadoEm) {
    aviso.querySelector('span').innerHTML =
      `Este documento já comprova a despesa <b>Nº ${docUsadoEm}</b>. Anexe o documento correto ou confira a despesa anterior.`;
    aviso.classList.remove('d-none');
  } else {
    aviso.classList.add('d-none');
  }

  $('tituloEfeito').textContent = meta ? `Efeito na ${meta.descricao.split(' - ')[0]}` : 'Efeito na meta';
  if (meta) {
    const esta = valor > 0 ? valor : 0;
    const depois = meta.valorPrevisto - meta.executado - esta;
    const pct = (meta.executado + esta) / meta.valorPrevisto * 100;
    $('efeitoPrevisto').textContent = moeda(meta.valorPrevisto);
    $('efeitoExecutado').textContent = `- ${moeda(meta.executado)}`;
    $('efeitoEsta').textContent = `- ${moeda(esta)}`;
    $('efeitoDisponivel').textContent = moeda(depois);
    $('efeitoBarra').style.width = `${Math.min(pct, 100)}%`;
    $('efeitoPercentual').textContent = `${pct.toFixed(1).replace('.', ',')}% executado após o registro`;
  } else {
    ['efeitoPrevisto', 'efeitoExecutado', 'efeitoEsta', 'efeitoDisponivel'].forEach(id => $(id).textContent = '-');
    $('efeitoBarra').style.width = '0';
    $('efeitoPercentual').textContent = '';
  }

  const camposPreenchidos = campos.data.value && campos.competencia.value && campos.historico.value.trim() && campos.conta.value;
  $('registrar').disabled = salvando || !(passaram === itens.length && camposPreenchidos);
}

function mostrarAviso(texto) {
  const toast = $('aviso');
  toast.querySelector('.toast-body').textContent = texto;
  bootstrap.Toast.getOrCreateInstance(toast).show();
}

function limparFormulario() {
  campos.valor.value = '';
  campos.historico.value = '';
  campos.numeroDocumento.value = '';
  campos.comprovante.value = '';
  arquivo = null;
  docUsadoEm = null;
  mostrarArquivo();
}

// ---------- Eventos ----------

function avisarFalha(erro) {
  console.error(erro);
  mostrarAviso('Não foi possível carregar os dados. Verifique se a API e o banco estão rodando.');
}

campos.parceria.addEventListener('change', () => atualizarPlanos().catch(avisarFalha));
campos.plano.addEventListener('change', () => atualizarMetas().catch(avisarFalha));
campos.fonte.addEventListener('change', atualizarListaMetas);

campos.fornecedor.addEventListener('change', () => {
  campos.cnpj.value = formatarDocumento(fornecedorAtual()?.cpfCnpj);
  verificarDocumento();
});

campos.tipoDocumento.addEventListener('change', verificarDocumento);
campos.numeroDocumento.addEventListener('input', verificarDocumento);

['meta', 'data', 'competencia', 'valor', 'historico', 'conta'].forEach(nome =>
  campos[nome].addEventListener('input', conferir));

campos.valor.addEventListener('blur', () => {
  const valor = lerMoeda(campos.valor.value);
  if (valor > 0) campos.valor.value = moeda(valor);
});

campos.comprovante.addEventListener('change', () => receberArquivo(campos.comprovante.files[0]));

const areaEnvio = $('areaEnvio');
areaEnvio.addEventListener('dragover', e => { e.preventDefault(); areaEnvio.classList.add('arrastando'); });
areaEnvio.addEventListener('dragleave', () => areaEnvio.classList.remove('arrastando'));
areaEnvio.addEventListener('drop', e => {
  e.preventDefault();
  areaEnvio.classList.remove('arrastando');
  receberArquivo(e.dataTransfer.files[0]);
});

$('formLancamento').addEventListener('submit', async e => {
  e.preventDefault();
  if ($('registrar').disabled) return;

  salvando = true;
  conferir();
  $('statusEnvio').textContent = 'Gravando...';

  const despesa = {
    descricao: campos.historico.value.trim(),
    valor: lerMoeda(campos.valor.value),
    dataPagamento: `${campos.data.value}T00:00:00Z`,
    contaBancariaId: Number(campos.conta.value),
    planoTrabalhoId: Number(campos.plano.value),
    metaId: Number(campos.meta.value),
    fornecedorId: campos.fornecedor.value ? Number(campos.fornecedor.value) : null
  };

  let salva = null;

  try {
    salva = await apiPost('Despesas', despesa);

    const envio = new FormData();
    envio.append('despesaId', salva.id);
    envio.append('tipo', campos.tipoDocumento.value);
    envio.append('numero', campos.numeroDocumento.value.trim());
    envio.append('arquivo', arquivo);

    await apiEnviarArquivo('Documentos', envio);

    mostrarAviso(`Despesa Nº ${salva.id} registrada com o comprovante.`);
    limparFormulario();
    await atualizarMetas();
  } catch (erro) {
    // Despesa sem comprovante não pode ficar: desfaz o que já foi gravado
    if (salva) await apiDelete(`Despesas/${salva.id}`).catch(console.error);
    mostrarAviso(`Não foi possível registrar: ${erro.message}`);
  } finally {
    salvando = false;
    $('statusEnvio').textContent = '';
    conferir();
  }
});

campos.data.value = new Date().toISOString().slice(0, 10);
campos.competencia.value = new Date().toISOString().slice(0, 7);
carregarCadastros().catch(avisarFalha);