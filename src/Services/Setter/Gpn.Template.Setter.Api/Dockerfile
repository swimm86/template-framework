FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Setter/Gpn.Template.Setter.Api/Gpn.Template.Setter.Api.csproj", "src/Services/Setter/Gpn.Template.Setter.Api/"]
COPY ["src/Shared/Core/Shared.Presentation.Core/Shared.Presentation.Core.csproj", "src/Shared/Core/Shared.Presentation.Core/"]
COPY ["src/Shared/Core/Shared.Infrastructure.Core/Shared.Infrastructure.Core.csproj", "src/Shared/Core/Shared.Infrastructure.Core/"]
COPY ["src/Shared/Core/Shared.Application.Core/Shared.Application.Core.csproj", "src/Shared/Core/Shared.Application.Core/"]
COPY ["src/Shared/Core/Shared.Domain.Core/Shared.Domain.Core.csproj", "src/Shared/Core/Shared.Domain.Core/"]
COPY ["src/Shared/Core/Shared.Common/Shared.Common.csproj", "src/Shared/Core/Shared.Common/"]
COPY ["src/Services/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "src/Services/Shared/Shared.Infrastructure/"]
COPY ["src/Shared/Logging/Shared.Infrastructure.Logging/Shared.Infrastructure.Logging.csproj", "src/Shared/Logging/Shared.Infrastructure.Logging/"]
COPY ["src/Shared/Mapper/Shared.Infrastructure.Mapper.AutoMapper/Shared.Infrastructure.Mapper.AutoMapper.csproj", "src/Shared/Mapper/Shared.Infrastructure.Mapper.AutoMapper/"]
COPY ["src/Services/Shared/Shared.Application/Shared.Application.csproj", "src/Services/Shared/Shared.Application/"]
COPY ["src/Services/Shared/Shared.Domain/Shared.Domain.csproj", "src/Services/Shared/Shared.Domain/"]
COPY ["src/Services/Setter/Abstractions/Gpn.Template.Setter.Api.Abstractions/Gpn.Template.Setter.Api.Abstractions.csproj", "src/Services/Setter/Abstractions/Gpn.Template.Setter.Api.Abstractions/"]
COPY ["src/Services/Setter/Abstractions/Gpn.Template.Setter.Application.Abstractions/Gpn.Template.Setter.Application.Abstractions.csproj", "src/Services/Setter/Abstractions/Gpn.Template.Setter.Application.Abstractions/"]
RUN dotnet restore "./src/Services/Setter/Gpn.Template.Setter.Api/Gpn.Template.Setter.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/Setter/Gpn.Template.Setter.Api"
RUN dotnet build "./Gpn.Template.Setter.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gpn.Template.Setter.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gpn.Template.Setter.Api.dll"]