// Movimentos de exemplo (primeira página do extrato de setembro)
const movimentos = [
  { data: '17/09', extrato: 'PIX ENVIADO ATACADAO DISTRIB', valor: -8420.90, situacao: 'ok', numero: 1041, lancamento: 'Gêneros alimentícios - Atacadão' },
  { data: '16/09', extrato: 'TED RECEBIDA PREF MUNICIPAL', valor: 62000.00, situacao: 'ok', numero: 1040, lancamento: 'Repasse TF 012/2025 - 4ª parcela' },
  { data: '15/09', extrato: 'PIX ENVIADO ANA LIMA', valor: -3200.00, situacao: 'ok', numero: 1039, lancamento: 'Oficina de música - Ana Lima' },
  { data: '14/09', extrato: 'TARIFA CESTA DE SERVICOS', valor: -42.90, situacao: 'sem-lancamento', acao: 'Lançar tarifa' },
  { data: '12/09', extrato: 'PIX ENVIADO PAPELARIA CENTRAL', valor: -1184.50, situacao: 'sugestao', numero: 1038, lancamento: 'Material pedagógico - Papelaria Central', acao: 'Confirmar' },
  { data: '10/09', extrato: 'DEPOSITO INST PARCEIRO', valor: 5000.00, situacao: 'ok', numero: 1037, lancamento: 'Doação - Instituto Parceiro' },
  { data: '', extrato: '', valor: -960.00, situacao: 'sem-extrato', numero: 1035, lancamento: 'Compra de uniformes (08/09)', acao: 'Aguardar' }
];

const SITUACOES = {
  'ok': { classe: 'ok', icone: 'bi-check2', texto: 'conciliado' },
  'sugestao': { classe: 'sugestao', icone: 'bi-arrow-left-right', texto: 'sugestão' },
  'sem-lancamento': { classe: 'falta', icone: 'bi-exclamation-circle', texto: 'sem lançamento' },
  'sem-extrato': { classe: 'pendente', icone: 'bi-circle', texto: 'sem extrato' }
};

const tbody = document.getElementById('movimentos');
const botaoConciliar = document.getElementById('conciliar');
let filtro = '';

function valorComSinal(valor) {
  const sinal = valor < 0 ? '-' : '+';
  return `${sinal} ${moeda(Math.abs(valor))}`;
}

function desenhar() {
  const visiveis = movimentos.filter(m => {
    if (!filtro) return true;
    if (filtro === 'divergencia') return m.situacao === 'sem-lancamento' || m.situacao === 'sem-extrato';
    return m.situacao === filtro;
  });

  tbody.innerHTML = visiveis.map(m => {
    const i = movimentos.indexOf(m);
    const s = SITUACOES[m.situacao];
    const cor = m.valor < 0 ? 'debito' : '';
    const podeSelecionar = m.situacao === 'sugestao';

    const ladoExtrato = m.extrato
      ? `<td class="data">${m.data}</td><td class="extrato">${m.extrato}</td><td class="valor ${cor}">${valorComSinal(m.valor)}</td>`
      : `<td class="data">-</td><td class="ausente">Não consta no extrato</td><td class="valor"></td>`;

    const ladoSistema = m.numero
      ? `<td class="numero filete">${m.numero}</td><td>${m.lancamento}</td><td class="valor ${cor}">${valorComSinal(m.valor)}</td>`
      : `<td class="numero filete">-</td><td class="ausente">Nenhum lançamento corresponde</td><td class="valor"></td>`;

    return `
      <tr class="${m.selecionado ? 'selecionada' : ''}" style="vertical-align: middle">
        <td><input class="form-check-input" type="checkbox" data-i="${i}" ${m.selecionado ? 'checked' : ''} ${podeSelecionar ? '' : 'disabled'} aria-label="Selecionar"></td>
        ${ladoExtrato}
        <td><span class="conferencia ${s.classe}"><i class="bi ${s.icone}"></i> ${s.texto}</span></td>
        ${ladoSistema}
        <td class="text-end">${m.acao ? `<button class="btn btn-outline-secondary btn-sm" type="button" data-acao="${i}">${m.acao}</button>` : ''}</td>
      </tr>`;
  }).join('');

  document.getElementById('contagem').textContent = `Mostrando ${visiveis.length} de 50 movimentos de setembro`;
  atualizarBotao();
}

function atualizarBotao() {
  const qtd = movimentos.filter(m => m.selecionado).length;
  botaoConciliar.disabled = qtd === 0;
  botaoConciliar.querySelector('span').textContent = qtd ? `Conciliar ${qtd} selecionado${qtd > 1 ? 's' : ''}` : 'Conciliar selecionados';
}

function conciliar(indices) {
  indices.forEach(i => {
    movimentos[i].situacao = 'ok';
    movimentos[i].selecionado = false;
    delete movimentos[i].acao;
  });
  desenhar();
  aviso(indices.length === 1 ? 'Movimento conciliado.' : `${indices.length} movimentos conciliados.`);
}

function aviso(texto) {
  const toast = document.getElementById('aviso');
  toast.querySelector('.toast-body').textContent = texto;
  bootstrap.Toast.getOrCreateInstance(toast).show();
}

tbody.addEventListener('change', e => {
  if (!e.target.matches('input[type=checkbox]')) return;
  movimentos[e.target.dataset.i].selecionado = e.target.checked;
  e.target.closest('tr').classList.toggle('selecionada', e.target.checked);
  atualizarBotao();
});

tbody.addEventListener('click', e => {
  const botao = e.target.closest('[data-acao]');
  if (!botao) return;
  const m = movimentos[botao.dataset.acao];

  if (m.situacao === 'sugestao') conciliar([Number(botao.dataset.acao)]);
  else if (m.situacao === 'sem-lancamento') window.location.href = 'novo-lancamento.html';
  else aviso('Esse lançamento vai aparecer no próximo extrato importado.');
});

botaoConciliar.addEventListener('click', () => {
  conciliar(movimentos.map((m, i) => m.selecionado ? i : -1).filter(i => i >= 0));
});

document.getElementById('abas').addEventListener('click', e => {
  const botao = e.target.closest('button');
  if (!botao) return;
  document.querySelectorAll('#abas button').forEach(b => b.classList.toggle('ativa', b === botao));
  filtro = botao.dataset.filtro;
  desenhar();
});

document.getElementById('arquivoOfx').addEventListener('change', e => {
  const arquivo = e.target.files[0];
  if (!arquivo) return;
  // TODO: enviar o OFX para a API quando o endpoint de importação estiver pronto
  document.getElementById('infoExtrato').textContent = `Arquivo ${arquivo.name} selecionado · importação ainda não disponível na API`;
});

// começa com a sugestão marcada para facilitar a conferência
movimentos[4].selecionado = true;
desenhar();
