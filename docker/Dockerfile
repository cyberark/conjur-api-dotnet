FROM mcr.microsoft.com/dotnet/sdk:6.0

RUN apt-get update -y && apt-get install -y gnupg2 git osslsigncode

RUN ln -s /src/docker/build.sh /
CMD /build.sh
