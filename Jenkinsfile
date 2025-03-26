#!/usr/bin/env groovy
@Library("product-pipelines-shared-library") _

// Automated release, promotion and dependencies
properties([
  // Include the automated release parameters for the build
  release.addParams(),
  // Dependencies of the project that should trigger builds
  dependencies([])
])

// Performs release promotion.  No other stages will be run
if (params.MODE == "PROMOTE") {
  release.promote(params.VERSION_TO_PROMOTE) { sourceVersion, targetVersion, assetDirectory ->

  }
  // Copy Github Enterprise release to Github
  release.copyEnterpriseRelease(params.VERSION_TO_PROMOTE)
  return
}

pipeline {
  agent { label 'conjur-enterprise-common-agent' }

  options {
    timestamps()
    buildDiscarder(logRotator(numToKeepStr: '30'))
  }

  environment {
    // Sets the MODE to the specified or autocalculated value as appropriate
    MODE = release.canonicalizeMode()
  }

  triggers {
    cron(getDailyCronString())
    parameterizedCron(getWeeklyCronString("H(1-5)","%MODE=RELEASE"))
  }

  stages {
    // Aborts any builds triggered by another project that wouldn't include any changes
    stage ("Skip build if triggering job didn't create a release") {
      when {
        expression {
          MODE == "SKIP"
        }
      }
      steps {
        script {
          currentBuild.result = 'ABORTED'
          error("Aborting build because this build was triggered from upstream, but no release was built")
        }
      }
    }

    stage('Scan for internal URLs') {
      steps {
        script {
          detectInternalUrls()
        }
      }
    }

    stage('Get InfraPool ExecutorV2 Agent') {
      steps {
        script {
          // Request ExecutorV2 agents for 1 hour(s)
          INFRAPOOL_EXECUTORV2_AGENTS = getInfraPoolAgent(type: "ExecutorV2", quantity: 1, duration: 1)
          INFRAPOOL_EXECUTORV2_AGENT_0 = INFRAPOOL_EXECUTORV2_AGENTS[0]
          infrapool = infraPoolConnect(INFRAPOOL_EXECUTORV2_AGENT_0, {})
        }
      }
    }

    stage('Validate') {
      parallel {
        stage('Changelog') {
          steps { parseChangelog(infrapool) }
        }
      }
    }

    // Generates a VERSION file based on the current build number and latest version in CHANGELOG.md
    stage('Validate Changelog and set version') {
      steps {
        script {
          updateVersion(infrapool, "CHANGELOG.md", "${BUILD_NUMBER}")
        }
      }
    }

    stage('Prepare build environment') {
      steps {
        script {
          infrapool.agentSh '''
            # make sure the build env is up to date
            make -C docker

            TAG=`cat docker/tag`

            if [ -z `docker images -q $TAG` ]; then
              # the image is not present, so pull or build
              docker pull $TAG || make -C docker rebuild && make -C docker push
            fi
          '''
        }
      }
    }

    stage('Build and test package') {
      steps {
        script {
          infrapool.agentSh "summon -e pipeline ./build.sh"
          infrapool.agentStash name: 'test-results', includes: '*.xml'
          unstash 'test-results'
          junit 'TestResults.xml'
          
          cobertura autoUpdateHealth: false,
                    autoUpdateStability: false,
                    coberturaReportFile: 'Coverage.xml',
                    conditionalCoverageTargets: '60, 0, 0',
                    failUnhealthy: true,
                    failUnstable: false,
                    lineCoverageTargets: '70, 0, 0',
                    maxNumberOfBuilds: 0,
                    methodCoverageTargets: '60, 0, 0',
                    onlyStable: false,
                    sourceEncoding: 'ASCII',
                    zoomCoverageChart: false

          infrapool.agentArchiveArtifacts artifacts: 'bin/*', fingerprint: true
        }
      }
    }

    stage('Release') {
      when {
        expression {
          MODE == "RELEASE"
        }
      }
      steps {
        script {
          release(infrapool) { billOfMaterialsDirectory, assetDirectory, toolsDirectory ->
            // Publish release artifacts to all the appropriate locations

            // Copy any artifacts to assetDirectory to attach them to the Github release
            infrapool.agentSh "cp -r bin/* ${assetDirectory}"
          }
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
