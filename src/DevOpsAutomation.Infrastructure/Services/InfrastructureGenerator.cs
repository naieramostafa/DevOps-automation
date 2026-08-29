using DevOpsAutomation.Core.Abstractions;
using DevOpsAutomation.Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevOpsAutomation.Infrastructure.Services;

public class InfrastructureGenerator : IInfrastructureGenerator
{
    private readonly ISerializer _yamlSerializer;

    public InfrastructureGenerator()
    {
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public Task<GeneratedArtifact> GenerateDockerfileAsync(string serviceName, int port, string language = "dotnet")
    {
        var content = language.ToLowerInvariant() switch
        {
            "node" or "nodejs" or "javascript" or "typescript" => GenerateNodeDockerfile(serviceName, port),
            "python" or "py" => GeneratePythonDockerfile(serviceName, port),
            "go" or "golang" => GenerateGoDockerfile(serviceName, port),
            "java" or "maven" => GenerateJavaMavenDockerfile(serviceName, port),
            "java-gradle" or "gradle" => GenerateJavaGradleDockerfile(serviceName, port),
            _ => GenerateDotNetDockerfile(serviceName, port)
        };

        return Task.FromResult(new GeneratedArtifact
        {
            Name = "Dockerfile",
            Content = content,
            FileType = "dockerfile"
        });
    }

    private string GenerateDotNetDockerfile(string serviceName, int port)
    {
        return $$"""
            FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
            WORKDIR /app
            EXPOSE {{port}}

            FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
            WORKDIR /src
            COPY ["{{serviceName}}/{{serviceName}}.csproj", "{{serviceName}}/"]
            RUN dotnet restore "{{serviceName}}/{{serviceName}}.csproj"
            COPY . .
            WORKDIR "/src/{{serviceName}}"
            RUN dotnet build "{{serviceName}}.csproj" -c Release -o /app/build

            FROM build AS publish
            RUN dotnet publish "{{serviceName}}.csproj" -c Release -o /app/publish

            FROM base AS final
            WORKDIR /app
            COPY --from=publish /app/publish .
            ENTRYPOINT ["dotnet", "{{serviceName}}.dll"]
            """;
    }

    private static string GenerateNodeDockerfile(string serviceName, int port)
    {
        return $$"""
            FROM node:20-alpine AS build
            WORKDIR /app
            COPY package*.json ./
            RUN npm install
            COPY . .
            RUN npm run build

            FROM node:20-alpine AS runtime
            WORKDIR /app
            COPY --from=build /app/dist ./dist
            COPY --from=build /app/node_modules ./node_modules
            COPY --from=build /app/package.json ./
            EXPOSE {{port}}
            ENTRYPOINT ["node", "dist/main.js"]
            """;
    }

    private static string GeneratePythonDockerfile(string serviceName, int port)
    {
        return $$"""
            FROM python:3.12-slim AS runtime
            WORKDIR /app
            COPY requirements.txt ./
            RUN pip install --no-cache-dir -r requirements.txt
            COPY . .
            EXPOSE {{port}}
            ENTRYPOINT ["python", "main.py"]
            """;
    }

    private static string GenerateGoDockerfile(string serviceName, int port)
    {
        return $$"""
            FROM golang:1.22 AS build
            WORKDIR /app
            COPY go.mod go.sum ./
            RUN go mod download
            COPY . .
            RUN CGO_ENABLED=0 GOOS=linux go build -o /app/{{serviceName}} .

            FROM alpine:3.19 AS runtime
            WORKDIR /app
            COPY --from=build /app/{{serviceName}} .
            EXPOSE {{port}}
            ENTRYPOINT ["./{{serviceName}}"]
            """;
    }

    private static string GenerateJavaMavenDockerfile(string serviceName, int port)
    {
        return $$"""
            FROM eclipse-temurin:21-jdk AS build
            WORKDIR /app
            COPY pom.xml ./
            COPY src ./src
            RUN mvn package -DskipTests

            FROM eclipse-temurin:21-jre AS runtime
            WORKDIR /app
            COPY --from=build /app/target/*.jar app.jar
            EXPOSE {{port}}
            ENTRYPOINT ["java", "-jar", "app.jar"]
            """;
    }

    private static string GenerateJavaGradleDockerfile(string serviceName, int port)
    {
        return $$"""
            FROM eclipse-temurin:21-jdk AS build
            WORKDIR /app
            COPY build.gradle settings.gradle ./
            COPY src ./src
            RUN gradle bootJar -x test

            FROM eclipse-temurin:21-jre AS runtime
            WORKDIR /app
            COPY --from=build /app/build/libs/*.jar app.jar
            EXPOSE {{port}}
            ENTRYPOINT ["java", "-jar", "app.jar"]
            """;
    }

    public Task<GeneratedArtifact> GenerateKubernetesManifestAsync(string serviceName, int port, int replicas = 1, string language = "dotnet")
    {
        var (livenessPath, readinessPath) = language.ToLowerInvariant() switch
        {
            "node" or "nodejs" => ("/health", "/ready"),
            "python" or "py" => ("/health", "/ready"),
            "go" or "golang" => ("/healthz", "/readyz"),
            "java" or "maven" or "gradle" => ("/actuator/health", "/actuator/health"),
            _ => ("/health", "/health")
        };

        var deployment = new Dictionary<string, object>
        {
            ["apiVersion"] = "apps/v1",
            ["kind"] = "Deployment",
            ["metadata"] = new Dictionary<string, object>
            {
                ["name"] = serviceName,
                ["labels"] = new Dictionary<string, string> { ["app"] = serviceName }
            },
            ["spec"] = new Dictionary<string, object>
            {
                ["replicas"] = replicas,
                ["selector"] = new Dictionary<string, object>
                {
                    ["matchLabels"] = new Dictionary<string, string> { ["app"] = serviceName }
                },
                ["template"] = new Dictionary<string, object>
                {
                    ["metadata"] = new Dictionary<string, object>
                    {
                        ["labels"] = new Dictionary<string, string> { ["app"] = serviceName }
                    },
                    ["spec"] = new Dictionary<string, object>
                    {
                        ["containers"] = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["name"] = serviceName,
                                ["image"] = $"{serviceName}:latest",
                                ["ports"] = new List<object>
                                {
                                    new Dictionary<string, object>
                                    {
                                        ["containerPort"] = port
                                    }
                                },
                                ["livenessProbe"] = new Dictionary<string, object>
                                {
                                    ["httpGet"] = new Dictionary<string, object>
                                    {
                                        ["path"] = livenessPath,
                                        ["port"] = port
                                    },
                                    ["initialDelaySeconds"] = 5,
                                    ["periodSeconds"] = 10
                                },
                                ["readinessProbe"] = new Dictionary<string, object>
                                {
                                    ["httpGet"] = new Dictionary<string, object>
                                    {
                                        ["path"] = readinessPath,
                                        ["port"] = port
                                    },
                                    ["initialDelaySeconds"] = 3,
                                    ["periodSeconds"] = 5
                                }
                            }
                        }
                    }
                }
            }
        };

