// Cadastro de contas bancárias: o mesmo formulário serve para criar e para editar

const $ = id => document.getElementById(id);

let contas = [];
let editando = null;   // conta em edição; null quando é uma nova

function aviso(texto) {
  const toast = $('aviso');
  toast.querySelector('.toast-body').textContent = texto;
  bootstrap.Toast.getOrCreateInstance(toast).show();
}

async function carregar() {
  contas = await apiGet('ContasBancarias');

  $('lista').innerHTML = contas.length
    ? contas.map(c => `
      <tr style="vertical-align: middle">
        <td><div class="historico">${escaparHtml(c.nomeBanco)}</div></td>
        <td class="numero">${escaparHtml(c.agencia)}</td>
        <td class="numero">${escaparHtml(c.numeroConta)}</td>
        <td class="valor">${moeda(c.saldoAtual)}</td>
        <td>${c.ativo
          ? '<span class="conferencia ok"><i class="bi bi-check2"></i> ativa</span>'
          : '<span class="conferencia pendente"><i class="bi bi-circle"></i> inativa</span>'}</td>
        <td class="text-end"><button class="btn btn-outline-secondary btn-sm" type="button" data-editar="${c.id}" data-perfis="Administrador,Tesouraria">Editar</button></td>
      </tr>`).join('')
    : '<tr><td colspan="6" class="ausente">Nenhuma conta cadastrada.</td></tr>';

  aplicarPerfis();
}

function abrirFormulario(conta) {
  editando = conta;

  $('tituloForm').textContent = conta ? 'Editar conta' : 'Nova conta';
  $('nomeBanco').value = conta?.nomeBanco ?? '';
  $('agencia').value = conta?.agencia ?? '';
  $('numeroConta').value = conta?.numeroConta ?? '';
  $('saldo').value = '';
  $('ativo').checked = conta?.ativo ?? true;

  // Saldo só no cadastro; ativo/inativo só na edição
  $('campoSaldo').hidden = !!conta;
  $('campoAtivo').hidden = !conta;
  $('erroSalvar').classList.add('d-none');

  bootstrap.Offcanvas.getOrCreateInstance($('painelForm')).show();
}

$('novo').addEventListener('click', () => abrirFormulario(null));

$('lista').addEventListener('click', e => {
  const botao = e.target.closest('[data-editar]');
  if (botao) abrirFormulario(contas.find(c => c.id == botao.dataset.editar));
});

$('saldo').addEventListener('blur', e => {
  const valor = lerMoeda(e.target.value);
  if (valor >= 0) e.target.value = moeda(valor);
});

$('form').addEventListener('submit', async e => {
  e.preventDefault();

  const corpo = {
    id: editando?.id ?? 0,
    nomeBanco: $('nomeBanco').value.trim(),
    agencia: $('agencia').value.trim(),
    numeroConta: $('numeroConta').value.trim(),
    saldoAtual: lerMoeda($('saldo').value) || 0,
    ativo: $('ativo').checked
  };

  try {
    if (editando) await apiPut(`ContasBancarias/${editando.id}`, corpo);
    else await apiPost('ContasBancarias', corpo);

    bootstrap.Offcanvas.getInstance($('painelForm')).hide();
    aviso(editando ? 'Conta atualizada.' : 'Conta cadastrada.');
    await carregar();
  } catch (erro) {
    $('erroSalvar').textContent = erro.message;
    $('erroSalvar').classList.remove('d-none');
  }
});

carregar().catch(erro => aviso(erro.message));