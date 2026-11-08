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


// "11222333000181" -> "11.222.333/0001-81" e "52998224725" -> "529.982.247-25".
// Aceita o CNPJ com letras (12.ABC.345/01DE-35).
function formatarCpfCnpj(texto) {
  const limpo = String(texto ?? '').toUpperCase().replace(/[^0-9A-Z]/g, '');

  if (limpo.length === 14) return limpo.replace(/^(.{2})(.{3})(.{3})(.{4})(.{2})$/, '$1.$2.$3/$4-$5');
  if (limpo.length === 11) return limpo.replace(/^(.{3})(.{3})(.{3})(.{2})$/, '$1.$2.$3-$4');
  return limpo;
}