        var service = new Dictionary<string, object>
        {
            ["apiVersion"] = "v1",
            ["kind"] = "Service",
            ["metadata"] = new Dictionary<string, object>
            {
                ["name"] = $"{serviceName}-svc"
            },
            ["spec"] = new Dictionary<string, object>
            {
                ["selector"] = new Dictionary<string, string> { ["app"] = serviceName },
                ["ports"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["protocol"] = "TCP",
                        ["port"] = port,
                        ["targetPort"] = port
                    }
                }
            }
        };

        var manifest = new List<object> { deployment, service };
        var yaml = _yamlSerializer.Serialize(manifest);

        return Task.FromResult(new GeneratedArtifact
        {
            Name = "k8s-manifest.yaml",
            Content = yaml,
            FileType = "yaml"
        });
    }

    public Task<GeneratedArtifact> GenerateGitHubActionsWorkflowAsync(string serviceName, string dockerRegistry, string language = "dotnet")
    {
        var content = language.ToLowerInvariant() switch
        {
            "node" or "nodejs" => GenerateNodeWorkflow(serviceName, dockerRegistry),
            "python" or "py" => GeneratePythonWorkflow(serviceName, dockerRegistry),
            "go" or "golang" => GenerateGoWorkflow(serviceName, dockerRegistry),
            "java" or "maven" => GenerateJavaMavenWorkflow(serviceName, dockerRegistry),
            "java-gradle" or "gradle" => GenerateJavaGradleWorkflow(serviceName, dockerRegistry),
            _ => GenerateDotNetWorkflow(serviceName, dockerRegistry)
        };

        return Task.FromResult(new GeneratedArtifact
        {
            Name = "deploy.yml",
            Content = content,
            FileType = "yaml"
        });
    }

    private static string GenerateDotNetWorkflow(string serviceName, string dockerRegistry)
    {
        return $$"""
            name: Build and Deploy {{serviceName}}

            on:
              push:
                branches: [main]
              pull_request:
                branches: [main]

            jobs:
              build:
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4
                  - name: Setup .NET
                    uses: actions/setup-dotnet@v4
                    with:
                      dotnet-version: 8.0.x
                  - name: Restore
                    run: dotnet restore
                  - name: Build
                    run: dotnet build --no-restore -c Release
                  - name: Test
                    run: dotnet test --no-build -c Release
                  - name: Log in to registry
                    run: echo "${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}" | docker login {{dockerRegistry}} -u ${{ "{{" }} github.actor {{ "}}" }} --password-stdin
                  - name: Build and push Docker image
                    uses: docker/build-push-action@v5
                    with:
                      context: .
                      push: true
                      tags: {{dockerRegistry}}/{{serviceName}}:${{ "{{" }} github.sha {{ "}}" }}
              deploy:
                needs: build
                if: github.ref == 'refs/heads/main'
                runs-on: ubuntu-latest
                steps:
                  - name: Deploy to cluster
                    run: echo "Deploying {{serviceName}}..."
            """;
    }

    private static string GenerateNodeWorkflow(string serviceName, string dockerRegistry)
    {
        return $$"""
            name: Build and Deploy {{serviceName}}

            on:
              push:
                branches: [main]
              pull_request:
                branches: [main]

            jobs:
              build:
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4
                  - name: Setup Node.js
                    uses: actions/setup-node@v4
                    with:
                      node-version: 20.x
                      cache: npm
                  - name: Install dependencies
                    run: npm ci
                  - name: Build
                    run: npm run build
                  - name: Test
                    run: npm test
                  - name: Log in to registry
                    run: echo "${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}" | docker login {{dockerRegistry}} -u ${{ "{{" }} github.actor {{ "}}" }} --password-stdin
                  - name: Build and push Docker image
                    uses: docker/build-push-action@v5
                    with:
                      context: .
                      push: true
                      tags: {{dockerRegistry}}/{{serviceName}}:${{ "{{" }} github.sha {{ "}}" }}
              deploy:
                needs: build
                if: github.ref == 'refs/heads/main'
                runs-on: ubuntu-latest
                steps:
                  - name: Deploy to cluster
                    run: echo "Deploying {{serviceName}}..."
            """;
    }

    private static string GeneratePythonWorkflow(string serviceName, string dockerRegistry)
    {
        return $$"""
            name: Build and Deploy {{serviceName}}

            on:
              push:
                branches: [main]
              pull_request:
                branches: [main]

            jobs:
              build:
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4
                  - name: Setup Python
                    uses: actions/setup-python@v5
                    with:
                      python-version: 3.12
                      cache: pip
                  - name: Install dependencies
                    run: pip install -r requirements.txt
                  - name: Test
                    run: pytest
                  - name: Log in to registry
                    run: echo "${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}" | docker login {{dockerRegistry}} -u ${{ "{{" }} github.actor {{ "}}" }} --password-stdin
                  - name: Build and push Docker image
                    uses: docker/build-push-action@v5
                    with:
                      context: .
                      push: true
                      tags: {{dockerRegistry}}/{{serviceName}}:${{ "{{" }} github.sha {{ "}}" }}
              deploy:
                needs: build
                if: github.ref == 'refs/heads/main'
                runs-on: ubuntu-latest
                steps:
                  - name: Deploy to cluster
                    run: echo "Deploying {{serviceName}}..."
            """;
    }

    private static string GenerateGoWorkflow(string serviceName, string dockerRegistry)
    {
        return $$"""
            name: Build and Deploy {{serviceName}}

            on:
              push:
                branches: [main]
              pull_request:
                branches: [main]

            jobs:
              build:
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4
                  - name: Setup Go
                    uses: actions/setup-go@v5
                    with:
                      go-version: 1.22.x
                      cache: true
                  - name: Build
                    run: go build -o {{serviceName}} .
                  - name: Test
                    run: go test ./...
                  - name: Log in to registry
                    run: echo "${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}" | docker login {{dockerRegistry}} -u ${{ "{{" }} github.actor {{ "}}" }} --password-stdin
                  - name: Build and push Docker image
                    uses: docker/build-push-action@v5
                    with:
                      context: .
                      push: true
                      tags: {{dockerRegistry}}/{{serviceName}}:${{ "{{" }} github.sha {{ "}}" }}
              deploy:
                needs: build
                if: github.ref == 'refs/heads/main'
                runs-on: ubuntu-latest
                steps:
                  - name: Deploy to cluster
                    run: echo "Deploying {{serviceName}}..."
            """;
    }

    private static string GenerateJavaMavenWorkflow(string serviceName, string dockerRegistry)
    {
        return $$"""
            name: Build and Deploy {{serviceName}}

            on:
              push:
                branches: [main]
              pull_request:
                branches: [main]

            jobs:
              build:
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4
                  - name: Setup Java
                    uses: actions/setup-java@v4
                    with:
                      java-version: 21
                      distribution: temurin
                      cache: maven
                  - name: Build
                    run: mvn package -DskipTests
                  - name: Test
                    run: mvn test
                  - name: Log in to registry
                    run: echo "${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}" | docker login {{dockerRegistry}} -u ${{ "{{" }} github.actor {{ "}}" }} --password-stdin
                  - name: Build and push Docker image
                    uses: docker/build-push-action@v5
                    with:
                      context: .
                      push: true
                      tags: {{dockerRegistry}}/{{serviceName}}:${{ "{{" }} github.sha {{ "}}" }}
              deploy:
                needs: build
                if: github.ref == 'refs/heads/main'
                runs-on: ubuntu-latest
                steps:
                  - name: Deploy to cluster
                    run: echo "Deploying {{serviceName}}..."
            """;
    }

    private static string GenerateJavaGradleWorkflow(string serviceName, string dockerRegistry)
    {
        return $$"""
            name: Build and Deploy {{serviceName}}

            on:
              push:
                branches: [main]
              pull_request:
                branches: [main]

            jobs:
              build:
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4
                  - name: Setup Java
                    uses: actions/setup-java@v4
                    with:
                      java-version: 21
                      distribution: temurin
                      cache: gradle
                  - name: Build
                    run: gradle bootJar -x test
                  - name: Test
                    run: gradle test
                  - name: Log in to registry
                    run: echo "${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}" | docker login {{dockerRegistry}} -u ${{ "{{" }} github.actor {{ "}}" }} --password-stdin
                  - name: Build and push Docker image
                    uses: docker/build-push-action@v5
                    with:
                      context: .
                      push: true
                      tags: {{dockerRegistry}}/{{serviceName}}:${{ "{{" }} github.sha {{ "}}" }}
              deploy:
                needs: build
                if: github.ref == 'refs/heads/main'
                runs-on: ubuntu-latest
                steps:
                  - name: Deploy to cluster
                    run: echo "Deploying {{serviceName}}..."
            """;
    }
}
