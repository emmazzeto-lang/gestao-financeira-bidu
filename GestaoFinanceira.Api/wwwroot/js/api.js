// Chamadas para a API (mesma origem, servida pelo próprio ASP.NET)

const API_BASE = '/api';

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

  if (!resposta.ok) {
    let detalhe = '';
    try {
      const erro = await resposta.json();
      detalhe = erro.title || '';
    } catch { }
    throw new Error(detalhe || `Erro ${resposta.status} ao salvar`);
  }

  return resposta.json();
}
