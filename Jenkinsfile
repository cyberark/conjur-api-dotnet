#!/usr/bin/env groovy

pipeline {
  agent { label 'executor-v2' }

  options {
    timestamps()
    buildDiscarder(logRotator(numToKeepStr: '30'))
  }

  triggers {
    cron(getDailyCronString())
  }

  stages {
    stage('Validate') {
      parallel {
        stage('Changelog') {
          steps { sh './test/parse-changelog.sh' }
        }
      }
    }

    stage('Prepare build environment') {
      steps {
        sh '''
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

    stage('Build and test package') {
      steps {
        script {
          BUILD_NAME = "${env.BUILD_NUMBER}-${env.BRANCH_NAME.replace('/','-')}"
          sh "summon -e pipeline ./build.sh ${BUILD_NAME}"
        }
        step([$class: 'XUnitPublisher',
          tools: [[$class: 'NUnitJunitHudsonTestType', pattern: 'TestResult.xml']]])
        archiveArtifacts artifacts: 'bin/*', fingerprint: true
      }
    }
  }

  post {
    always {
      cleanupAndNotify(currentBuild.currentResult)
    }
  }
}
