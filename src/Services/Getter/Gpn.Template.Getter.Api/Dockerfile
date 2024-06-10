FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Getter/Gpn.Template.Getter.Api/Gpn.Template.Getter.Api.csproj", "src/Services/Getter/Gpn.Template.Getter.Api/"]
COPY ["src/Shared/Core/Shared.Presentation.Core/Shared.Presentation.Core.csproj", "src/Shared/Core/Shared.Presentation.Core/"]
COPY ["src/Shared/Core/Shared.Infrastructure.Core/Shared.Infrastructure.Core.csproj", "src/Shared/Core/Shared.Infrastructure.Core/"]
COPY ["src/Shared/Core/Shared.Application.Core/Shared.Application.Core.csproj", "src/Shared/Core/Shared.Application.Core/"]
COPY ["src/Shared/Core/Shared.Domain.Core/Shared.Domain.Core.csproj", "src/Shared/Core/Shared.Domain.Core/"]
COPY ["src/Shared/Core/Shared.Common/Shared.Common.csproj", "src/Shared/Core/Shared.Common/"]
COPY ["src/Services/Getter/Abstractions/Gpn.Template.Getter.Api.Abstractions/Gpn.Template.Getter.Api.Abstractions.csproj", "src/Services/Getter/Abstractions/Gpn.Template.Getter.Api.Abstractions/"]
COPY ["src/Services/Getter/Abstractions/Gpn.Template.Getter.Application.Abstractions/Gpn.Template.Getter.Application.Abstractions.csproj", "src/Services/Getter/Abstractions/Gpn.Template.Getter.Application.Abstractions/"]
COPY ["src/Services/Getter/Gpn.Template.Getter.Infrastructure/Gpn.Template.Getter.Infrastructure.csproj", "src/Services/Getter/Gpn.Template.Getter.Infrastructure/"]
COPY ["src/Services/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "src/Services/Shared/Shared.Infrastructure/"]
COPY ["src/Shared/Logging/Shared.Infrastructure.Logging/Shared.Infrastructure.Logging.csproj", "src/Shared/Logging/Shared.Infrastructure.Logging/"]
COPY ["src/Shared/Mapper/Shared.Infrastructure.Mapper.AutoMapper/Shared.Infrastructure.Mapper.AutoMapper.csproj", "src/Shared/Mapper/Shared.Infrastructure.Mapper.AutoMapper/"]
COPY ["src/Services/Shared/Shared.Application/Shared.Application.csproj", "src/Services/Shared/Shared.Application/"]
COPY ["src/Services/Shared/Shared.Domain/Shared.Domain.csproj", "src/Services/Shared/Shared.Domain/"]
COPY ["src/Services/Getter/Gpn.Template.Getter.Application/Gpn.Template.Getter.Application.csproj", "src/Services/Getter/Gpn.Template.Getter.Application/"]
RUN dotnet restore "src/Services/Getter/Gpn.Template.Getter.Api/Gpn.Template.Getter.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/Getter/Gpn.Template.Getter.Api"
RUN dotnet build "Gpn.Template.Getter.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Gpn.Template.Getter.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gpn.Template.Getter.Api.dll"]