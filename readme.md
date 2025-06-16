# Conflux
[![GitHub Issues or Pull Requests](https://img.shields.io/github/issues/team-golfslag/Conflux)](https://github.com/team-golfslag/Conflux/issues)
[![GitHub Issues or Pull Requests](https://img.shields.io/github/issues-pr/team-golfslag/Conflux)](https://github.com/team-golfslag/Conflux/pulls)
[![GitHub deployments](https://img.shields.io/github/deployments/team-golfslag/Conflux/github-pages?label=docfx)
](https://team-golfslag.github.io/Conflux/)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=team-golfslag_Conflux&metric=alert_status)](https://sonarcloud.io/project/overview?id=team-golfslag_Conflux)
[![SonarCloud Coverage](https://sonarcloud.io/api/project_badges/measure?project=team-golfslag_Conflux&metric=coverage)](https://sonarcloud.io/project/overview?id=team-golfslag_Conflux)

Conflux is a backend system designed to manage and integrate research project information. It provides a robust API for interacting with project data and connects with external services like RAiD, SRAM, and NWOpen.

We are using the license described in our [LICENSE](LICENSE) file; please read through it before using our software.

## Table of Contents
- [Project Overview](#project-overview)
- [Prerequisites](#prerequisites)
- [Setup](#setup)
  - [Configuration](#configuration)
  - [Building the Project](#building-the-project)
  - [Running the Project](#running-the-project)
    - [Using .NET CLI](#using-net-cli)
    - [Using Docker](#using-docker)
    - [Full Stack Deployment](#full-stack-deployment)
- [Usage](#usage)
  - [API Access](#api-access)
- [How it Works](#how-it-works)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [License](#license)

## Project Overview

Conflux serves as a central hub for research project metadata. It aims to streamline the management of project details, contributors, and related identifiers by integrating with various research infrastructure services. The system exposes a RESTful API, as detailed in [Conflux.API/Program.cs](Conflux.API/Program.cs), for creating, retrieving, updating, and deleting project information.

## Prerequisites

Before you begin, ensure you have the following installed:
- .NET SDK (the version compatible with this project, typically specified in a `global.json` file or inferable from the project files)
- [Docker](https://www.docker.com/get-started) (Optional, for containerized deployment using [Dockerfile](Dockerfile) and [docker-compose.yml](docker-compose.yml))
- Git
- [Git LFS](https://git-lfs.github.io/) (Required for downloading embedding models)

## Setup

1.  **Clone the repository:**
    ```sh
    git clone <repository-url>
    cd conflux-backend
    ```

2.  **Restore .NET dependencies:**
    ```sh
    dotnet restore Conflux.sln
    ```

3.  **Download embedding models for semantic search:**
    ```sh
    ./download-model.sh
    ```
    This script will download the all-MiniLM-L12-v2.onnx embedding model required for semantic search functionality. The models will be placed in a `Models/` directory and used for local, self-hosted semantic search across 100+ languages.

### Configuration

The application uses `appsettings.json` and environment-specific `appsettings.{Environment}.json` files (e.g., `appsettings.Development.json`) for configuration. These are located in the [`Conflux.API`](Conflux.API) project. Sensitive information and certain environment-specific settings are managed via environment variables.

Key configurations include:
-   **Database Connection String:** Configure your database connection string in `appsettings.Development.json` for local development. For deployed environments (like Docker or cloud services), this is typically overridden by an environment variable named `ConnectionStrings__DefaultConnection`.
    ```json
    // filepath: Conflux.API/appsettings.Development.json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Your_Local_Database_Connection_String_Here"
      }
      // ... other settings
    }
    ```
-   **Integration Settings:** API keys, secrets, and endpoints for external services like RAiD, SRAM, and NWOpen are configured via environment variables. Refer to [`Conflux.API/Program.cs`](Conflux.API/Program.cs) for details on how these are loaded and used.

-   **Embedding Model Settings:** The semantic search functionality uses locally hosted ONNX embedding models. Configuration is in `appsettings.json` under the `EmbeddingModel` section:
    ```json
    // filepath: Conflux.API/appsettings.json
    {
      "EmbeddingModel": {
        "Path": "Models/all-MiniLM-L12-v2.onnx.onnx",
        "TokenizerPath": "Models/vocab.txt",
        "MaxTokens": 512,
        "Dimension": 384
      }
      // ... other settings
    }
    ```
    These models are downloaded automatically using the `download-model.sh` script and provide multilingual semantic search capabilities across 100+ languages.

    **Required Environment Variables (for integrations and environment-specific overrides):**
    Set the following environment variables in your deployment environment or local shell if you need to override `appsettings.json` or provide sensitive data:
    ```sh
    ASPNETCORE_ENVIRONMENT=Development # Or Production, Staging, etc.
    ORCID_CLIENT_SECRET="your_orcid_client_secret"
    SRAM_CLIENT_SECRET="your_sram_client_secret"
    SRAM_SCIM_SECRET="your_sram_scim_secret"
    RAID_BEARER_TOKEN="your_raid_bearer_token"
    # Add any other environment variables required for NWOpen or other integrations
    ```
    The application will read these environment variables at runtime. For local development, you can set these in your shell profile (e.g., `.bashrc`, `.zshrc`), a `.env` file if you use a tool like `dotenv` with `dotnet run`, or directly in your IDE's launch settings.

### Feature Flags

The application supports several feature flags to control its behavior, configured in `appsettings.json` under the `FeatureFlags` section:
-   **`Swagger`**: (boolean) Enables or disables the Swagger UI for API documentation. Default: `true`.
-   **`SeedDatabase`**: (boolean) If true, seeds the database with initial data. Should be `false` in production. Default: `false`.
-   **`DatabaseConnection`**: (boolean) Enables or disables the database connection. Should be `true` for normal operation. Default: `true`.
-   **`SRAMAuthentication`**: (boolean) Enables or disables SRAM authentication. Default: `true`.
-   **`OrcidAuthentication`**: (boolean) Enables or disables ORCID authentication. Default: `true`.
-   **`ReverseProxy`**: (boolean) Set to `true` if the application is running behind a reverse proxy. Default: `true`.
-   **`HttpsRedirect`**: (boolean) Enables or disables automatic redirection to HTTPS. Default: `true`.

    ```json
    // Example FeatureFlags configuration in Conflux.API/appsettings.json
    {
      // ... other settings
      "FeatureFlags": {
        "Swagger": true,
        "SeedDatabase": false,
        "DatabaseConnection": true,
        "SRAMAuthentication": true,
        "OrcidAuthentication": true,
        "ReverseProxy": true,
        "HttpsRedirect": true
      }
    }
    ```

### Building the Project

Build the solution using the .NET CLI:
```sh
dotnet build Conflux.sln --configuration Release
```

### Running the Project

#### Using .NET CLI

1.  **Navigate to the API project directory:**
    ```sh
    cd Conflux.API
    ```

2.  **Run the application:**
    ```sh
    dotnet run
    ```
    By default, the application will be accessible at `http://localhost:5000` and `https://localhost:5001` (standard Kestrel ports). Check the console output for the exact URLs.

#### Using Docker

The project includes a [Dockerfile](Dockerfile) and [docker-compose.yml](docker-compose.yml) for containerized deployment. The Docker image automatically includes the embedding models for semantic search.

**Note:** Make sure to run `./download-model.sh` before building the Docker image to ensure the embedding models are available.

1.  **Build and run using Docker Compose:**
    Ensure your `docker-compose.yml` is configured with necessary environment variables or mounts for settings.
    ```sh
    ./download-model.sh  # Download models first
    docker-compose up --build -d
    ```
    This will build the images and start the services defined in [docker-compose.yml](docker-compose.yml).

2.  **Build and run a single Docker image:**
    ```sh
    ./download-model.sh  # Download models first
    docker build -t conflux-backend .
    # Ensure to pass all required environment variables for configuration
    docker run -p 8080:80 conflux-backend
    ```
    Adjust port mappings and environment variables as needed. The API would then be accessible, for example, at `http://localhost:8080`.

#### Full Stack Deployment

For instructions on how to set up and deploy the complete Conflux application stack (frontend, backend, database, and proxy), please refer to the [conflux-deployment repository](https://github.com/team-golfslag/conflux-deployment).

## Usage

### API Access

Once the application is running:
-   **Swagger UI:** API documentation and a testing interface are available via Swagger UI. As configured in [`Conflux.API/Program.cs`](Conflux.API/Program.cs), navigate to `/swagger` in your browser (e.g., `http://localhost:5000/swagger` or the port you've configured/exposed via Docker).
-   **API Endpoints:** The Swagger UI provides a comprehensive list of available endpoints, request/response schemas, and allows for direct interaction with the API.

## How it Works

Conflux is built using .NET and follows a layered architecture:
-   **[`Conflux.API`](Conflux.API)**: Exposes the RESTful API endpoints, handles request/response processing, and authentication.
-   **[`Conflux.Domain.Logic`](Conflux.Domain.Logic)**: Contains the core business logic, services (e.g., [`ContributorsService`](Conflux.Domain.Logic/Services/ContributorsService.cs)), and domain-specific operations. Includes the `OnnxEmbeddingService` for local semantic search.
-   **[`Conflux.Domain`](Conflux.Domain)**: Defines the domain entities and interfaces. Project entities include vector embeddings for semantic search.
-   **[`Conflux.Data`](Conflux.Data)**: Manages data persistence using Entity Framework Core with PostgreSQL and pgvector extension for vector similarity search.
-   **Integrations**: Modules for connecting with external services:
    -   [`Conflux.Integrations.RAiD`](Conflux.Integrations.RAiD) (e.g., [`ProjectMapperService.cs`](Conflux.Integrations.RAiD/ProjectMapperService.cs))
    -   [`Conflux.Integrations.SRAM`](Conflux.Integrations.SRAM)
    -   [`Conflux.Integrations.NWOpen`](Conflux.Integrations.NWOpen)

### Semantic Search Features

Conflux includes advanced semantic search capabilities:
-   **Multilingual Support**: Supports semantic search across 100+ languages using the all-MiniLM-L12-v2.onnx model
-   **Self-Hosted**: All embedding generation happens locally using ONNX Runtime - no external API calls required
-   **Always Up-to-Date**: Embeddings are automatically updated whenever project titles or descriptions change
-   **Vector Similarity**: Uses PostgreSQL's pgvector extension for efficient vector similarity search
-   **Hybrid Search**: Combines traditional text search with semantic vector search for optimal results

## Documentation

-   Project documentation is generated using `docfx`. Refer to [docfx.json](docfx.json) for its configuration.
-   The main entry point for browsing the documentation is [index.md](index.md).
-   Supplementary documentation can be found in the [docs/](docs/) directory, starting with [docs/index.md](docs/index.md).

## License

This project is licensed under the terms of the GNU Affero General Public License v3.0. See the [LICENSE](LICENSE) file for the full license text.