// Chamadas para a API (mesma origem, servida pelo próprio ASP.NET)

const API_BASE = '/api';

// A API devolve { mensagem: "..." } quando recusa algo
async function lerErro(resposta, padrao) {
  try {
    const erro = await resposta.json();
    return erro.mensagem || erro.title || padrao;
  } catch {
    return padrao;
  }
}

async function apiGet(rota) {
  const resposta = await fetch(`${API_BASE}/${rota}`);
  if (!resposta.ok) throw new Error(`Erro ${resposta.status} ao buscar ${rota}`);
  return resposta.json();
}

async function apiPost(rota, corpo) {
  const resposta = await fetch(`${API_BASE}/${rota}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(corpo)
  });

  if (!resposta.ok) throw new Error(await lerErro(resposta, `Erro ${resposta.status} ao salvar`));
  return resposta.json();
}

// Para arquivos: o navegador monta o multipart sozinho a partir do FormData
async function apiEnviarArquivo(rota, formulario) {
  const resposta = await fetch(`${API_BASE}/${rota}`, {
    method: 'POST',
    body: formulario
  });

  if (!resposta.ok) throw new Error(await lerErro(resposta, `Erro ${resposta.status} ao enviar o arquivo`));
  return resposta.json();
}

async function apiDelete(rota) {
  const resposta = await fetch(`${API_BASE}/${rota}`, { method: 'DELETE' });
  if (!resposta.ok) throw new Error(await lerErro(resposta, `Erro ${resposta.status} ao excluir`));
}