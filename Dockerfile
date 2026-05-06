FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:f9ddb8a31ae90f4b38d18355d82f03d76dcdd2a57d7235a2ffdf008fab11a862
WORKDIR /app

# Install dependencies
COPY . .
RUN dotnet restore

# Install dotnet-ef
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

EXPOSE 5273
ENTRYPOINT ["dotnet", "run"]
