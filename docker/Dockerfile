FROM mcr.microsoft.com/dotnet/sdk:8.0

ENV VERSION=""
ENV PATH="$PATH:/root/.dotnet/tools"

RUN apt-get update -y && \
    apt-get install --no-install-recommends -y gnupg2 git osslsigncode && \
    apt-get clean

RUN dotnet tool install --global trx2junit --version 2.1.0

RUN ln -s /src/docker/build.sh /
RUN ln -s /src/docker/release.sh /
CMD ["bash", "/build.sh"]
