FROM mono

RUN apt-get update -y && apt-get install -y gnupg2 git osslsigncode

# Install nuget-console of version 3.X.X as apt-get only has up to 2.X.X version
ENV NUNIT_VERSION 3.0.1
RUN nuget install NUnit.Console -o /tmp/nunit -version $NUNIT_VERSION && \
    cp -r /tmp/nunit/NUnit.Console.$NUNIT_VERSION/tools/ /nunit/
RUN echo '#!/bin/bash\nmono /nunit/nunit3-console.exe "$@"' > /usr/bin/nunit-console && \
    chmod +x /usr/bin/nunit-console

RUN ln -s /src/docker/build.sh /
CMD /build.sh

ADD packages.config /packages/
RUN nuget restore /packages/packages.config -PackagesDirectory /packages
