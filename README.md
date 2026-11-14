# Sistema de Gestão Financeira e Prestação de Contas
Desenvolvido para a Fundação Amigos de João Bidu, como parte do projeto Fábrica de Software.

**Objetivo geral**
Modernizar a gestão da Fundação, reduzir processos manuais e centralizar informações para organizar receitas, despesas, notas fiscais, documentos, contas bancárias, metas e fontes de recursos, com transparência e agilidade na prestação de contas.

## O que o sistema faz
* **Cadastros:** parcerias (termos de fomento), planos de trabalho e metas, fornecedores (CPF/CNPJ validado) e contas bancárias.
* **Movimento:** receitas e despesas ligadas a uma meta do plano; toda despesa entra com a nota ou o comprovante.
* **Conciliação:** importa o extrato OFX do banco e confere cada movimento com o que foi lançado.
* **Prestação de contas:** execução por meta e fonte de recurso, exportação para Excel e rastreio de cada despesa.
* **Acesso:** login com perfis (Administrador, Tesouraria, Diretoria e Consulta) e registro de quem lançou cada valor.

## Tecnologias
* ASP.NET Core 10 (C#) e Entity Framework Core
* PostgreSQL 15
* HTML, CSS, JavaScript e Bootstrap 5 (telas servidas pela própria API)
* Docker e Caddy (HTTPS) no servidor
* xUnit nos testes

## Rodar no computador (desenvolvimento)
Pré-requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download), [Docker Desktop](https://www.docker.com/products/docker-desktop) e Git.

1. Clone o repositório e entre na branch de desenvolvimento:
   ```
   git clone https://github.com/emmazzeto-lang/gestao-financeira-bidu.git
   cd gestao-financeira-bidu
   git checkout develop
   ```
2. Suba o banco de desenvolvimento: `docker compose up -d`
3. Crie as tabelas: `cd GestaoFinanceira.Api` e depois `dotnet ef database update`
4. Rode a API: `dotnet run`
5. Abra `http://localhost:5108`. No primeiro acesso o sistema pede para criar o administrador.

A conexão de desenvolvimento fica em `appsettings.Development.json` e só vale no computador.
A documentação da API fica em `http://localhost:5108/swagger`.

Para rodar os testes, na pasta raiz: `dotnet test`

## Colocar no servidor (produção)
Pré-requisitos no servidor: Docker com o Compose, as portas 80 e 443 liberadas e um domínio apontado para o IP do servidor.

1. Clone o repositório no servidor, na branch `main`.
2. Copie `.env.exemplo` para `.env` e preencha a senha do banco e o domínio. O `.env` não vai para o Git.
3. Suba tudo:
   ```
   docker compose -f docker-compose.producao.yml up -d --build
   ```
4. Abra `https://seu-dominio` e crie o administrador no primeiro acesso.

O Caddy pede e renova o certificado HTTPS sozinho. A API atualiza as tabelas do banco sozinha ao iniciar.

**Atualizar para uma versão nova:** `git pull` e o mesmo comando do passo 3. Os dados ficam nos volumes do Docker e não se perdem.

**Esqueceu a senha:**
```
docker compose -f docker-compose.producao.yml exec api dotnet GestaoFinanceira.Api.dll redefinir-senha email@fundacao.org NovaSenha123
```
No computador, o mesmo comando é `dotnet run -- redefinir-senha email@fundacao.org NovaSenha123`.

## Cópias de segurança
`sh scripts/backup.sh` salva o banco e os comprovantes na pasta `backups/`, com a data no nome, e apaga os arquivos com mais de 30 dias. O próprio script explica como agendar para rodar todo dia.

Copie a pasta `backups/` para fora do servidor (um disco externo ou um serviço de nuvem da Fundação). Uma cópia que fica só no servidor se perde junto com ele.

**Restaurar** (apaga os dados atuais e volta para os da cópia):
```
docker compose -f docker-compose.producao.yml stop api
docker compose -f docker-compose.producao.yml exec -T db psql -U gestao -d postgres -c "DROP DATABASE gestao_financeira WITH (FORCE)" -c "CREATE DATABASE gestao_financeira"
gunzip -c backups/banco-AAAA-MM-DD.sql.gz | docker compose -f docker-compose.producao.yml exec -T db psql -U gestao -d gestao_financeira
docker compose -f docker-compose.producao.yml start api
docker compose -f docker-compose.producao.yml exec -T api tar -xzf - -C /app/Armazenamento < backups/comprovantes-AAAA-MM-DD.tar.gz
```

## Organização do código
* `GestaoFinanceira.Api/Models`: tabelas do banco e dados que a API recebe e devolve
* `GestaoFinanceira.Api/Services`: regras de negócio (validações, conciliação, relatórios)
* `GestaoFinanceira.Api/Controllers`: rotas da API
* `GestaoFinanceira.Api/Migrations`: histórico das mudanças no banco
* `GestaoFinanceira.Api/wwwroot`: telas (`css/` para o visual, `js/layout.js` para o menu e o topo, `js/api.js` para as chamadas)
* `GestaoFinanceira.Tests`: testes automáticos