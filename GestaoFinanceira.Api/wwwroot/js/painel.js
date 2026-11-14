// Painel: razão do mês da conta, execução das metas e pendências da parceria do topo

const $ = id => document.getElementById(id);

const SITUACOES = {
  'conciliado': '<i class="bi bi-check2 conferencia ok" title="Conciliado"></i>',
  'a-conciliar': '<i class="bi bi-circle conferencia pendente" title="A conciliar"></i>',
  'sem-comprovante': '<i class="bi bi-exclamation-circle conferencia falta" title="Sem comprovante"></i>'
};

const nomeDoMes = (ano, mes) =>
  new Date(Date.UTC(ano, mes - 1, 1)).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric', timeZone: 'UTC' });

function avisarFalha(erro) {
  console.error(erro);
  $('notaRazao').textContent = 'Não foi possível carregar os dados. Verifique se a API e o banco estão rodando.';
}

// ---------- Razão ----------

async function carregarContas() {
  const contas = (await apiGet('ContasBancarias')).filter(c => c.ativo);

  $('conta').innerHTML = contas.length
    ? contas.map(c => `<option value="${c.id}">${escaparHtml(`${c.nomeBanco} - CC ${c.numeroConta}`)}</option>`).join('')
    : '<option value="">Nenhuma conta cadastrada</option>';
}

async function carregarRazao() {
  if (!$('conta').value) return;

  // Sem mês escolhido, a API devolve o mês do lançamento mais recente
  const [ano, mes] = $('periodo').value.split('-');
  const rota = `Painel/razao?contaBancariaId=${$('conta').value}` + (ano ? `&ano=${ano}&mes=${Number(mes)}` : '');
  const razao = await apiGet(rota);

  $('periodo').value = `${razao.ano}-${String(razao.mes).padStart(2, '0')}`;
  const ultimoDia = razao.linhas.length ? dataCurta(razao.linhas.at(-1).data).slice(0, 5) : 'fim do mês';

  $('transporte').textContent = moeda(razao.transporte);
  $('entradas').textContent = moeda(razao.entradas);
  $('saidas').textContent = moeda(razao.saidas);
  $('rotuloSaldo').textContent = `= Saldo em ${ultimoDia}`;
  $('saldoFinal').textContent = moeda(razao.saldoFinal);

  $('tituloRazao').textContent = `Razão de ${nomeDoMes(razao.ano, razao.mes).split(' ')[0]}`;
  $('detalheRazao').textContent = $('conta').selectedOptions[0]?.text ?? '';
  $('notaRazao').textContent = `${razao.linhas.length} lançamentos em ${nomeDoMes(razao.ano, razao.mes)} - valores em R$`;

  const linhas = razao.linhas.map(l => `
    <tr>
      <td class="numero">${l.tipo === 'despesa' ? 'D' : 'R'}-${l.id}</td>
      <td class="data">${dataCurta(l.data).slice(0, 5)}</td>
      <td><div class="historico">${escaparHtml(l.historico)}</div>
          <div class="complemento" title="${l.lancadoPor ? `Lançado por ${escaparHtml(l.lancadoPor)}` : ''}">${escaparHtml(l.complemento)}</div></td>
      <td class="valor filete debito">${l.debito ? moeda(l.debito) : ''}</td>
      <td class="valor credito">${l.credito ? moeda(l.credito) : ''}</td>
      <td class="valor">${moeda(l.saldo)}</td>
      <td class="text-center">${SITUACOES[l.situacao]}</td>
    </tr>`).join('');

  $('razao').innerHTML = `
    <tr class="transportado">
      <td class="numero">-</td><td class="data">01/${String(razao.mes).padStart(2, '0')}</td>
      <td>Transporte do mês anterior</td>
      <td class="valor filete"></td><td class="valor"></td><td class="valor">${moeda(razao.transporte)}</td><td></td>
    </tr>
    ${linhas || '<tr><td></td><td></td><td class="ausente">Nenhum lançamento neste mês.</td><td class="filete"></td><td></td><td></td><td></td></tr>'}
    <tr class="total">
      <td></td><td></td><td>Saldo em ${ultimoDia} - a transportar</td>
      <td class="valor filete debito">${moeda(razao.saidas)}</td><td class="valor credito">${moeda(razao.entradas)}</td>
      <td class="valor"><span class="sublinhado-duplo">${moeda(razao.saldoFinal)}</span></td><td></td>
    </tr>`;
}

