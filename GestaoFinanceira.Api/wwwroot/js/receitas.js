// Receitas: lista do plano escolhido e formulário para registrar um novo aporte

const $ = id => document.getElementById(id);

let planos = [];

function aviso(texto) {
  const toast = $('aviso');
  toast.querySelector('.toast-body').textContent = texto;
  bootstrap.Toast.getOrCreateInstance(toast).show();
}

function falhou(erro) {
  console.error(erro);
  aviso(erro.message || 'Não foi possível falar com a API.');
}

const planoAtual = () => planos.find(p => p.id == $('plano').value);

async function carregarCadastros() {
  const [parcerias, fontes, contas] = await Promise.all([
    apiGet('Parcerias'),
    apiGet('FontesRecurso'),
    apiGet('ContasBancarias')
  ]);

  $('parceria').innerHTML = parcerias.length
    ? parcerias.map(p => `<option value="${p.id}">${escaparHtml(`${p.termoFomento} - ${p.nome}`)}</option>`).join('')
    : '<option value="">Nenhuma parceria cadastrada</option>';

  $('fonte').innerHTML = fontes.map(f => `<option value="${f.id}">${escaparHtml(f.nome)}</option>`).join('');

  $('conta').innerHTML = contas.filter(c => c.ativo)
    .map(c => `<option value="${c.id}">${escaparHtml(`${c.nomeBanco} - Ag ${c.agencia} - CC ${c.numeroConta}`)}</option>`).join('');

  await carregarPlanos();
}

async function carregarPlanos() {
  planos = $('parceria').value ? await apiGet(`PlanosTrabalho?parceriaId=${$('parceria').value}`) : [];

  $('plano').innerHTML = planos.length
    ? planos.map(p => `<option value="${p.id}">${escaparHtml(p.descricao)}${p.validado ? '' : ' (aguardando validação)'}</option>`).join('')
    : '<option value="">Nenhum plano</option>';

  await carregarReceitas();
}

async function carregarReceitas() {
  const plano = planoAtual();
  const receitas = plano ? await apiGet(`Receitas?planoTrabalhoId=${plano.id}`) : [];

  // Sem plano validado não entra dinheiro: avisa e esconde o formulário
  const bloqueado = !plano || !plano.validado;
  $('avisoBloqueio').hidden = !plano || plano.validado;
  $('painelNova').dataset.perfis = bloqueado ? 'nenhum' : 'Administrador,Tesouraria';
  aplicarPerfis();

  const total = receitas.reduce((t, r) => t + r.valor, 0);
  $('resumo').textContent = receitas.length ? `${receitas.length} receitas - R$ ${moeda(total)}` : '';

  $('lista').innerHTML = receitas.length
    ? receitas.map(r => `
      <tr>
        <td class="data">${dataCurta(r.dataRecebimento)}</td>
        <td><div class="historico">${escaparHtml(r.descricao)}</div>
            <div class="complemento">${escaparHtml(r.contaBancaria ? `${r.contaBancaria.nomeBanco} - CC ${r.contaBancaria.numeroConta}` : '')}</div></td>
        <td>${escaparHtml(r.fonteRecurso?.nome ?? '-')}</td>
        <td class="valor credito">${moeda(r.valor)}</td>
      </tr>`).join('') + `
      <tr class="total">
        <td colspan="3">Total recebido no plano</td>
        <td class="valor credito"><span class="sublinhado-duplo">${moeda(total)}</span></td>
      </tr>`
    : '<tr><td colspan="4" class="ausente">Nenhuma receita neste plano.</td></tr>';
}

$('parceria').addEventListener('change', () => carregarPlanos().catch(falhou));
$('plano').addEventListener('change', () => carregarReceitas().catch(falhou));

$('valor').addEventListener('blur', e => {
  const valor = lerMoeda(e.target.value);
  if (valor > 0) e.target.value = moeda(valor);
});

$('form').addEventListener('submit', async e => {
  e.preventDefault();
  const valor = lerMoeda($('valor').value);

  if (!$('descricao').value.trim() || !(valor > 0) || !$('data').value) {
    aviso('Preencha a descrição, o valor e a data.');
    return;
  }

  $('registrar').disabled = true;

  try {
    await apiPost('Receitas', {
      descricao: $('descricao').value.trim(),
      valor,
      dataRecebimento: `${$('data').value}T00:00:00Z`,
      contaBancariaId: Number($('conta').value),
      planoTrabalhoId: Number($('plano').value),
      fonteRecursoId: Number($('fonte').value)
    });

    $('descricao').value = '';
    $('valor').value = '';
    aviso('Receita registrada.');
    await carregarReceitas();
  } catch (erro) {
    falhou(erro);
  } finally {
    $('registrar').disabled = false;
  }
});

$('data').value = new Date().toISOString().slice(0, 10);
carregarCadastros().catch(falhou);