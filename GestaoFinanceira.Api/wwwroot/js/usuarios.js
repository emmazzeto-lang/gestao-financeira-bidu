// Gestão de usuários (só o administrador chega aqui; a API recusa os outros perfis)

const PERFIS = ['Administrador', 'Tesouraria', 'Diretoria', 'Consulta'];

const $ = id => document.getElementById(id);
let usuarios = [];

function aviso(texto) {
  const toast = $('aviso');
  toast.querySelector('.toast-body').textContent = texto;
  bootstrap.Toast.getOrCreateInstance(toast).show();
}

function opcoesPerfil(escolhido) {
  return PERFIS.map(p => `<option ${p === escolhido ? 'selected' : ''}>${p}</option>`).join('');
}

async function carregar() {
  try {
    usuarios = await apiGet('Usuarios');
  } catch (erro) {
    $('listaUsuarios').innerHTML = `<tr><td colspan="5" class="ausente">${escaparHtml(erro.message)}</td></tr>`;
    return;
  }

  $('listaUsuarios').innerHTML = usuarios.map(u => `
    <tr data-id="${u.id}" style="vertical-align: middle">
      <td><div class="historico">${escaparHtml(u.nome)}</div></td>
      <td>${escaparHtml(u.email)}</td>
      <td><select class="form-select form-select-sm" data-campo="perfil">${opcoesPerfil(u.perfil)}</select></td>
      <td><div class="form-check form-switch"><input class="form-check-input" type="checkbox" data-campo="ativo" ${u.ativo ? 'checked' : ''} aria-label="Ativo"></div></td>
      <td class="text-end"><button class="btn btn-outline-secondary btn-sm" type="button" data-senha>Nova senha</button></td>
    </tr>`).join('');
}

// PUT não está no api.js porque só esta tela usa
async function salvar(usuario) {
  const resposta = await fetch(`/api/Usuarios/${usuario.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nome: usuario.nome, perfil: usuario.perfil, ativo: usuario.ativo })
  });

  exigirLogin(resposta);
  if (!resposta.ok) throw new Error(await lerErro(resposta, 'Não foi possível salvar.'));
}

// Trocar perfil ou ativo grava na hora
$('listaUsuarios').addEventListener('change', async e => {
  const linha = e.target.closest('tr');
  const usuario = { ...usuarios.find(u => u.id == linha.dataset.id) };

  if (e.target.dataset.campo === 'perfil') usuario.perfil = e.target.value;
  if (e.target.dataset.campo === 'ativo') usuario.ativo = e.target.checked;

  try {
    await salvar(usuario);
    aviso(`${usuario.nome} atualizado.`);
  } catch (erro) {
    aviso(erro.message);
  }
  await carregar();
});

$('listaUsuarios').addEventListener('click', async e => {
  if (!e.target.closest('[data-senha]')) return;
  const usuario = usuarios.find(u => u.id == e.target.closest('tr').dataset.id);

  const senha = prompt(`Nova senha para ${usuario.nome} (mínimo 8 caracteres):`);
  if (!senha) return;

  const resposta = await fetch(`/api/Usuarios/${usuario.id}/senha`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ senha })
  });

  exigirLogin(resposta);
  aviso(resposta.ok ? 'Senha redefinida.' : await lerErro(resposta, 'Não foi possível redefinir.'));
});

$('formUsuario').addEventListener('submit', async e => {
  e.preventDefault();
  const form = e.target;
  form.classList.add('was-validated');
  if (!form.checkValidity()) return;

  try {
    await apiPost('Usuarios', {
      nome: $('nome').value,
      email: $('email').value,
      senha: $('senha').value,
      perfil: $('perfil').value
    });

    bootstrap.Offcanvas.getInstance($('novoUsuario')).hide();
    form.reset();
    form.classList.remove('was-validated');
    $('erroSalvar').classList.add('d-none');
    aviso('Usuário criado.');
    await carregar();
  } catch (erro) {
    $('erroSalvar').textContent = erro.message;
    $('erroSalvar').classList.remove('d-none');
  }
});

$('perfil').innerHTML = opcoesPerfil('Tesouraria');
carregar();