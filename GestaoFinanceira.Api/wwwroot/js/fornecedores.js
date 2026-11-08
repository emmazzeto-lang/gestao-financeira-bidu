// Cadastro de fornecedores: o mesmo formulário serve para criar e para editar

const $ = id => document.getElementById(id);

let fornecedores = [];
let editando = null;   // fornecedor em edição; null quando é um novo

function aviso(texto) {
  const toast = $('aviso');
  toast.querySelector('.toast-body').textContent = texto;
  bootstrap.Toast.getOrCreateInstance(toast).show();
}

async function carregar() {
  // todos=true traz também os inativos, para poder reativar
  fornecedores = await apiGet('Fornecedores?todos=true');
  desenhar();
}

function desenhar() {
  const busca = $('busca').value.trim().toUpperCase();
  const buscaDocumento = busca.replace(/[^0-9A-Z]/g, '');

  const visiveis = fornecedores.filter(f =>
    !busca || f.nome.toUpperCase().includes(busca) || (buscaDocumento && f.cpfCnpj.includes(buscaDocumento)));

  $('resumo').textContent = `${fornecedores.filter(f => f.ativo).length} ativos de ${fornecedores.length}`;

  $('lista').innerHTML = visiveis.length
    ? visiveis.map(f => `
      <tr style="vertical-align: middle">
        <td><div class="historico">${escaparHtml(f.nome)}</div></td>
        <td class="numero">${formatarCpfCnpj(f.cpfCnpj)}</td>
        <td>${f.ativo
          ? '<span class="conferencia ok"><i class="bi bi-check2"></i> ativo</span>'
          : '<span class="conferencia pendente"><i class="bi bi-circle"></i> inativo</span>'}</td>
        <td class="text-end"><button class="btn btn-outline-secondary btn-sm" type="button" data-editar="${f.id}" data-perfis="Administrador,Tesouraria">Editar</button></td>
      </tr>`).join('')
    : '<tr><td colspan="4" class="ausente">Nenhum fornecedor encontrado.</td></tr>';

  aplicarPerfis();
}

function abrirFormulario(fornecedor) {
  editando = fornecedor;

  $('tituloForm').textContent = fornecedor ? 'Editar fornecedor' : 'Novo fornecedor';
  $('nome').value = fornecedor?.nome ?? '';
  $('cpfCnpj').value = formatarCpfCnpj(fornecedor?.cpfCnpj ?? '');
  $('ativo').checked = fornecedor?.ativo ?? true;

  // Um fornecedor novo sempre nasce ativo
  $('campoAtivo').hidden = !fornecedor;
  $('erroSalvar').classList.add('d-none');

  bootstrap.Offcanvas.getOrCreateInstance($('painelForm')).show();
}

$('novo').addEventListener('click', () => abrirFormulario(null));

$('lista').addEventListener('click', e => {
  const botao = e.target.closest('[data-editar]');
  if (botao) abrirFormulario(fornecedores.find(f => f.id == botao.dataset.editar));
});

$('busca').addEventListener('input', desenhar);

// Formata enquanto a pessoa digita
$('cpfCnpj').addEventListener('input', e => {
  e.target.value = formatarCpfCnpj(e.target.value);
});

$('form').addEventListener('submit', async e => {
  e.preventDefault();

  const corpo = {
    id: editando?.id ?? 0,
    nome: $('nome').value.trim(),
    cpfCnpj: $('cpfCnpj').value,
    ativo: $('ativo').checked
  };

  try {
    if (editando) await apiPut(`Fornecedores/${editando.id}`, corpo);
    else await apiPost('Fornecedores', corpo);

    bootstrap.Offcanvas.getInstance($('painelForm')).hide();
    aviso(editando ? 'Fornecedor atualizado.' : 'Fornecedor cadastrado.');
    await carregar();
  } catch (erro) {
    // A API confere os dígitos do CPF/CNPJ e devolve a mensagem
    $('erroSalvar').textContent = erro.message;
    $('erroSalvar').classList.remove('d-none');
  }
});

carregar().catch(erro => aviso(erro.message));