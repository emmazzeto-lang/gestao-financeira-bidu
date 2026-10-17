let parcerias = [];

const lista = document.getElementById('listaParcerias');
const resumo = document.getElementById('resumo');
const filtroTexto = document.getElementById('filtroTexto');
const filtroSituacao = document.getElementById('filtroSituacao');

function estaVigente(p) {
  return p.ativo && new Date(p.dataFim) >= new Date();
}

function linhaMensagem(html) {
  lista.innerHTML = `<tr><td colspan="5" class="py-4 text-center text-secondary">${html}</td></tr>`;
}

function desenharLista() {
  const texto = filtroTexto.value.trim().toLowerCase();
  const situacao = filtroSituacao.value;

  const filtradas = parcerias.filter(p => {
    const casaTexto = !texto ||
      p.nome.toLowerCase().includes(texto) ||
      p.termoFomento.toLowerCase().includes(texto);
    const vigente = estaVigente(p);
    const casaSituacao = !situacao || (situacao === 'ativa' ? vigente : !vigente);
    return casaTexto && casaSituacao;
  });

  const total = filtradas.reduce((soma, p) => soma + Number(p.valorTotal), 0);
  resumo.textContent = `${filtradas.length} parceria(s) - R$ ${moeda(total)} em valor total`;

  if (parcerias.length === 0) {
    linhaMensagem('Nenhuma parceria cadastrada ainda. Use <b>Nova parceria</b> para começar.');
    return;
  }

  if (filtradas.length === 0) {
    linhaMensagem('Nenhuma parceria encontrada com esse filtro.');
    return;
  }

  lista.innerHTML = filtradas.map(p => {
    const vigente = estaVigente(p);
    const situacao = vigente
      ? '<span class="conferencia ok"><i class="bi bi-check2"></i> vigente</span>'
      : '<span class="conferencia pendente"><i class="bi bi-circle"></i> encerrada</span>';

    return `
      <tr>
        <td class="numero text-body fw-medium">${escaparHtml(p.termoFomento)}</td>
        <td><div class="historico">${escaparHtml(p.nome)}</div></td>
        <td class="data">${dataCurta(p.dataInicio)} a ${dataCurta(p.dataFim)}</td>
        <td class="valor filete">${moeda(p.valorTotal)}</td>
        <td>${situacao}</td>
      </tr>`;
  }).join('');
}

async function carregarParcerias() {
  try {
    parcerias = await apiGet('Parcerias');
    desenharLista();
  } catch (erro) {
    console.error(erro);
    resumo.textContent = '';
    linhaMensagem(`Não foi possível carregar as parcerias. Verifique se a API e o banco estão rodando.
      <button class="btn btn-sm btn-outline-secondary ms-2" type="button" id="tentarDeNovo">Tentar de novo</button>`);
    document.getElementById('tentarDeNovo').addEventListener('click', carregarParcerias);
  }
}

filtroTexto.addEventListener('input', desenharLista);
filtroSituacao.addEventListener('change', desenharLista);

// ---------- Cadastro ----------

const form = document.getElementById('formParceria');
const campoInicio = document.getElementById('inicio');
const campoFim = document.getElementById('fim');
const campoValor = document.getElementById('valor');
const erroSalvar = document.getElementById('erroSalvar');
const botaoSalvar = document.getElementById('botaoSalvar');

function validarFormulario() {
  let valido = form.checkValidity();

  campoFim.setCustomValidity('');
  if (campoInicio.value && campoFim.value && campoFim.value <= campoInicio.value) {
    campoFim.setCustomValidity('fim antes do início');
    document.getElementById('erroFim').textContent = 'O fim da vigência precisa ser depois do início.';
    valido = false;
  } else {
    document.getElementById('erroFim').textContent = 'Informe a data de fim.';
  }

  const valor = lerMoeda(campoValor.value);
  campoValor.setCustomValidity(valor > 0 ? '' : 'valor inválido');
  if (!(valor > 0)) valido = false;

  form.classList.add('was-validated');
  return valido;
}

campoValor.addEventListener('blur', () => {
  const valor = lerMoeda(campoValor.value);
  if (valor > 0) campoValor.value = moeda(valor);
});

form.addEventListener('submit', async (e) => {
  e.preventDefault();
  erroSalvar.classList.add('d-none');

  if (!validarFormulario()) return;

  const nova = {
    termoFomento: form.termoFomento.value.trim(),
    nome: form.nome.value.trim(),
    // o Npgsql só grava "timestamp with time zone" em UTC, então a data vai com o Z no final
    dataInicio: `${campoInicio.value}T00:00:00Z`,
    dataFim: `${campoFim.value}T00:00:00Z`,
    valorTotal: lerMoeda(campoValor.value),
    ativo: true
  };

  botaoSalvar.disabled = true;
  try {
    const salva = await apiPost('Parcerias', nova);
    parcerias.push(salva);
    desenharLista();

    form.reset();
    form.classList.remove('was-validated');
    bootstrap.Offcanvas.getInstance('#novaParceria').hide();
    bootstrap.Toast.getOrCreateInstance('#avisoSalvo').show();
  } catch (erro) {
    erroSalvar.textContent = `Não foi possível salvar: ${erro.message}`;
    erroSalvar.classList.remove('d-none');
  } finally {
    botaoSalvar.disabled = false;
  }
});

carregarParcerias();
