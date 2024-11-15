FROM mcr.microsoft.com/dotnet/sdk:6.0

ENV VERSION=""

RUN apt-get update -y && \
    apt-get install --no-install-recommends -y gnupg2 git osslsigncode && \
    apt-get clean

RUN ln -s /src/docker/build.sh /
CMD ["/build.sh"]
