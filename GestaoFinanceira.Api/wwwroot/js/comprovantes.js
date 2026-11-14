// Notas e comprovantes: o que cada despesa já tem, download e envio do que falta

const $ = id => document.getElementById(id);

let despesas = [];
let filtro = '';
let despesaDoAnexo = null;

function aviso(texto) {
  const toast = $('aviso');
  toast.querySelector('.toast-body').textContent = texto;
  bootstrap.Toast.getOrCreateInstance(toast).show();
}

async function carregar() {
  despesas = await apiGet('Documentos/situacao');
  desenhar();
}

function desenhar() {
  const busca = $('busca').value.trim().toLowerCase();
  const sem = despesas.filter(d => !d.documentos.length);

  $('qtdTodas').textContent = despesas.length;
  $('qtdCom').textContent = despesas.length - sem.length;
  $('qtdSem').textContent = sem.length;

  const visiveis = despesas.filter(d => {
    if (filtro === 'com' && !d.documentos.length) return false;
    if (filtro === 'sem' && d.documentos.length) return false;
    if (!busca) return true;

    // O valor entra nos dois jeitos de digitar: 1.234,56 e 1234,56
    const texto = [d.descricao, d.fornecedor ?? '', String(d.despesaId), moeda(d.valor),
      String(d.valor).replace('.', ','), ...d.documentos.map(doc => doc.numero)]
      .join(' ').toLowerCase();
    return texto.includes(busca);
  });

  $('lista').innerHTML = visiveis.length
    ? visiveis.map(d => {
      // O link vai direto para a API; o cookie de sessão vai junto
      const docs = d.documentos.length
        ? d.documentos.map(doc => `
            <a class="d-block text-truncate" href="/api/Documentos/${doc.id}/arquivo" title="${escaparHtml(doc.nomeOriginal)}">
              <i class="bi bi-download"></i> ${escaparHtml(`${doc.tipo} ${doc.numero}`)}
              <small class="text-secondary">${Math.max(1, Math.round(doc.tamanho / 1024))} KB</small>
            </a>`).join('')
        : '<span class="conferencia falta"><i class="bi bi-exclamation-circle"></i> sem comprovante</span>';

      return `
        <tr style="vertical-align: middle">
          <td class="numero">${d.despesaId}</td>
          <td class="data">${dataCurta(d.dataPagamento)}</td>
          <td><div class="historico">${escaparHtml(d.descricao)}</div>
              <div class="complemento">${escaparHtml(d.fornecedor ?? 'Sem fornecedor')}</div></td>
          <td class="valor debito">${moeda(d.valor)}</td>
          <td>${docs}</td>
          <td class="text-end"><button class="btn btn-outline-secondary btn-sm" type="button" data-anexar="${d.despesaId}" data-perfis="Administrador,Tesouraria">Anexar</button></td>
        </tr>`;
    }).join('')
    : '<tr><td colspan="6" class="ausente">Nenhuma despesa encontrada.</td></tr>';

  aplicarPerfis();
}

$('abas').addEventListener('click', e => {
  const botao = e.target.closest('button');
  if (!botao) return;
  document.querySelectorAll('#abas button').forEach(b => b.classList.toggle('ativa', b === botao));
  filtro = botao.dataset.filtro;
  desenhar();
});

$('busca').addEventListener('input', desenhar);

$('lista').addEventListener('click', e => {
  const botao = e.target.closest('[data-anexar]');
  if (!botao) return;

  despesaDoAnexo = despesas.find(d => d.despesaId == botao.dataset.anexar);
  $('despesaDoAnexo').textContent = `Despesa Nº ${despesaDoAnexo.despesaId} - ${despesaDoAnexo.descricao} - R$ ${moeda(despesaDoAnexo.valor)}`;
  $('formAnexo').reset();
  $('erroAnexo').classList.add('d-none');
  bootstrap.Offcanvas.getOrCreateInstance($('painelAnexo')).show();
});

$('formAnexo').addEventListener('submit', async e => {
  e.preventDefault();

  const envio = new FormData();
  envio.append('despesaId', despesaDoAnexo.despesaId);
  envio.append('tipo', $('tipo').value);
  envio.append('numero', $('numero').value.trim());
  if ($('arquivo').files[0]) envio.append('arquivo', $('arquivo').files[0]);

  try {
    // A API confere o arquivo, o tamanho e se a nota já foi usada em outra despesa
    await apiEnviarArquivo('Documentos', envio);
    bootstrap.Offcanvas.getInstance($('painelAnexo')).hide();
    aviso(`Comprovante anexado à despesa Nº ${despesaDoAnexo.despesaId}.`);
    carregarContadores();
    await carregar();
  } catch (erro) {
    $('erroAnexo').textContent = erro.message;
    $('erroAnexo').classList.remove('d-none');
  }
});

// Busca vinda do topo: comprovantes.html?busca=texto
$('busca').value = new URLSearchParams(window.location.search).get('busca') ?? '';
carregar().catch(erro => aviso(erro.message));