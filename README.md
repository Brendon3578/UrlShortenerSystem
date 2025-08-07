# UrlShortenerSystem

Este projeto é uma API RESTful desenvolvida em C# utilizando o ASP.NET Core 8 com Minimal API, destinada ao encurtamento de URLs. A aplicação permite operações de criação, redirecionamento, listagem e exclusão de URLs encurtadas, com persistência dos dados em um banco de dados SQLite local.

## 💻 Descrição

O sistema foi projetado para facilitar o encurtamento de URLs longas, gerando códigos únicos de 6 caracteres e oferecendo rastreamento de cliques para análise de uso.

## 🔮 Funcionalidades

- **Encurtamento de URLs**: Gera códigos únicos de 6 caracteres para URLs longas.
- **Redirecionamento Inteligente**: Redireciona automaticamente para a URL original e incrementa contador de cliques.
- **Gerenciamento de URLs**: Permite listar e excluir URLs encurtadas cadastradas.
- **Rastreamento de Cliques**: Contador automático de acessos para cada URL encurtada.
- **Validação de URLs**: Verifica se a URL fornecida é válida antes do encurtamento.

## 📊 Estrutura da Entidade

### ShortUrl (URL Encurtada)

- **Atributos**: `Id` (GUID), `OriginalUrl` (string), `ShortCode` (string), `CreatedAt` (DateTime), `Clicks` (int)
- **Índices**: Índice único no campo `ShortCode` para garantir códigos únicos

## 🛠️ Tecnologias Utilizadas

- **C# .NET 8**
- **ASP.NET Core 8 Minimal API**
- **Entity Framework Core**
- **SQLite**
- **Swagger/OpenAPI**

## ✨ Padrões e Práticas Aplicadas

- **Minimal API**: Utilizado para criação de endpoints RESTful de forma simplificada.
- **Entity Framework Core**: ORM para persistência de dados com Code First.
- **Dependency Injection**: Injeção de dependência nativa do ASP.NET Core.
- **DTOs (Data Transfer Objects)**: Utilizados para requisições e respostas da API.
- **CORS**: Configurado para permitir requisições cross-origin em desenvolvimento.
- **Swagger/OpenAPI**: Documentação automática da API com interface interativa.
- **Separação de Responsabilidades**: Código organizado em arquivos específicos (Models, Data, DTOs).

## 📂 Estrutura do Projeto

```
UrlShortenerSystem/
├── Models/
│   └── ShortUrl.cs                 # Entidade principal do sistema
├── Data/
│   └── UrlShortenerContext.cs      # Contexto do Entity Framework
├── DTOs/
│   ├── CreateURLRequestDTO.cs      # DTO para criação de URLs
│   └── UrlResponseDTO.cs           # DTO para resposta da API
├── Utils/
│   └── Generators.cs               # Gerador de códigos únicos
├── Program.cs                      # Configuração da API e endpoints
├── appsettings.json               # Configurações da aplicação
└── UrlShortenerSystem.csproj      # Arquivo do projeto
```

## 🚀 Configuração do Ambiente

1. **Clone ou crie o projeto:**

   ```bash
   dotnet new web -n UrlShortenerSystem
   cd UrlShortenerSystem
   ```

2. **Instale os pacotes necessários:**

   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add package Microsoft.EntityFrameworkCore.Tools
   dotnet add package Microsoft.AspNetCore.OpenApi
   dotnet add package Swashbuckle.AspNetCore
   ```

3. **Restaure as dependências:**

   ```bash
   dotnet restore
   ```

4. **Configure a string de conexão no `appsettings.json` (opcional - SQLite será criado automaticamente).**

5. **Inicie a aplicação:**

   ```bash
   dotnet run
   ```

A API estará disponível em:
- **HTTPS**: `https://localhost:5001`
- **HTTP**: `http://localhost:5000`
- **Swagger UI**: Disponível na raiz da aplicação (`/`)

## 📋 Endpoints da API

### **POST /urls**
Cria uma nova URL encurtada.

**Requisição:**
```json
{
  "originalUrl": "https://www.exemplo.com.br"
}
```

**Resposta:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "originalUrl": "https://www.exemplo.com.br",
  "shortCode": "AbCd3f",
  "createdAt": "2024-01-15T10:30:00Z",
  "clicks": 0,
  "shortUrl": "https://localhost:5001/AbCd3f"
}
```

### GET /{code}
Redireciona para a URL original e incrementa o contador de cliques.

### GET /urls
Lista todas as URLs encurtadas cadastradas.

### DELETE /urls/{code}
Remove uma URL encurtada do sistema.

### GET /health
Verifica o status da aplicação.

## 🗄️ Banco de Dados

O sistema utiliza SQLite como banco local, que será criado automaticamente no arquivo `urlshortener.db` na primeira execução da aplicação.

### Estrutura da Tabela ShortUrls:

| Campo | Tipo | Descrição |
|-------|------|-----------|
| Id | GUID | Chave primária |
| OriginalUrl | VARCHAR(2048) | URL original fornecida |
| ShortCode | VARCHAR(10) | Código encurtado único |
| CreatedAt | DATETIME | Data/hora de criação |
| Clicks | INTEGER | Contador de acessos |

## 🧪 Testando a API

### **Usando curl:**
```bash
# Criar URL encurtada
curl -X POST "https://localhost:5001/urls" \
  -H "Content-Type: application/json" \
  -d '{"originalUrl": "https://www.github.com"}'

# Listar URLs
curl -X GET "https://localhost:5001/urls"

# Deletar URL
curl -X DELETE "https://localhost:5001/urls/AbCd3f"
```

### **Usando Swagger UI:**
Acesse a raiz da aplicação (`https://localhost:5001`) para uma interface interativa completa.

## 🔧 Recursos Técnicos

- ✅ **Geração automática de códigos únicos** de 6 caracteres
- ✅ **Validação de URLs** antes do encurtamento
- ✅ **Contador de cliques** automático
- ✅ **Índice único** no ShortCode para performance
- ✅ **CORS habilitado** para desenvolvimento
- ✅ **Documentação Swagger** completa
- ✅ **Health check endpoint** para monitoramento
- ✅ **Criação automática do banco** SQLite

---

<h3 align="center">
    Feito com ☕ por <a href="https://github.com/Brendon3578">Brendon Gomes</a>
</h3>