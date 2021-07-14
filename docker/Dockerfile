FROM mono

RUN apt-get update -y && apt-get install -y gnupg2

# This is for osslsigncode
ADD dividedmind.pub /tmp
RUN apt-key add - < /tmp/dividedmind.pub && rm /tmp/dividedmind.pub
RUN echo 'deb http://download.opensuse.org/repositories:/dividedmind:/conjur/Debian_7.0/ /' >> /etc/apt/sources.list.d/dividedmind.list

RUN apt-get install -y nunit-console git osslsigncode

#ENV NUNIT_VERSION 3.0.1
#RUN nuget install NUnit.ConsoleRunner -o /tmp/nunit -version $NUNIT_VERSION && \
#    cp -r /tmp/nunit/NUnit.ConsoleRunner.$NUNIT_VERSION/tools/ /nunit/
#RUN echo '#!/bin/bash\nmono /nunit/nunit3-console.exe "$@"' > /usr/bin/nunit-console && \
#    chmod +x /usr/bin/nunit-console

RUN ln -s /src/docker/build.sh /
CMD /build.sh

ADD packages.config /packages/
RUN nuget restore /packages/packages.config -PackagesDirectory /packages