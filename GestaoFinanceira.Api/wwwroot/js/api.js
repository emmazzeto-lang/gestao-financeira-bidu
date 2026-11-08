// Chamadas para a API (mesma origem, servida pelo próprio ASP.NET)

const API_BASE = '/api';

// Sem sessão (401): volta para a tela de login
function exigirLogin(resposta) {
  if (resposta.status === 401) {
    window.location.href = 'index.html';
    throw new Error('Sua sessão terminou. Entre novamente.');
  }
}

// A API devolve { mensagem: "..." } quando recusa algo
async function lerErro(resposta, padrao) {
    // 403: está logado, mas o perfil não permite esta ação
  if (resposta.status === 403) return 'Seu perfil não tem permissão para esta ação.';

  try {
    const erro = await resposta.json();
    return erro.mensagem || erro.title || padrao;
  } catch {
    return padrao;
  }
}

async function apiGet(rota) {
  const resposta = await fetch(`${API_BASE}/${rota}`);
  exigirLogin(resposta);
  if (!resposta.ok) throw new Error(`Erro ${resposta.status} ao buscar ${rota}`);
  return resposta.json();
}

async function apiPost(rota, corpo) {
  const resposta = await fetch(`${API_BASE}/${rota}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(corpo)
  });
  exigirLogin(resposta);
  if (!resposta.ok) throw new Error(await lerErro(resposta, `Erro ${resposta.status} ao salvar`));
  // 204 (No Content) não tem corpo para ler
  return resposta.status === 204 ? null : resposta.json();
}

// Para arquivos: o navegador monta o multipart sozinho a partir do FormData
async function apiEnviarArquivo(rota, formulario) {
  const resposta = await fetch(`${API_BASE}/${rota}`, {
    method: 'POST',
    body: formulario
  });
  exigirLogin(resposta);
  if (!resposta.ok) throw new Error(await lerErro(resposta, `Erro ${resposta.status} ao enviar o arquivo`));
  return resposta.json();
}

async function apiDelete(rota) {
  const resposta = await fetch(`${API_BASE}/${rota}`, { method: 'DELETE' });
  exigirLogin(resposta);
  if (!resposta.ok) throw new Error(await lerErro(resposta, `Erro ${resposta.status} ao excluir`));
}