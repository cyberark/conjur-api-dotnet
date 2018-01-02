#!/usr/bin/env groovy

pipeline {
  agent { label 'executor-v2' }

  options {
    timestamps()
    buildDiscarder(logRotator(numToKeepStr: '30'))
  }

  stages {
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
        sh './build.sh'
        step([$class: 'XUnitBuilder',
          tools: [[$class: NJUnitType', pattern: 'TestResult.xml']]])
        archiveArtifacts artifacts: 'bin/*', fingerprint: true
      }
    }
    
    stage('Sign package') {
      steps {
        sh './sign.sh'
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
