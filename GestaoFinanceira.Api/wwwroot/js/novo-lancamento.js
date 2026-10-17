// Dados de exemplo enquanto a API não tem os endpoints de planos, metas e despesas.
const cadastro = {
  parcerias: [
    {
      id: 1, nome: 'TF 012/2025 - Prefeitura Municipal',
      planos: [{
        id: 1, nome: 'Plano 2025/2027', validado: true, validadoEm: '12/02/2025',
        fontes: [
          { id: 1, nome: 'Recurso público municipal', metas: [1, 2] },
          { id: 2, nome: 'Recursos próprios', metas: [3] },
          { id: 3, nome: 'Doações privadas', metas: [4] }
        ],
        metas: [
          { id: 1, nome: 'Meta 1 - Alimentação', previsto: 720000, executado: 561600 },
          { id: 2, nome: 'Meta 2 - Educação', previsto: 540000, executado: 291600 },
          { id: 3, nome: 'Meta 3 - Saúde', previsto: 300000, executado: 273000 },
          { id: 4, nome: 'Meta 4 - Infraestrutura', previsto: 240000, executado: 76800 }
        ]
      }]
    },
    {
      id: 2, nome: 'CV 021/2026 - Fundo Municipal da Criança',
      planos: [{ id: 2, nome: 'Plano 2026/2027', validado: false, fontes: [], metas: [] }]
    }
  ],
  documentosUsados: { '004512': 1041, '88120': 1038 }
};

const $ = id => document.getElementById(id);

const campos = {
  parceria: $('parceria'), plano: $('plano'), fonte: $('fonte'), meta: $('meta'),
  fornecedor: $('fornecedor'), cnpj: $('cnpj'), data: $('data'), competencia: $('competencia'),
  valor: $('valor'), historico: $('historico'), comprovante: $('comprovante'), numeroDocumento: $('numeroDocumento')
};

let arquivo = null;

function opcoes(select, itens, textoVazio) {
  select.innerHTML = itens.length
    ? itens.map(i => `<option value="${i.id}">${escaparHtml(i.nome)}${i.validado === false ? ' - aguardando validação' : i.validado ? ' - validado' : ''}</option>`).join('')
    : `<option value="">${textoVazio}</option>`;
}

function parceriaAtual() { return cadastro.parcerias.find(p => p.id == campos.parceria.value); }
function planoAtual() { return parceriaAtual()?.planos.find(p => p.id == campos.plano.value); }
function fonteAtual() { return planoAtual()?.fontes.find(f => f.id == campos.fonte.value); }
function metaAtual() { return planoAtual()?.metas.find(m => m.id == campos.meta.value); }

function atualizarPlanos() {
  opcoes(campos.plano, parceriaAtual()?.planos ?? [], 'Nenhum plano');
  atualizarFontes();
}

function atualizarFontes() {
  const plano = planoAtual();
  opcoes(campos.fonte, plano?.validado ? plano.fontes : [], 'Plano ainda não validado');
  atualizarMetas();
}

function atualizarMetas() {
  const plano = planoAtual();
  const fonte = fonteAtual();
  const metas = plano && fonte ? plano.metas.filter(m => fonte.metas.includes(m.id)) : [];
  opcoes(campos.meta, metas, '-');
  conferir();
}

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
        <small>${kb} KB - enviado agora</small>
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

function conferir() {
  const plano = planoAtual();
  const fonte = fonteAtual();
  const meta = metaAtual();
  const valor = lerMoeda(campos.valor.value);
  const numeroDoc = campos.numeroDocumento.value.trim().replace(/^0+/, '');
  const docUsadoEm = Object.entries(cadastro.documentosUsados)
    .find(([doc]) => doc.replace(/^0+/, '') === numeroDoc)?.[1];

  const disponivelAntes = meta ? meta.previsto - meta.executado : 0;

  const itens = [
    { ok: !!plano?.validado, texto: plano?.validado ? 'Parceria com plano de trabalho validado' : 'O plano de trabalho desta parceria ainda não foi validado' },
    { ok: !!meta, texto: 'Meta pertence ao plano selecionado' },
    { ok: !!(fonte && meta && fonte.metas.includes(meta.id)), texto: 'Fonte de recurso cobre a meta' },
    {
      ok: !!arquivo && !!numeroDoc && !docUsadoEm,
      erro: !!docUsadoEm,
      texto: docUsadoEm
        ? `Comprovante único: documento ${campos.numeroDocumento.value} já usado no Nº ${docUsadoEm}`
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
      `Este documento já comprova o lançamento <b>Nº ${docUsadoEm}</b>. Anexe o documento correto ou confira o lançamento anterior.`;
    aviso.classList.remove('d-none');
  } else {
    aviso.classList.add('d-none');
  }

  $('tituloEfeito').textContent = meta ? `Efeito na ${meta.nome.split(' - ')[0]}` : 'Efeito na meta';
  if (meta) {
    const esta = valor > 0 ? valor : 0;
    const depois = meta.previsto - meta.executado - esta;
    const pct = (meta.executado + esta) / meta.previsto * 100;
    $('efeitoPrevisto').textContent = moeda(meta.previsto);
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

  const camposPreenchidos = campos.data.value && campos.competencia.value && campos.historico.value.trim() && campos.fornecedor.value;
  $('registrar').disabled = !(passaram === itens.length && camposPreenchidos);
}

function mostrarAviso(texto) {
  const toast = $('aviso');
  toast.querySelector('.toast-body').textContent = texto;
  bootstrap.Toast.getOrCreateInstance(toast).show();
}

campos.parceria.addEventListener('change', atualizarPlanos);
campos.plano.addEventListener('change', atualizarFontes);
campos.fonte.addEventListener('change', atualizarMetas);
campos.fornecedor.addEventListener('change', () => { campos.cnpj.value = campos.fornecedor.value; conferir(); });

['meta', 'data', 'competencia', 'valor', 'historico', 'numeroDocumento'].forEach(nome =>
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

$('salvarRascunho').addEventListener('click', () => {
  const hora = new Date().toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  $('statusRascunho').textContent = `Rascunho salvo às ${hora}`;
});

$('formLancamento').addEventListener('submit', e => {
  e.preventDefault();
  if ($('registrar').disabled) return;

  const numero = $('numeroLancamento').textContent;
  mostrarAviso(`Despesa Nº ${numero} registrada no razão.`);
  $('numeroLancamento').textContent = Number(numero) + 1;
});

opcoes(campos.parceria, cadastro.parcerias, '-');
campos.cnpj.value = campos.fornecedor.value;
campos.data.value = new Date().toISOString().slice(0, 10);
campos.competencia.value = new Date().toISOString().slice(0, 7);
atualizarPlanos();
