FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Bff/Gpn.Template.Bff.Api/Gpn.Template.Bff.Api.csproj", "src/Services/Bff/Gpn.Template.Bff.Api/"]
COPY ["src/Shared/Core/Shared.Presentation.Core/Shared.Presentation.Core.csproj", "src/Shared/Core/Shared.Presentation.Core/"]
COPY ["src/Shared/Core/Shared.Infrastructure.Core/Shared.Infrastructure.Core.csproj", "src/Shared/Core/Shared.Infrastructure.Core/"]
COPY ["src/Shared/Core/Shared.Application.Core/Shared.Application.Core.csproj", "src/Shared/Core/Shared.Application.Core/"]
COPY ["src/Shared/Core/Shared.Domain.Core/Shared.Domain.Core.csproj", "src/Shared/Core/Shared.Domain.Core/"]
COPY ["src/Shared/Core/Shared.Common/Shared.Common.csproj", "src/Shared/Core/Shared.Common/"]
COPY ["src/Services/Getter/Abstractions/Gpn.Template.Getter.Api.Abstractions/Gpn.Template.Getter.Api.Abstractions.csproj", "src/Services/Getter/Abstractions/Gpn.Template.Getter.Api.Abstractions/"]
COPY ["src/Services/Getter/Abstractions/Gpn.Template.Getter.Application.Abstractions/Gpn.Template.Getter.Application.Abstractions.csproj", "src/Services/Getter/Abstractions/Gpn.Template.Getter.Application.Abstractions/"]
COPY ["src/Services/Setter/Abstractions/Gpn.Template.Setter.Api.Abstractions/Gpn.Template.Setter.Api.Abstractions.csproj", "src/Services/Setter/Abstractions/Gpn.Template.Setter.Api.Abstractions/"]
COPY ["src/Services/Setter/Abstractions/Gpn.Template.Setter.Application.Abstractions/Gpn.Template.Setter.Application.Abstractions.csproj", "src/Services/Setter/Abstractions/Gpn.Template.Setter.Application.Abstractions/"]
RUN dotnet restore "./src/Services/Bff/Gpn.Template.Bff.Api/Gpn.Template.Bff.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/Bff/Gpn.Template.Bff.Api"
RUN dotnet build "./Gpn.Template.Bff.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gpn.Template.Bff.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gpn.Template.Bff.Api.dll"]