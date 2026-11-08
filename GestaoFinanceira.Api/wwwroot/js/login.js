// Tela de login: envia e-mail e senha; a API responde com o cookie de sessão

const form = document.getElementById('formLogin');
const erro = document.getElementById('erroLogin');
const botao = document.getElementById('botaoEntrar');

// Quem já tem sessão aberta vai direto para o painel
fetch('/api/Auth/eu').then(resposta => {
  if (resposta.ok) window.location.href = 'painel.html';
});

form.addEventListener('submit', async e => {
  e.preventDefault();
  erro.classList.add('d-none');
  form.classList.add('was-validated');
  if (!form.checkValidity()) return;

  botao.disabled = true;

  try {
    const resposta = await fetch('/api/Auth/entrar', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email: document.getElementById('email').value,
        senha: document.getElementById('senha').value,
        manterConectado: document.getElementById('manter').checked
      })
    });

    if (resposta.ok) {
      window.location.href = 'painel.html';
      return;
    }

    const corpo = await resposta.json().catch(() => ({}));
    erro.textContent = corpo.mensagem || 'Não foi possível entrar.';
  } catch {
    erro.textContent = 'Não foi possível falar com a API. Verifique se ela está rodando.';
  }

  erro.classList.remove('d-none');
  botao.disabled = false;
});