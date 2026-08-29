# AI-Powered Infrastructure & Test Automation

A .NET 10 tool that uses AI agents (LLM intent parsing and prompt engineering)
to automate DevOps tasks. It generates Dockerfiles, Kubernetes manifests, and
GitHub Actions workflows from a few parameters (in English or in any major
language), and can generate Playwright test scripts for a target URL.

## Projects

| Project | Purpose |
|---|---|
| `DevOpsAutomation.Core` | AI orchestration (`AutomationEngine`, `LlmService`), intent parsing, contracts |
| `DevOpsAutomation.Infrastructure` | Dockerfile, Kubernetes, and GitHub Actions generators (multi-language) |
| `DevOpsAutomation.Testing` | Playwright test script generation |
| `DevOpsAutomation.Api` | ASP.NET Core Web API |
| `DevOpsAutomation.Tests` | xUnit unit tests |

## Prerequisites

- .NET 10 SDK
- A Groq API key (for the AI-powered endpoints only; the basic generators work without one)

## Getting started

```bash
cd src/DevOpsAutomation.Api

# set the LLM provider key (optional, only needed for AI endpoints)
dotnet user-secrets set "LlmSettings:ApiKey" "gsk_your_groq_key"

# optionally protect the API with an API key
dotnet user-secrets set "ApiKey" "your-secret-api-key"

dotnet run
```

The API starts on `http://localhost:5220` (HTTPS: `https://localhost:7220`).
Swagger UI is available at `http://localhost:5220/swagger` in development.

## Configuration

`appsettings.json` holds safe defaults. Override locally with User Secrets
(shown above) or environment variables. Never commit real keys.

| Setting | Default | Description |
|---|---|---|
| `LlmSettings:Provider` | `Groq` | `Groq`, `OpenAI`, or `AzureOpenAI` |
| `LlmSettings:DeploymentName` | `openai/gpt-oss-120b` | Model name / deployment |
| `LlmSettings:ApiKey` | *(empty)* | Provider API key (User Secrets) |
| `ApiKey` | *(empty)* | Optional API key; when set, all endpoints (except `/api/health`) require the `X-Api-Key` header |
| `AllowedOrigins` | `[]` | CORS origins; empty allows all origins (development default — configure explicitly for production) |
| `RateLimit` | `100 req/min/IP` | Global fixed-window rate limit |

## API endpoints

### Infrastructure (no AI key needed)

```bash
curl -X POST "http://localhost:5220/api/Infrastructure/dockerfile?serviceName=orders&port=8080&language=dotnet"
curl -X POST "http://localhost:5220/api/Infrastructure/kubernetes?serviceName=orders&port=8080&replicas=3&language=node"
curl -X POST "http://localhost:5220/api/Infrastructure/cicd?serviceName=orders&dockerRegistry=ghcr.io/myorg&language=python"
```

Supported languages: `dotnet`, `node`, `python`, `go`, `java`, `gradle`.

### Testing

```bash
curl -X POST "http://localhost:5220/api/Testing/run" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example.com", "testScript": null}'
```

### Orchestration (requires a Groq API key)

```bash
# structured request
curl -X POST "http://localhost:5220/api/Orchestration/process" \
  -H "Content-Type: application/json" \
  -d '{"requestType":"Infrastructure","parameters":{"serviceName":"orders","port":"8080","replicas":"2","language":"dotnet"}}'

# natural language
curl -X POST "http://localhost:5220/api/Orchestration/process-natural" \
  -H "Content-Type: application/json" \
  -d '{"prompt":"Set up a Python service called reports on port 9000 with 2 replicas"}'
```

### Health

```bash
curl "http://localhost:5220/api/health"
```

Returns `200 OK` with status. Exempt from API key auth so it can be
used as a Kubernetes liveness/readiness probe.

## Testing & build

```bash
dotnet build
dotnet test
dotnet test --collect:"XPlat Code Coverage"   # coverage via coverlet
```

## CI/CD

`.github/workflows/ci-cd.yml` builds, tests, and publishes the Docker image on
every push to `main`. Configure the `DOCKER_USERNAME` / `DOCKER_PASSWORD`
secrets in GitHub to push to a registry.

## Docker

```bash
docker compose up --build
```

Set your Groq key in `.env` (copy from `.env.example`) before building if you
want the AI endpoints to work in the container.
