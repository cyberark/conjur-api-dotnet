FROM mono

# This is for osslsigncode
ADD dividedmind.pub /tmp
RUN apt-key add - < /tmp/dividedmind.pub && rm /tmp/dividedmind.pub
RUN echo 'deb http://download.opensuse.org/repositories/home:/dividedmind:/conjur/Debian_7.0/ /' >> /etc/apt/sources.list.d/dividedmind.list

RUN apt-get update -y
RUN apt-get install -y nunit-console git osslsigncode

RUN ln -s /src/docker/build.sh /
CMD /build.sh

ADD packages.config /packages/
RUN nuget restore /packages/packages.config -PackagesDirectory /packages
