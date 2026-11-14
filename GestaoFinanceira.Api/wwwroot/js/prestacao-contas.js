// Prestação de contas ligada à API: execução por meta e rastreio de cada despesa

const $ = id => document.getElementById(id);

let relatorio = null;

const pct = (a, b) => b ? (a / b * 100).toFixed(0) + '%' : '-';
const comprov = (tem, total) =>
  `<span class="${tem < total ? 'debito' : 'text-secondary'}">${tem} / ${total}</span>`;

function mesAno(data) {
  return new Date(data).toLocaleDateString('pt-BR', { month: 'short', year: 'numeric', timeZone: 'UTC' }).replace(' de ', '/');
}

function avisarFalha(erro) {
  console.error(erro);
  $('pendencias').textContent = 'Não foi possível carregar os dados. Verifique se a API e o banco estão rodando.';
}

async function carregarParcerias() {
  const parcerias = await apiGet('Parcerias');

  $('parceria').innerHTML = parcerias.length
    ? parcerias.map(p => `<option value="${p.id}">${escaparHtml(`${p.termoFomento} - ${p.nome}`)}</option>`).join('')
    : '<option value="">Nenhuma parceria cadastrada</option>';
    // Começa na parceria escolhida no topo
    if (parcerias.some(p => p.id == parceriaEscolhida())) $('parceria').value = parceriaEscolhida();
}

async function carregarRelatorio() {
  if (!$('parceria').value) return;

  relatorio = await apiGet(`Relatorios/execucao?parceriaId=${$('parceria').value}`);

  // As fontes do filtro saem das próprias metas, sem repetir
  const fontes = [...new Map(relatorio.metas.map(m => [m.fonteRecursoId, m.fonteRecurso])).entries()];
  $('fonte').innerHTML = '<option value="">Todas as fontes</option>' +
    fontes.map(([id, nome]) => `<option value="${id}">${escaparHtml(nome)}</option>`).join('');

  desenhar();
}

function desenhar() {
  const r = relatorio;
  const metas = r.metas.filter(m => !$('fonte').value || m.fonteRecursoId == $('fonte').value);

  const soma = campo => metas.reduce((t, m) => t + m[campo], 0);
  const previsto = soma('previsto');
  const executado = soma('executado');
  const despesas = soma('despesas');
  const comComprovante = soma('comComprovante');

  $('totalPrevisto').textContent = moeda(previsto);
  $('totalExecutado').textContent = moeda(executado);
  $('totalSaldo').textContent = moeda(previsto - executado);

  $('subtitulo').textContent = r.plano
    ? `Previsto x executado - ${r.termoFomento} - ${mesAno(r.dataInicio)} a ${mesAno(r.dataFim)}`
    : `${r.termoFomento} ainda não tem plano de trabalho validado`;

  $('pendencias').textContent =
    `Vigência até ${dataCurta(r.dataFim)} - antes do envio: ` +
    `${r.despesasSemComprovante} despesa${r.despesasSemComprovante === 1 ? '' : 's'} sem comprovante e ` +
    `${r.movimentosAConciliar} movimento${r.movimentosAConciliar === 1 ? '' : 's'} a conciliar`;

  const faltam = despesas - comComprovante;
  const selo = $('seloComprovantes');
  selo.className = `conferencia ${faltam ? 'pendente' : 'ok'}`;
  selo.innerHTML = faltam
    ? `<i class="bi bi-circle"></i> ${faltam} comprovante${faltam > 1 ? 's' : ''} pendente${faltam > 1 ? 's' : ''}`
    : '<i class="bi bi-check2"></i> comprovantes em dia';

  if (!metas.length) {
    $('execucao').innerHTML = '<tr><td colspan="7" class="ausente">Nenhuma meta para mostrar.</td></tr>';
    return;
  }

  $('execucao').innerHTML = metas.map(m => `
    <tr>
      <td><div class="historico">${escaparHtml(m.meta)}</div></td>
      <td>${escaparHtml(m.fonteRecurso)}</td>
      <td class="valor filete">${moeda(m.previsto)}</td>
      <td class="valor">${moeda(m.executado)}</td>
      <td class="valor">${moeda(m.previsto - m.executado)}</td>
      <td class="valor">${pct(m.executado, m.previsto)}</td>
      <td class="valor">${comprov(m.comComprovante, m.despesas)}</td>
    </tr>`).join('') + `
    <tr class="total">
      <td colspan="2">Total ${$('fonte').value ? 'da fonte' : 'da parceria'}</td>
      <td class="valor filete"><span class="sublinhado-duplo">${moeda(previsto)}</span></td>
      <td class="valor"><span class="sublinhado-duplo">${moeda(executado)}</span></td>
      <td class="valor"><span class="sublinhado-duplo">${moeda(previsto - executado)}</span></td>
      <td class="valor">${previsto ? (executado / previsto * 100).toFixed(1).replace('.', ',') + '%' : '-'}</td>
      <td class="valor">${comprov(comComprovante, despesas)}</td>
    </tr>`;
}

async function rastrear(numero) {
  $('tituloTrilha').textContent = `Do termo ao extrato, despesa Nº ${numero}`;

  try {
    const etapas = await apiGet(`Relatorios/rastreio/${numero}`);
    $('trilha').innerHTML = etapas.map(e => `
      <li><span class="ponto"></span><div><small>${e.etapa}</small><strong class="${e.ok ? '' : 'debito'}">${escaparHtml(e.descricao)}</strong></div></li>`).join('');
  } catch {
    $('trilha').innerHTML = `<li><span class="ponto"></span><div><small>Despesa Nº ${escaparHtml(numero)}</small><strong class="debito">Não encontrada</strong></div></li>`;
  }
}

$('parceria').addEventListener('change', () => carregarRelatorio().catch(avisarFalha));
$('fonte').addEventListener('change', desenhar);

$('formRastreio').addEventListener('submit', e => {
  e.preventDefault();
  const numero = $('numeroRastreio').value.trim();
  if (/^\d+$/.test(numero)) rastrear(numero);
});

// O navegador baixa o arquivo que a API devolve
$('baixarCsv').addEventListener('click', () => {
  if ($('parceria').value) window.location.href = `/api/Relatorios/execucao/csv?parceriaId=${$('parceria').value}`;
});

// Na janela de impressão dá para escolher "Salvar como PDF"
$('imprimir').addEventListener('click', () => window.print());

$('trilha').innerHTML = '<li><span class="ponto"></span><div><small>Rastrear</small><strong>Digite o Nº de uma despesa abaixo</strong></div></li>';

carregarParcerias()
  .then(carregarRelatorio)
  .catch(avisarFalha);