// ---------- Parceria: metas e pendências ----------

async function carregarParceria(parceriaId) {
  if (!parceriaId) return;

  const [relatorio, situacao] = await Promise.all([
    apiGet(`Relatorios/execucao?parceriaId=${parceriaId}`),
    apiGet('Documentos/situacao')
  ]);

  $('subtitulo').textContent = `${relatorio.termoFomento} - ${relatorio.parceria}`;

  desenharMetas(relatorio.metas);
  desenharPendencias(relatorio, situacao.filter(d => !d.documentos.length));
}

function desenharMetas(metas) {
  if (!metas.length) {
    $('metas').innerHTML = '<p class="texto-apoio px-3">Esta parceria ainda não tem plano validado.</p>';
    return;
  }

  const maiorPrevisto = Math.max(...metas.map(m => m.previsto));

  $('metas').innerHTML = metas.map(m => {
    const pct = Math.round(m.executado / m.previsto * 100);
    const alerta = pct >= 90 ? ' alerta' : '';
    return `
      <div class="meta-linha">
        <div>${escaparHtml(m.meta)}</div>
        <div class="trilho-celula">
          <div class="trilho${alerta}" style="width: ${m.previsto / maiorPrevisto * 100}%">
            <span style="width: ${Math.min(pct, 100)}%"></span>
          </div>
        </div>
        <div class="executado">${moeda(m.executado)}<small>de ${moeda(m.previsto)}</small></div>
        <div class="percentual${alerta}">${pct}%</div>
      </div>`;
  }).join('');
}

function desenharPendencias(relatorio, semComprovante) {
  const notas = [];

  if (semComprovante.length) {
    const primeira = semComprovante[0];
    const outras = semComprovante.length - 1;
    notas.push({
      titulo: `Nº ${primeira.despesaId} sem comprovante`,
      texto: `${primeira.descricao}, R$ ${moeda(primeira.valor)}. Despesa sem documento não entra na prestação de contas.` +
        (outras ? ` Há mais ${outras} na mesma situação.` : ''),
      link: 'comprovantes.html', acao: 'Anexar comprovante'
    });
  }

  if (relatorio.movimentosAConciliar) {
    notas.push({
      titulo: `${relatorio.movimentosAConciliar} movimento${relatorio.movimentosAConciliar > 1 ? 's' : ''} a conciliar`,
      texto: 'Movimentos do extrato que ainda não foram ligados a uma despesa ou receita.',
      link: 'conciliacao.html', acao: 'Conciliar agora'
    });
  }

  // Dias até o fim da vigência da parceria
  const dias = Math.ceil((new Date(relatorio.dataFim) - new Date()) / 86400000);
  if (dias <= 90) {
    notas.push({
      titulo: dias >= 0 ? `Vigência do ${relatorio.termoFomento} termina em ${dias} dias` : `Vigência do ${relatorio.termoFomento} encerrada`,
      texto: `Prazo ${dataCurta(relatorio.dataFim)}. Confira as pendências antes da prestação de contas.`,
      link: 'prestacao-contas.html', acao: 'Ver prestação'
    });
  }

  $('qtdPendencias').textContent = notas.length ? `${notas.length} pendência${notas.length > 1 ? 's' : ''}` : '';

  $('pendencias').innerHTML = notas.length
    ? notas.map((n, i) => `
      <div class="nota">
        <span class="alfinete">${i + 1}</span>
        <div>
          <strong>${escaparHtml(n.titulo)}</strong>
          <p>${escaparHtml(n.texto)}</p>
          <a href="${n.link}">${n.acao}</a>
        </div>
      </div>`).join('')
    : '<p class="texto-apoio px-3 pb-3">Nenhuma pendência. Tudo comprovado e conciliado.</p>';
}

// ---------- Eventos ----------

$('conta').addEventListener('change', () => {
  $('periodo').value = '';
  carregarRazao().catch(avisarFalha);
});

$('periodo').addEventListener('change', () => carregarRazao().catch(avisarFalha));

// O layout dispara este evento ao carregar e sempre que a parceria do topo muda
document.addEventListener('parceriaEscolhida', e => carregarParceria(e.detail).catch(avisarFalha));

carregarContas()
  .then(carregarRazao)
  .catch(avisarFalha);