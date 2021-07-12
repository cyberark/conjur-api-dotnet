# This Makefile is to make sure docker image isn't rebuilt unless the source
# files changed, while pinning docker image version to project source version.
# As long as tag is committed in the repo, the corresponding image can be
# pushed to dockerhub and then pulled when necessary.

FILES = Dockerfile packages.config
REPO = conjurinc/dotnet-build
TAG := ${REPO}:$(shell date -u +%Y%m%dT%H%M%SZ)

tag: $(FILES)
	docker build -t ${TAG} .
	echo ${TAG} > tag
	docker tag -f ${TAG} ${REPO}

.PHONY: rebuild push
rebuild: tag
	docker build -t $(shell cat tag) .

push: tag
	docker push $(shell cat tag)
