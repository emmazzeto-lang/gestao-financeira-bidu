// Conciliação ligada à API: importa o OFX e confere cada movimento com as despesas e receitas

const SITUACOES = {
  'conciliado': { classe: 'ok', icone: 'bi-check2', texto: 'conciliado' },
  'sugestao': { classe: 'sugestao', icone: 'bi-arrow-left-right', texto: 'sugestão' },
  'sem-lancamento': { classe: 'falta', icone: 'bi-exclamation-circle', texto: 'sem lançamento' },
  'sem-extrato': { classe: 'pendente', icone: 'bi-circle', texto: 'sem extrato' }
};

const $ = id => document.getElementById(id);
const tbody = $('movimentos');
const botaoConciliar = $('conciliar');

let linhas = [];
let filtro = '';
const selecionadas = new Set();

function valorComSinal(valor) {
  const sinal = valor < 0 ? '-' : '+';
  return `${sinal} ${moeda(Math.abs(valor))}`;
}

// "despesa 2" aparece como D-2 e "receita 1" como R-1
function numeroLancamento(l) {
  return `${l.tipo === 'despesa' ? 'D' : 'R'}-${l.id}`;
}

function aviso(texto) {
  const toast = $('aviso');
  toast.querySelector('.toast-body').textContent = texto;
  bootstrap.Toast.getOrCreateInstance(toast).show();
}

function falhou(erro) {
  console.error(erro);
  aviso(erro.message || 'Não foi possível falar com a API.');
}

// ---------- Dados ----------

async function carregarContas() {
  const contas = (await apiGet('ContasBancarias')).filter(c => c.ativo);

  $('conta').innerHTML = contas.length
    ? contas.map(c => `<option value="${c.id}">${escaparHtml(`${c.nomeBanco} - CC ${c.numeroConta}`)}</option>`).join('')
    : '<option value="">Nenhuma conta cadastrada</option>';
}

async function carregar() {
  const conta = $('conta').value;
  const [ano, mes] = $('periodo').value.split('-');
  if (!conta || !ano) return;

  const batimento = await apiGet(`Conciliacao?contaBancariaId=${conta}&ano=${ano}&mes=${Number(mes)}`);
  linhas = batimento.linhas;
  selecionadas.clear();

  // Começa com as sugestões marcadas, para conferir e confirmar de uma vez
  linhas.forEach((l, i) => { if (l.situacao === 'sugestao') selecionadas.add(i); });

  mostrarTotais(batimento);
  desenhar();
}

function mostrarTotais(batimento) {
  const diferenca = batimento.totalExtrato - batimento.totalSistema;

  $('totalExtrato').textContent = valorComSinal(batimento.totalExtrato);
  $('totalSistema').textContent = valorComSinal(batimento.totalSistema);
  $('diferenca').textContent = moeda(Math.abs(diferenca));
  $('termoDiferenca').classList.toggle('divergencia', Math.abs(diferenca) >= 0.01);

  const semLancamento = linhas.filter(l => l.situacao === 'sem-lancamento');
  const semExtrato = linhas.filter(l => l.situacao === 'sem-extrato');

  const partes = [
    ...semLancamento.map(l => `${l.movimento.descricao} de ${dataCurta(l.movimento.data).slice(0, 5)} (${valorComSinal(l.movimento.valor)}) ainda não lançado`),
    ...semExtrato.map(l => `${l.lancamento.descricao} de ${dataCurta(l.lancamento.data).slice(0, 5)} (${valorComSinal(l.lancamento.valor)}) ainda não compensado`)
  ];

  $('notaDiferenca').textContent = !linhas.length
    ? 'Nenhum movimento neste mês. Importe o extrato OFX da conta.'
    : partes.length ? `A diferença vem de: ${partes.join('; ')}.` : 'Extrato e sistema batem.';
}

// ---------- Tabela ----------

