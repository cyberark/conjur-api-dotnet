#!/usr/bin/env groovy
@Library("product-pipelines-shared-library") _

pipeline {
  agent { label 'conjur-enterprise-common-agent' }

  options {
    timestamps()
    buildDiscarder(logRotator(numToKeepStr: '30'))
  }

  triggers {
    cron(getDailyCronString())
  }

  stages {
    stage('Scan for internal URLs') {
      steps {
        script {
          detectInternalUrls()
        }
      }
    }

    stage('Get InfraPool Agent') {
      steps {
        script {
          INFRAPOOL_EXECUTORV2_AGENT_0 = getInfraPoolAgent.connected(type: "ExecutorV2", quantity: 1, duration: 1)[0]
        }
      }
    }

    stage('Validate') {
      parallel {
        stage('Changelog') {
          steps { parseChangelog(INFRAPOOL_EXECUTORV2_AGENT_0) }
        }
      }
    }

    stage('Prepare build environment') {
      steps {
        script {
          INFRAPOOL_EXECUTORV2_AGENT_0.agentSh '''
            # make sure the build env is up to date
            make -C docker

            TAG=`cat docker/tag`

            if [ -z `docker images -q $TAG` ]; then
              # the image is not present, so pull or build
              docker pull $TAG || make -C docker rebuild
            fi
          '''
        }
      }
    }

    stage('Build and test package') {
      steps {
        script {
          BUILD_NAME = "${env.BUILD_NUMBER}-${env.BRANCH_NAME.replace('/','-')}"
          INFRAPOOL_EXECUTORV2_AGENT_0.agentSh "summon -e pipeline ./build.sh ${BUILD_NAME}"
          INFRAPOOL_EXECUTORV2_AGENT_0.agentStash name: 'test-results', includes: '*.xml'
          unstash 'test-results'
          junit 'TestResults.xml'
          INFRAPOOL_EXECUTORV2_AGENT_0.agentArchiveArtifacts artifacts: 'bin/*', fingerprint: true
        }
      }
    }
  }

  post {
    always {
      releaseInfraPoolAgent(".infrapool/release_agents")
    }
  }
}
