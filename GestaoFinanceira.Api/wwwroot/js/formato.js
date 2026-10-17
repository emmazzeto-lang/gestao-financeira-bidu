// Formatação no padrão brasileiro

const formatoMoeda = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2
});

function moeda(valor) {
  return formatoMoeda.format(Number(valor) || 0);
}

function dataCurta(valor) {
  const d = new Date(valor);
  if (isNaN(d)) return '';
  return d.toLocaleDateString('pt-BR', { timeZone: 'UTC' });
}

// "8.420,90" -> 8420.9
function lerMoeda(texto) {
  if (!texto) return NaN;
  const limpo = String(texto).replace(/[^\d,-]/g, '').replace(',', '.');
  return parseFloat(limpo);
}

function escaparHtml(texto) {
  const div = document.createElement('div');
  div.textContent = texto ?? '';
  return div.innerHTML;
}
