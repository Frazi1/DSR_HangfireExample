FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["DSR_HangfireExample.csproj", "./"]
RUN dotnet restore "DSR_HangfireExample.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "DSR_HangfireExample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DSR_HangfireExample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DSR_HangfireExample.dll"]
