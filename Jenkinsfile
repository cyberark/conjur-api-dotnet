#!/usr/bin/env groovy

pipeline {
  agent { label 'executor-v2' }

  options {
    timestamps()
    buildDiscarder(logRotator(numToKeepStr: '30'))
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
        GIT_COMMIT_HASH = sh(script: "git rev-parse --short=8 HEAD", returnStdout: true).trim()
        ARTIFACT_REMOTE_DIRECTORY = "${env.BRANCH_NAME}_${GIT_COMMIT_HASH}"
        sh "summon -e pipeline ./build.sh ${ARTIFACT_REMOTE_DIRECTORY}"
        step([$class: 'XUnitBuilder',
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