function desenhar() {
  const quantas = situacao => linhas.filter(l => l.situacao === situacao).length;
  $('qtdTodos').textContent = linhas.length;
  $('qtdConciliados').textContent = quantas('conciliado');
  $('qtdSugestoes').textContent = quantas('sugestao');
  $('qtdDivergencias').textContent = quantas('sem-lancamento') + quantas('sem-extrato');

  const visiveis = linhas
    .map((l, i) => ({ ...l, i }))
    .filter(l => {
      if (!filtro) return true;
      if (filtro === 'divergencia') return l.situacao === 'sem-lancamento' || l.situacao === 'sem-extrato';
      return l.situacao === filtro;
    });

  tbody.innerHTML = visiveis.map(l => {
    const s = SITUACOES[l.situacao];
    const m = l.movimento;
    const lanc = l.lancamento;
    const marcada = selecionadas.has(l.i);

    const ladoExtrato = m
      ? `<td class="data">${dataCurta(m.data).slice(0, 5)}</td><td class="extrato">${escaparHtml(m.descricao)}</td><td class="valor ${m.valor < 0 ? 'debito' : ''}">${valorComSinal(m.valor)}</td>`
      : `<td class="data">-</td><td class="ausente">Não consta no extrato</td><td class="valor"></td>`;

    const ladoSistema = lanc
      ? `<td class="numero filete">${numeroLancamento(lanc)}</td><td>${escaparHtml(lanc.descricao)}</td><td class="valor ${lanc.valor < 0 ? 'debito' : ''}">${valorComSinal(lanc.valor)}</td>`
      : `<td class="numero filete">-</td><td class="ausente">Nenhum lançamento corresponde</td><td class="valor"></td>`;

    const acoes = {
      'sugestao': 'Confirmar',
      'conciliado': 'Desfazer',
      'sem-lancamento': 'Lançar'
    };
    const acao = acoes[l.situacao];

    return `
      <tr class="${marcada ? 'selecionada' : ''}" style="vertical-align: middle">
        <td><input class="form-check-input" type="checkbox" data-i="${l.i}" ${marcada ? 'checked' : ''} ${l.situacao === 'sugestao' ? '' : 'disabled'} aria-label="Selecionar"></td>
        ${ladoExtrato}
        <td><span class="conferencia ${s.classe}"><i class="bi ${s.icone}"></i> ${s.texto}</span></td>
        ${ladoSistema}
        <td class="text-end">${acao ? `<button class="btn btn-outline-secondary btn-sm" type="button" data-acao="${l.i}">${acao}</button>` : ''}</td>
      </tr>`;
  }).join('');

  $('contagem').textContent = `Mostrando ${visiveis.length} de ${linhas.length} linhas do mês`;
  atualizarBotao();
}

function atualizarBotao() {
  const qtd = selecionadas.size;
  botaoConciliar.disabled = qtd === 0;
  botaoConciliar.querySelector('span').textContent = qtd ? `Conciliar ${qtd} selecionado${qtd > 1 ? 's' : ''}` : 'Conciliar selecionados';
}

// ---------- Ações ----------

async function conciliar(indices) {
  let feitos = 0;

  // Um de cada vez: se um falhar, os outros já gravados continuam valendo
  for (const i of indices) {
    const l = linhas[i];
    try {
      await apiPost(`Conciliacao/${l.movimento.id}`, { tipo: l.lancamento.tipo, lancamentoId: l.lancamento.id });
      feitos++;
    } catch (erro) {
      falhou(erro);
    }
  }

  if (feitos) aviso(feitos === 1 ? 'Movimento conciliado.' : `${feitos} movimentos conciliados.`);
  await carregar();
}

async function desfazer(i) {
  await apiDelete(`Conciliacao/${linhas[i].movimento.id}`);
  aviso('Conciliação desfeita.');
  await carregar();
}

tbody.addEventListener('change', e => {
  if (!e.target.matches('input[type=checkbox]')) return;
  const i = Number(e.target.dataset.i);
  if (e.target.checked) selecionadas.add(i); else selecionadas.delete(i);
  e.target.closest('tr').classList.toggle('selecionada', e.target.checked);
  atualizarBotao();
});

tbody.addEventListener('click', e => {
  const botao = e.target.closest('[data-acao]');
  if (!botao) return;
  const i = Number(botao.dataset.acao);
  const situacao = linhas[i].situacao;

  if (situacao === 'sugestao') conciliar([i]);
  else if (situacao === 'conciliado') desfazer(i).catch(falhou);
  else window.location.href = 'novo-lancamento.html';
});

botaoConciliar.addEventListener('click', () => conciliar([...selecionadas]));

$('abas').addEventListener('click', e => {
  const botao = e.target.closest('button');
  if (!botao) return;
  document.querySelectorAll('#abas button').forEach(b => b.classList.toggle('ativa', b === botao));
  filtro = botao.dataset.filtro;
  desenhar();
});

$('conta').addEventListener('change', () => carregar().catch(falhou));
$('periodo').addEventListener('change', () => carregar().catch(falhou));

$('arquivoOfx').addEventListener('change', async e => {
  const arquivo = e.target.files[0];
  if (!arquivo) return;

  const envio = new FormData();
  envio.append('contaBancariaId', $('conta').value);
  envio.append('arquivo', arquivo);

  try {
    const resultado = await apiEnviarArquivo('Conciliacao/importar', envio);
    $('infoExtrato').textContent = `${arquivo.name}: ${resultado.importados} movimentos novos, ${resultado.repetidos} já importados`;

    // Mostra o mês do extrato que acabou de entrar
    $('periodo').value = resultado.mes;
    await carregar();
  } catch (erro) {
    falhou(erro);
  } finally {
    // Permite escolher o mesmo arquivo de novo
    e.target.value = '';
  }
});

$('periodo').value = new Date().toISOString().slice(0, 7);
carregarContas()
  .then(carregar)
  .catch(falhou);