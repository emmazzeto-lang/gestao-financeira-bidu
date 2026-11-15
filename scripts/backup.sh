#!/bin/sh
# Cópia de segurança do banco e dos comprovantes. Rode na pasta do projeto, no servidor:
#   sh scripts/backup.sh
# Para rodar todo dia às 2h, coloque no crontab do servidor (crontab -e):
#   0 2 * * * cd /caminho/do/projeto && sh scripts/backup.sh

set -e

COMPOSE="docker compose -f docker-compose.producao.yml"
PASTA=backups
DATA=$(date +%Y-%m-%d)

mkdir -p "$PASTA"

# Banco inteiro em um arquivo .sql compactado
$COMPOSE exec -T db pg_dump -U gestao gestao_financeira | gzip > "$PASTA/banco-$DATA.sql.gz"

# PDFs e imagens dos comprovantes
$COMPOSE exec -T api tar -czf - -C /app/Armazenamento Comprovantes > "$PASTA/comprovantes-$DATA.tar.gz"

# Guarda só os últimos 30 dias
find "$PASTA" -name '*.gz' -mtime +30 -delete

echo "Backup salvo em $PASTA"